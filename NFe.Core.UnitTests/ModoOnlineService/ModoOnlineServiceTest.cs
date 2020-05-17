﻿using System;
using Moq;
using NFe.Core.NotasFiscais.Services;
using NFe.Core.Interfaces;
using Xunit;

namespace NFe.Core.UnitTests.ModoOnlineService
{
    public class ModoOnlineServiceTest
    {
        [Fact]
        public void AtivarModoOnline_TransmiteNotasFiscaisEmContingencia()
        {
            var enviaNotaFiscalService = new Mock<IEnviaNotaFiscalFacade>().Object;
            var notaFiscalContingenciaServiceMock = new Mock<IEmiteNotaFiscalContingenciaFacade>();
            IEmiteNotaFiscalContingenciaFacade emiteNotaFiscalContingenciaService = notaFiscalContingenciaServiceMock.Object;
            IConfiguracaoRepository configuracaoRepository = new ConfiguracaoRepositoryFake();
            IConsultaStatusServicoFacade consultaStatusServicoService = new Mock<IConsultaStatusServicoFacade>().Object;
            INotaFiscalRepository notaFiscalRepository = new Mock<INotaFiscalRepository>().Object;

            var modoOnlineService = new NotasFiscais.Services.ModoOnlineService(enviaNotaFiscalService,
                configuracaoRepository, consultaStatusServicoService, notaFiscalRepository,
                emiteNotaFiscalContingenciaService);

            modoOnlineService.AtivarModoOnline();

            notaFiscalContingenciaServiceMock.Verify(n => n.TransmitirNotasFiscalEmContingencia(), Times.Once);
        }

        [Fact]
        public void AtivarModoOnline_AlteraConfiguraçãoIsContingencia()
        {
            // Arrange 

            var notaFiscalServiceMock = new Mock<IEnviaNotaFiscalFacade>();
            IEnviaNotaFiscalFacade enviaNotaFiscalService = notaFiscalServiceMock.Object;
            var notaFiscalContingenciaServiceMock = new Mock<IEmiteNotaFiscalContingenciaFacade>();
            IEmiteNotaFiscalContingenciaFacade emiteNotaFiscalContingenciaService = notaFiscalContingenciaServiceMock.Object;
            IConfiguracaoRepository configuracaoRepository = new ConfiguracaoRepositoryFake();
            IConsultaStatusServicoFacade consultaStatusServicoService = new Mock<IConsultaStatusServicoFacade>().Object;
            INotaFiscalRepository notaFiscalRepository = new Mock<INotaFiscalRepository>().Object;

            var modoOnlineService = new NotasFiscais.Services.ModoOnlineService(enviaNotaFiscalService, configuracaoRepository, consultaStatusServicoService, notaFiscalRepository, emiteNotaFiscalContingenciaService);

            // Act

            var configuracao = configuracaoRepository.GetConfiguracao();
            configuracao.IsContingencia = true;
            configuracaoRepository.Salvar(configuracao);
            Assert.True(configuracaoRepository.GetConfiguracao().IsContingencia);

            modoOnlineService.AtivarModoOnline();

            configuracao = configuracaoRepository.GetConfiguracao();

            // Assert 

            Assert.False(configuracao.IsContingencia);
        }

        [Fact]
        public void AtivarModoOffline_AlteraConfiguraçãoIsContingencia()
        {
            // Arrange 

            var notaFiscalServiceMock = new Mock<IEnviaNotaFiscalFacade>();
            IEnviaNotaFiscalFacade enviaNotaFiscalService = notaFiscalServiceMock.Object;
            IConfiguracaoRepository configuracaoRepository = new ConfiguracaoRepositoryFake();
            IConsultaStatusServicoFacade consultaStatusServicoService = new Mock<IConsultaStatusServicoFacade>().Object;
            INotaFiscalRepository notaFiscalRepository = new Mock<INotaFiscalRepository>().Object;
            var notaFiscalContingenciaServiceMock = new Mock<IEmiteNotaFiscalContingenciaFacade>();
            IEmiteNotaFiscalContingenciaFacade emiteNotaFiscalContingenciaService = notaFiscalContingenciaServiceMock.Object;

            var modoOnlineService = new NotasFiscais.Services.ModoOnlineService(enviaNotaFiscalService, configuracaoRepository, consultaStatusServicoService, notaFiscalRepository, emiteNotaFiscalContingenciaService);

            // Act

            var configuracao = configuracaoRepository.GetConfiguracao();
            configuracao.IsContingencia = false;
            configuracaoRepository.Salvar(configuracao);
            Assert.False(configuracaoRepository.GetConfiguracao().IsContingencia);

            modoOnlineService.AtivarModoOffline("Teste unitário", DateTime.Now);

            configuracao = configuracaoRepository.GetConfiguracao();

            // Assert 

            Assert.True(configuracao.IsContingencia);
        }
    }
}
