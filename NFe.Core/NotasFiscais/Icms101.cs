﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NFe.Core
{
    public class Icms101 : IcmsSn
    {
        public Icms101(string cst, OrigemMercadoria origem) : base(cst, origem)
        {
        }
    }
}