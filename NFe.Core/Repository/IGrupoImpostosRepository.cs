﻿using System.Collections.Generic;
using DgSystems.NFe.Core.Cadastro;
using NFe.Core.Cadastro.Imposto;

namespace NFe.Core.Interfaces
{
    public interface IGrupoImpostosRepository
    {
        int Salvar(GrupoImpostos grupoImpostos);
        void Excluir(GrupoImpostos grupoImpostos);
        List<GrupoImpostos> GetAll();
        GrupoImpostos GetById(int id);
    }
}