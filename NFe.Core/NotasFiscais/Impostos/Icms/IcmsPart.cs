﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFe.Core
{
    public class IcmsPartilha : Icms
    {
        public IcmsPartilha(CstEnum cst, OrigemMercadoria origem, decimal aliquota) : base(cst, origem, aliquota)
        {
        }
    }
}