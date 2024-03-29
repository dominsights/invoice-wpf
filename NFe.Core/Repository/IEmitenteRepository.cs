﻿using System.Collections.Generic;
using NFe.Core.Domain;
using NFe.Core.Entitities;

namespace NFe.Core.Interfaces
{
    public interface IEmitenteRepository
    {
        int Salvar(EmitenteEntity emitente);
        void Excluir(EmitenteEntity emitente);
        List<EmitenteEntity> GetAll();
        EmitenteEntity GetEmitente();
        Emissor GetEmissor();
        EmitenteEntity GetEmitenteEntity();
    }
}