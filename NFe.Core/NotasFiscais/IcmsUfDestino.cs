﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFe.Core
{
    public class IcmsUfDestino : Icms
    {
        public IcmsUfDestino(CstEnum cst, OrigemMercadoria origem) : base(cst, origem)
        {
        }
    }
}