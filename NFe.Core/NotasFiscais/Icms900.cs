﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFe.Core
{
    public class Icms900 : IcmsSn
    {
        public Icms900(CstEnum cst, OrigemMercadoria origem) : base(cst, origem)
        {
        }
    }
}