﻿using NFe.Core.XmlSchemas.NfeAutorizacao.Envio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using NFe.Core.Utils;
using DgSystems.NFe.Extensions;

namespace NFe.Core.Utils.Xml
{
    public static class ValidadorXml
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static List<string> _mensagens;

        /** <exception cref="ArgumentException"/>
         * <param name="nomeXsd">Nome do arquivo .xsd usado para validar o xml.</param> 
         * <param name="xml">String em formato xml que deve ser validada.</param> 
         * <summary>Método usado para validar o xml informado de acordo com o esquema.</summary>
         */
        public static void ValidarXml(string xml, string nomeXsd)
        {
            XmlReader validador = null;
            _mensagens = new List<string>();

            try
            {
                string path = Path.Combine(Environment.CurrentDirectory, @"XmlSchemas\XSD\Nota 4.0\" + nomeXsd);

                if(!File.Exists(path))
                {
                    throw new InvalidOperationException("Arquivo do schema xml não existe!");
                }

                var cfg = new XmlReaderSettings { ValidationType = ValidationType.Schema };

                var schemas = new XmlSchemaSet();
                cfg.Schemas = schemas;
                cfg.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
                cfg.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
                cfg.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
                cfg.ValidationEventHandler += NotaSettingsValidationEventHandler;

                schemas.Add(null, path);

                validador = XmlReader.Create(new StringReader(xml), cfg);
                while (validador.Read()) { }

                if (_mensagens.Count > 0)
                {
                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine("O xml informado é inválido de acordo com o esquema fornecido.");

                    foreach (var mensagem in _mensagens)
                    {
                        builder.AppendLine(mensagem);
                    }

                    throw new ArgumentException(builder.ToString());
                }
            }
            catch (XmlException e)
            {
                log.Error(e);
                throw new Exception("Ocorreu o seguinte erro durante a validação XML:" + "\n" + e.Message);
            }
            finally
            {
                validador.Close();
            }
        }

        private static void NotaSettingsValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if (_mensagens == null)
                return;

            if (e.Severity == XmlSeverityType.Warning)
            {
                _mensagens.Add("Alerta: " + e.Message);
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                string texto;
                if (e.Message.Contains("dependendo"))
                {
                    texto = e.Message.Before(" dependendo").Replace("http://www.portalfiscal.inf.br/nfe:", string.Empty).Replace(" -", ".") + ".";
                }
                else
                {
                    texto = e.Message;
                }

                _mensagens.Add("Erro: " + texto);
            }
        }
    }
}
