﻿using Akka.Actor;
using DgSystems.NFe.Services.Actors;
using NFe.Core.Cadastro.Certificado;
using NFe.Core.Domain;
using NFe.Core.Entitities;
using NFe.Core.Events;
using NFe.Core.Interfaces;
using NFe.Core.Messaging;
using NFe.Core.NotasFiscais;
using NFe.Core.NotasFiscais.Sefaz.NfeConsulta2;
using NFe.Core.NotasFiscais.Services;
using NFe.Core.Sefaz;
using NFe.Core.Sefaz.Facades;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DgSystems.NFe.Services.Actors
{
    public class ModoOnlineActor : ReceiveActor
    {
        #region Messages
        private class Tick { }
        public class Start { }
        public class AtivarModoOnline { }
        public class AtivarModoOffline
        {
            public AtivarModoOffline(string justificativa, DateTime dataHoraContingencia)
            {
                DataHoraContingencia = dataHoraContingencia;
                Justificativa = justificativa;
            }

            public DateTime DataHoraContingencia { get; }
            public string Justificativa { get; }
        }
        #endregion

        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IConfiguracaoRepository _configuracaoRepository;
        private readonly IConsultaStatusServicoSefazService _consultaStatusServicoService;
        private readonly IEmiteNotaFiscalContingenciaFacade _emiteNotaFiscalContingenciaService;
        private readonly INotaFiscalRepository _notaFiscalRepository;
        private readonly IEmitenteRepository emissorService;
        private readonly IConsultarNotaFiscalService nfeConsulta;
        private readonly IServiceFactory serviceFactory;
        private readonly ICertificadoService certificadoService;
        private readonly SefazSettings sefazSettings;
        private IActorRef _self;
        private IActorRef emiteNfeContingenciaActor;

        public ModoOnlineActor(IConfiguracaoRepository configuracaoRepository, IConsultaStatusServicoSefazService consultaStatusServicoService,
            INotaFiscalRepository notaFiscalRepository, IEmiteNotaFiscalContingenciaFacade emiteNotaFiscalContingenciaService, ActorSystem actorSystem,
            IEmitenteRepository emissorService, IConsultarNotaFiscalService nfeConsulta, IServiceFactory serviceFactory, ICertificadoService certificadoService, SefazSettings sefazSettings)
        {
            _notaFiscalRepository = notaFiscalRepository;
            _configuracaoRepository = configuracaoRepository;
            _consultaStatusServicoService = consultaStatusServicoService;

            _emiteNotaFiscalContingenciaService = emiteNotaFiscalContingenciaService;

            this.emissorService = emissorService;
            this.nfeConsulta = nfeConsulta;
            this.serviceFactory = serviceFactory;
            this.certificadoService = certificadoService;
            this.sefazSettings = sefazSettings;

            MessagingCenter.Subscribe<EnviarNotaFiscalService, NotaFiscalEmitidaEmContingenciaEvent>(this, nameof(NotaFiscalEmitidaEmContingenciaEvent), (s, e) =>
            {
                NotaEmitidaEmContingenciaEvent(e.justificativa, e.horário);
            });

            _self = Self; // use only in NotaEmitidaEmContingenciaEvent

            Receive<Start>(HandleStart);
            Receive<Tick>(HandleTick);
            Receive<AtivarModoOffline>(HandleAtivarModoOffline);
            Receive<AtivarModoOnline>(HandleAtivarModoOnline);
            Receive<EmiteNFeContingenciaActor.ResultadoNotasTransmitidas>(HandleResultadoNotasTransmitidas);
            Receive<ReceiveTimeout>(HandleReceiveTimeout);
        }

        private void HandleReceiveTimeout(ReceiveTimeout msg)
        {
            SetReceiveTimeout(null);
            var theEvent = new NotasFiscaisTransmitidasEvent() { MensagensErro = new List<string> { "Erro ao tentar transmitir notas emitidas em contingência." } };
            MessagingCenter.Send(this, nameof(NotasFiscaisTransmitidasEvent), theEvent);
            log.Error("Erro ao tentar transmitir as notas emitidas em contingência.");

            Context.Stop(emiteNfeContingenciaActor);
        }

        private void HandleResultadoNotasTransmitidas(EmiteNFeContingenciaActor.ResultadoNotasTransmitidas msg)
        {
            try
            {
                var configuração = _configuracaoRepository.GetConfiguracao();

                if (msg.Erros != null)
                {
                    var primeiraNotaContingencia = _notaFiscalRepository.GetPrimeiraNotaEmitidaEmContingencia(configuração.DataHoraEntradaContingencia, DateTime.Now);

                    NotaFiscalEntity notaParaCancelar = null;

                    if (primeiraNotaContingencia != null)
                    {
                        var numero = int.Parse(primeiraNotaContingencia.Numero) - 1;
                        notaParaCancelar = _notaFiscalRepository.GetNota(numero.ToString(), primeiraNotaContingencia.Serie,
                            primeiraNotaContingencia.Modelo);
                    }

                    _emiteNotaFiscalContingenciaService.InutilizarCancelarNotasPendentesContingencia(notaParaCancelar, _notaFiscalRepository);

                    var theEvent = new NotasFiscaisTransmitidasEvent() { MensagensErro = msg.Erros };
                    MessagingCenter.Send(this, nameof(NotasFiscaisTransmitidasEvent), theEvent);
                }

                configuração.IsContingencia = false;
                _configuracaoRepository.Salvar(configuração);
            }
            catch (Exception e)
            {
                var theEvent = new NotasFiscaisTransmitidasEvent() { MensagensErro = new List<string> { "Erro ao tentar transmitir notas emitidas em contingência." } };
                MessagingCenter.Send(this, nameof(NotasFiscaisTransmitidasEvent), theEvent);
                log.Error("Erro ao tentar transmitir as notas emitidas em contingência.", e);
            }
            finally
            {
                Context.Stop(emiteNfeContingenciaActor);
            }
        }

        private void NotaEmitidaEmContingenciaEvent(string justificativa, DateTime horário)
        {
            log.Info("Evento de nota fiscal emitida em contingência recebido.");

            _self.Tell(new AtivarModoOffline(justificativa, horário));
        }

        private void HandleAtivarModoOnline(AtivarModoOnline msg)
        {
            var configuração = _configuracaoRepository.GetConfiguracao();

            configuração.IsContingencia = false;
            _configuracaoRepository.Salvar(configuração);

            SetReceiveTimeout(TimeSpan.FromSeconds(30));
            emiteNfeContingenciaActor = Context.ActorOf(Props.Create(() => new EmiteNFeContingenciaActor(_notaFiscalRepository, emissorService, nfeConsulta, serviceFactory, certificadoService, sefazSettings)));
            emiteNfeContingenciaActor.Tell(new EmiteNFeContingenciaActor.TransmitirNFeEmContingencia());
        }

        private void HandleAtivarModoOffline(AtivarModoOffline msg)
        {
            var config = _configuracaoRepository.GetConfiguracao();
            config.IsContingencia = true;
            config.DataHoraEntradaContingencia = msg.DataHoraContingencia;
            config.JustificativaContingencia = msg.Justificativa;
            _configuracaoRepository.Salvar(config);

            var theEvent = new ServicoOfflineEvent();
            MessagingCenter.Send(this, nameof(ServicoOfflineEvent), theEvent);
        }

        private void HandleTick(Tick obj)
        {
            log.Info("Verificando estado do serviço da Sefaz.");
            var config = _configuracaoRepository.GetConfiguracao();

            if (config == null)
                return;

            if (_consultaStatusServicoService.ExecutarConsultaStatus(config, Modelo.Modelo55)
                && _consultaStatusServicoService.ExecutarConsultaStatus(config, Modelo.Modelo65))
            {
                if (!config.IsContingencia) return;

                Self.Tell(new AtivarModoOnline());
                log.Info("Modo online ativado.");
            }
            else
            {
                if (config.IsContingencia) return;

                Self.Tell(new AtivarModoOffline("Serviço indisponível ou sem conexão com a internet", DateTime.Now));
                log.Info("Modo offline ativado.");
            }
        }

        private void HandleStart(Start obj)
        {
            Context.System.Scheduler.ScheduleTellRepeatedly(
               TimeSpan.FromMilliseconds(0),
               TimeSpan.FromMilliseconds(3 * 60 * 1000),
               Self,
               new Tick(),
               Self);
        }
    }
}