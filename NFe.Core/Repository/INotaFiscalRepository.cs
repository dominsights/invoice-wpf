﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using NFe.Core.Entitities;
using NFe.Core.Domain;
using NFe.Core.NotasFiscais.Sefaz.NfeAutorizacao;

namespace NFe.Core.Interfaces
{
    public interface INotaFiscalRepository
    {
        int Salvar(NotaFiscalEntity notafiscal);
        int Salvar(Domain.NotaFiscal notaFiscal, string xml);
        List<NotaFiscalEntity> GetAll();
        NotaFiscalEntity GetNotaFiscalById(int idNotaFiscalDb, bool isLoadXmlData);
        NotaFiscalEntity GetNotaFiscalByChave(string chave);
        List<NotaFiscalEntity> GetNotasContingencia();
        NotaFiscalEntity GetPrimeiraNotaEmitidaEmContingencia(DateTime dataHoraContingencia, DateTime now);
        NotaFiscalEntity GetNota(string numero, string serie, string modelo);
        Task<List<NotaFiscalEntity>> TakeAsync(int quantity, int page);
        void ExcluirNota(string chave);
        int SalvarNotaFiscalPendente(Domain.NotaFiscal notaFiscal, string v);
        List<NotaFiscalEntity> GetNotasFiscaisPorMesAno(DateTime periodo, bool v);
        int Salvar(NotaFiscalEntity notaFiscalEntity, string p);
        Domain.NotaFiscal GetNotaFiscalFromNfeProcXml(string xml);
        List<Domain.NotaFiscal> GetNotasFiscaisPorPeriodo(DateTime periodoInicial, DateTime dateTime, bool v);
        List<NotaFiscalEntity> GetNotasPendentes(bool isLoadXmlData);
        Task<List<NotaFiscalEntity>> GetNotasPendentesAsync(bool isLoadXmlData);
        void SalvarXmlNFeComErro(Domain.NotaFiscal notaFiscal, XmlNode node);
    }
}