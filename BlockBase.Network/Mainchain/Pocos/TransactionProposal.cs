using EosSharp;
using EosSharp.Core.Api.v1;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Network.Mainchain.Pocos
{
    public class TransactionProposal
    {
        public string ProposalName { get; set; }

        public Transaction Transaction { get; set; }

        public string TransactionHash { get; set; }
    }
}
