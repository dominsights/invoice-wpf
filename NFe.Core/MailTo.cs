﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace NFe.Core
{
    public class MailTo
    {
        public MailTo(string account, string name)
        {
            Address = new MailAddress(account, name);
        }

        public MailAddress Address { get; }
    }
}