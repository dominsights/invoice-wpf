﻿using MediatR;
using NFe.Core.Domain;
using NFe.Core.Utils.PDF;

namespace NFe.Core
{
    public class ImprimirDanfeCommand : Command
    {
        private readonly IMediator mediator;

        public ImprimirDanfeCommand(NotaFiscal notaFiscal, IMediator mediator)
        {
            NotaFiscal = notaFiscal;
            this.mediator = mediator;
        }

        public NotaFiscal NotaFiscal { get; }

        public override async void ExecuteAsync()
        {
            try
            {
                bool result = await mediator.Send(new ImprimirDanfe(NotaFiscal));
                IsExecuted = result;
            }
            catch
            {
                IsExecuted = false;
            }
        }
    }
}
