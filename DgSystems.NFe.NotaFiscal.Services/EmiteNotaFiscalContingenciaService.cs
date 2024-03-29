﻿using NFe.Core.Cadastro.Certificado;
using NFe.Core.Cadastro.Configuracoes;
using NFe.Core.Domain;
using NFe.Core.Entitities;
using NFe.Core.Interfaces;
using NFe.Core.NFeAutorizacao4;
using NFe.Core.NFeRetAutorizacao4;
using NFe.Core.NotasFiscais;
using NFe.Core.NotasFiscais.Sefaz.NfeAutorizacao;
using NFe.Core.NotasFiscais.Sefaz.NfeConsulta2;
using NFe.Core.NotasFiscais.Services;
using NFe.Core.Utils.Conversores;
using NFe.Core.Utils.Xml;
using NFe.Core.XmlSchemas.NfeAutorizacao.Envio;
using NFe.Core.XmlSchemas.NfeAutorizacao.Retorno;
using NFe.Core.XmlSchemas.NfeRetAutorizacao.Envio;
using NFe.Core.XmlSchemas.NfeRetAutorizacao.Retorno;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace NFe.Core.Sefaz.Facades
{
    public class EmiteNotaFiscalContingenciaFacade : IEmiteNotaFiscalContingenciaFacade
    {
        static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string MensagemErro =
            "Tentativa de transmissão de notas em contingência falhou. Serviço continua indisponível.";
        private readonly IConfiguracaoRepository _configuracaoService;
        private readonly INotaFiscalRepository _notaFiscalRepository;

        private bool _isFirstTimeRecheckingRecipts;
        private bool _isFirstTimeResending;
        private readonly IEmitenteRepository _emissorService;
        private readonly IConsultarNotaFiscalService _nfeConsulta;
        private readonly IServiceFactory _serviceFactory;
        private readonly ICertificadoService _certificadoService;
        private readonly InutilizarNotaFiscalService _notaInutilizadaFacade;
        private readonly ICancelaNotaFiscalService _cancelaNotaFiscalService;
        private readonly SefazSettings _sefazSettings;

        public EmiteNotaFiscalContingenciaFacade(IConfiguracaoRepository configuracaoService, INotaFiscalRepository notaFiscalRepository, IEmitenteRepository emissorService, IConsultarNotaFiscalService nfeConsulta, IServiceFactory serviceFactory, ICertificadoService certificadoService, InutilizarNotaFiscalService notaInutilizadaFacade, ICancelaNotaFiscalService cancelaNotaFiscalService, SefazSettings sefazSettings)
        {
            _configuracaoService = configuracaoService;
            _notaFiscalRepository = notaFiscalRepository;
            _emissorService = emissorService;
            _nfeConsulta = nfeConsulta;
            _serviceFactory = serviceFactory;
            _certificadoService = certificadoService;
            _notaInutilizadaFacade = notaInutilizadaFacade;
            _cancelaNotaFiscalService = cancelaNotaFiscalService;
            _sefazSettings = sefazSettings;
        }

        public Domain.NotaFiscal SaveNotaFiscalContingencia(X509Certificate2 certificado, ConfiguracaoEntity config, NotaFiscal notaFiscal, string cscId, string csc, string nFeNamespaceName)
        {
            notaFiscal = SetContingenciaFields(config, notaFiscal);
            var xmlNFeContingencia = new XmlNFe(notaFiscal, nFeNamespaceName, certificado, cscId, csc);
            notaFiscal.QrCodeUrl = xmlNFeContingencia.QrCode.ToString();

            _notaFiscalRepository.Salvar(notaFiscal, xmlNFeContingencia.XmlNode.OuterXml);
            return notaFiscal;
        }

        private Domain.NotaFiscal SetContingenciaFields(ConfiguracaoEntity config, NotaFiscal notaFiscal)
        {
            notaFiscal.Identificacao.Numero = _configuracaoService.ObterProximoNumeroNotaFiscal(notaFiscal.Identificacao.Modelo);
            notaFiscal.Identificacao.DataHoraEntradaContigencia = config.DataHoraEntradaContingencia;
            notaFiscal.Identificacao.JustificativaContigencia = config.JustificativaContingencia;
            notaFiscal.Identificacao.TipoEmissao = notaFiscal.Identificacao.Modelo == Modelo.Modelo65
                ? TipoEmissao.ContigenciaNfce
                : TipoEmissao.FsDa;
            notaFiscal.CalcularChave();
            notaFiscal.Identificacao.Status = new StatusEnvio(Status.CONTINGENCIA);
            return notaFiscal;
        }

        public async Task<List<string>> TransmitirNotasFiscalEmContingencia() //Chamar esse método quando o serviço voltar
        {
            var erros = new List<string>();

            var notas = _notaFiscalRepository.GetNotasContingencia();

            var notasNFe = new List<string>();
            var notasNfCe = new List<string>();

            foreach (var nota in notas)
            {
                var xml = await nota.LoadXmlAsync();

                if (nota.Modelo.Equals("55"))
                    notasNFe.Add(xml);
                else
                    notasNfCe.Add(xml);
            }

            try
            {
                if (notasNfCe.Count != 0)
                    erros = await TransmitirConsultarLoteContingenciaAsync(notasNfCe, Modelo.Modelo65);

                if (notasNFe.Count != 0)
                    erros = await TransmitirConsultarLoteContingenciaAsync(notasNFe, Modelo.Modelo55);
            }
            catch (Exception e)
            {
                log.Error(e);
                var sDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "EmissorNFeDir");

                if (!Directory.Exists(sDirectory)) Directory.CreateDirectory(sDirectory);

                using (var stream = File.Create(Path.Combine(sDirectory, "logTransmitirContingencia.txt")))
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        await writer.WriteLineAsync(e.ToString());
                    }
                }

                return null;
            }

            return erros;
        }

        public void InutilizarCancelarNotasPendentesContingencia(NotaFiscalEntity notaParaCancelar,
            INotaFiscalRepository notaFiscalRepository)
        {
            if (notaParaCancelar == null || notaParaCancelar.Status == 0)
                return;

            var emitente = _emissorService.GetEmissor();
            var ufEmissor = emitente.Endereco.UF;
            var codigoUf = UfToCodigoUfConversor.GetCodigoUf(ufEmissor);

            var certificado = _certificadoService.GetX509Certificate2();
            var modelo = notaParaCancelar.Modelo.Equals("55") ? Modelo.Modelo55 : Modelo.Modelo65;

            var result = _nfeConsulta.ConsultarNotaFiscal(notaParaCancelar.Chave, codigoUf, certificado, modelo);
            var codigoUfEnum = (CodigoUfIbge)Enum.Parse(typeof(CodigoUfIbge), emitente.Endereco.UF);

            if (result.IsEnviada)
            {
                var dadosNotaParaCancelar = new DadosNotaParaCancelar
                {
                    ufEmitente = ufEmissor,
                    codigoUf = codigoUfEnum,
                    cnpjEmitente = emitente.CNPJ,
                    chaveNFe = notaParaCancelar.Chave,
                    protocoloAutorizacao = result.Protocolo.infProt.nProt,
                    modeloNota = modelo
                };

                _cancelaNotaFiscalService.CancelarNotaFiscal(dadosNotaParaCancelar, "Nota duplicada em contingência");
            }
            else
            {
                var resultadoInutilizacao = _notaInutilizadaFacade.InutilizarNotaFiscal(ufEmissor, codigoUfEnum,
                    emitente.CNPJ, modelo, notaParaCancelar.Serie,
                    notaParaCancelar.Numero, notaParaCancelar.Numero);

                if (resultadoInutilizacao.Status != NotasFiscais.Sefaz.NfeInutilizacao2.Status.ERRO)
                    _notaFiscalRepository.ExcluirNota(notaParaCancelar.Chave);
            }
        }

        private async Task<List<string>> TransmitirConsultarLoteContingenciaAsync(List<string> notasNfCe, Modelo modelo)
        {
            var retornoTransmissao = TransmitirLoteNotasFiscaisContingencia(notasNfCe, modelo);

            switch (retornoTransmissao.TipoMensagem)
            {
                case TipoMensagem.ErroValidacao:
                    return new List<string> { retornoTransmissao.Mensagem };
                case TipoMensagem.ServicoIndisponivel:
                    return new List<string> { MensagemErro };
            }

            var tempoEspera = int.Parse(retornoTransmissao.RetEnviNFeInfRec.tMed) * 1000;
            var erros = new List<string>();
            Thread.Sleep(tempoEspera);
            var resultadoConsulta = ConsultarReciboLoteContingencia(retornoTransmissao.RetEnviNFeInfRec.nRec, modelo);

            if (resultadoConsulta == null) return new List<string> { MensagemErro };

            foreach (var resultado in resultadoConsulta)
            {
                var nota = _notaFiscalRepository.GetNotaFiscalByChave(resultado.Chave);

                if (resultado.CodigoStatus == "100")
                {
                    nota.DataAutorizacao = DateTime.ParseExact(resultado.DataAutorizacao, "yyyy-MM-ddTHH:mm:sszzz",
                        CultureInfo.InvariantCulture);
                    nota.Protocolo = resultado.Protocolo;
                    nota.Status = (int)Status.ENVIADA;

                    var xml = await nota.LoadXmlAsync();
                    xml = xml.Replace("<protNFe />", resultado.Xml);

                    _notaFiscalRepository.Salvar(nota, xml);
                }
                else
                {
                    if (resultado.Motivo.Contains("Duplicidade"))
                    {
                        X509Certificate2 certificado = _certificadoService.GetX509Certificate2();
                        var emitente = _emissorService.GetEmissor();

                        var retornoConsulta = _nfeConsulta.ConsultarNotaFiscal
                        (
                            nota.Chave,
                            emitente.Endereco.CodigoUF,
                            certificado,
                            nota.Modelo.Equals("65") ? Modelo.Modelo65 : Modelo.Modelo55
                        );

                        if (retornoConsulta.IsEnviada)
                        {
                            var protSerialized = XmlUtil.Serialize(retornoConsulta.Protocolo, string.Empty)
                                .Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", string.Empty)
                                .Replace("TProtNFe", "protNFe");

                            protSerialized = Regex.Replace(protSerialized, "<infProt (.*?)>", "<infProt>");

                            nota.DataAutorizacao = retornoConsulta.DhAutorizacao;
                            nota.Protocolo = retornoConsulta.Protocolo.infProt.nProt;
                            nota.Status = (int)Status.ENVIADA;

                            var xml = await nota.LoadXmlAsync();
                            xml = xml.Replace("<protNFe />", protSerialized);

                            _notaFiscalRepository.Salvar(nota, xml);
                        }
                        else
                        {
                            erros.Add(
                                $"Modelo: {nota.Modelo} Nota: {nota.Numero} Série: {nota.Serie} \nMotivo: {resultado.Motivo}"); //O que fazer com essas mensagens de erro?
                        }
                    }
                    else
                    {
                        erros.Add(
                            $"Modelo: {nota.Modelo} Nota: {nota.Numero} Série: {nota.Serie} \nMotivo: {resultado.Motivo}"); //O que fazer com essas mensagens de erro?
                    }
                }
            }

            return erros;
        }

        private List<RetornoNotaFiscal> ConsultarReciboLoteContingencia(string nRec, Modelo modelo)
        {
            X509Certificate2 certificado = _certificadoService.GetX509Certificate2();

            var consultaRecibo = new TConsReciNFe
            {
                versao = "4.00",
                tpAmb = _sefazSettings.Ambiente == Ambiente.Producao ? XmlSchemas.NfeRetAutorizacao.Envio.TAmb.Item1 : XmlSchemas.NfeRetAutorizacao.Envio.TAmb.Item2,
                nRec = nRec
            };

            var parametroXml = XmlUtil.Serialize(consultaRecibo, "http://www.portalfiscal.inf.br/nfe");

            var node = new XmlDocument();
            node.LoadXml(parametroXml);

            var codigoUf = (CodigoUfIbge)Enum.Parse(typeof(CodigoUfIbge), _emissorService.GetEmissor().Endereco.UF);

            try
            {
                var servico = _serviceFactory.GetService(modelo, Servico.RetAutorizacao, codigoUf, certificado);
                var client = (NFeRetAutorizacao4SoapClient)servico.SoapClient;

                var result = client.nfeRetAutorizacaoLote(node);

                var retorno = (TRetConsReciNFe)XmlUtil.Deserialize<TRetConsReciNFe>(result.OuterXml);

                return retorno.protNFe.Select(protNFe => new RetornoNotaFiscal
                {
                    Chave = protNFe.infProt.chNFe,
                    CodigoStatus = protNFe.infProt.cStat,
                    DataAutorizacao = protNFe.infProt.dhRecbto,
                    Motivo = protNFe.infProt.xMotivo,
                    Protocolo = protNFe.infProt.nProt,
                    Xml = XmlUtil.Serialize(protNFe, string.Empty)
                            .Replace("<?xml version=\"1.0\" encoding=\"utf-8\"?>", string.Empty)
                            .Replace("TProtNFe", "protNFe")
                            .Replace("<infProt xmlns=\"http://www.portalfiscal.inf.br/nfe\">", "<infProt>")
                }).ToList();
            }
            catch (Exception e)
            {
                log.Error(e);
                if (!_isFirstTimeRecheckingRecipts)
                {
                    _isFirstTimeRecheckingRecipts = true;
                    return ConsultarReciboLoteContingencia(nRec, modelo);
                }

                _isFirstTimeRecheckingRecipts = false;
                return null;
            }
        }

        private MensagemRetornoTransmissaoNotasContingencia TransmitirLoteNotasFiscaisContingencia(List<string> nfeList, Modelo modelo)
        {
            var lote = new TEnviNFe
            {
                idLote = "999999",
                indSinc = TEnviNFeIndSinc.Item0,
                versao = "4.00",
                NFe = new TNFe[1]
            };
            //qual a regra pra gerar o id?
            //apenas uma nota no lote
            lote.NFe[0] = new TNFe(); //Gera tag <NFe /> vazia para usar no replace

            var parametroXml = XmlUtil.Serialize(lote, "http://www.portalfiscal.inf.br/nfe");
            parametroXml = parametroXml.Replace("<NFe />", GerarXmlListaNFe(nfeList))
                .Replace("<motDesICMS>1</motDesICMS>", string.Empty);

            var document = new XmlDocument();
            document.LoadXml(parametroXml);
            var node = document.DocumentElement;

            var codigoUf = (CodigoUfIbge)Enum.Parse(typeof(CodigoUfIbge), _emissorService.GetEmissor().Endereco.UF);
            X509Certificate2 certificado = _certificadoService.GetX509Certificate2();

            try
            {
                var servico = _serviceFactory.GetService(modelo, Servico.AUTORIZACAO, codigoUf, certificado);
                var client = (NFeAutorizacao4SoapClient)servico.SoapClient;

                var result = client.nfeAutorizacaoLote(node);
                var retorno = (TRetEnviNFe)XmlUtil.Deserialize<TRetEnviNFe>(result.OuterXml);

                return new MensagemRetornoTransmissaoNotasContingencia
                {
                    RetEnviNFeInfRec = (TRetEnviNFeInfRec)retorno.Item,
                    TipoMensagem = TipoMensagem.Sucesso
                };
            }
            catch (Exception e)
            {
                log.Error(e);
                if (!_isFirstTimeResending)
                {
                    _isFirstTimeResending = true;
                    return TransmitirLoteNotasFiscaisContingencia(nfeList, modelo);
                }

                _isFirstTimeResending = false;

                return new MensagemRetornoTransmissaoNotasContingencia
                {
                    TipoMensagem = TipoMensagem.ServicoIndisponivel
                };
            }
        }

        public static string GerarXmlListaNFe(List<string> notasFiscais)
        {
            var notasConcatenadas = new StringBuilder();

            for (var i = 0; i < notasFiscais.Count; i++)
            {
                var nfeProc = new XmlDocument();
                nfeProc.LoadXml(notasFiscais[i]);
                notasConcatenadas.Append(nfeProc.GetElementsByTagName("NFe")[0].OuterXml);
            }

            return notasConcatenadas.ToString();
        }
    }
}