﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using BlockBase.Domain.Enums;

namespace BlockBase.Domain
{
    public class ProducerInfo
    {
        public string PublicKey { get; set; }

        public string AccountName { get; set; }

        public IPEndPoint IPEndPoint { get; set; }

        public ProducerTypeEnum ProducerType{ get; set; }

        public bool NewlyJoined { get; set; }

    }
}
