﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NFe.Core;
using NFe.Core.Entitities;
using NFe.Core.Interfaces;

namespace NFe.Repository.Repositories
{
    public class NotaInutilizadaRepository : INotaInutilizadaRepository
    {
        private NFeContext _context;

        public NotaInutilizadaRepository()
        {
            _context = new NFeContext();
        }

        public int Salvar(NotaInutilizadaEntity notaInutilizada)
        {
            _context.NotaInutilizada.Add(notaInutilizada);
            _context.SaveChanges();
            return notaInutilizada.Id;
        }

        public IQueryable<NotaInutilizadaEntity> GetNotasFiscaisPorMesAno(DateTime mesAno)
        {
            return _context.NotaInutilizada.Where(n => n.DataInutilizacao.Month.Equals(mesAno.Month) && n.DataInutilizacao.Year == mesAno.Year);
        }

        public NotaInutilizadaEntity GetNotaInutilizada(string idInutilizacao)
        {
            return _context.NotaInutilizada.FirstOrDefault(n => n.IdInutilizacao == idInutilizacao);
        }

        public virtual void Insert(NotaInutilizadaEntity novaNotaInutilizada)
        {
            bool isNotaInutilizadaJáExiste = GetNotaInutilizada(novaNotaInutilizada.IdInutilizacao) != null;

            if (!isNotaInutilizadaJáExiste)
            {
                _context.NotaInutilizada.Add(novaNotaInutilizada);
            }
        }

        public virtual void Save()
        {
            _context.SaveChanges();
        }
    }
}
