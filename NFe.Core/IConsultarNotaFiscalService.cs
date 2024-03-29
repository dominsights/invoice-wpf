﻿using System;
using System.Security.Cryptography.X509Certificates;
using NFe.Core.Domain;
using NFe.Core.XmlSchemas.NfeConsulta2.Retorno;

namespace NFe.Core.NotasFiscais.Sefaz.NfeConsulta2
{
    public struct MensagemRetornoConsulta
    {
        public bool IsEnviada { get; set; }
        public DateTime DhAutorizacao { get; set; }
        public TProtNFe Protocolo { get; set; }
    }

    public interface IConsultarNotaFiscalService
    {
        MensagemRetornoConsulta ConsultarNotaFiscal(string chave, string codigoUf, X509Certificate2 certificado, Modelo modelo);
    }
}