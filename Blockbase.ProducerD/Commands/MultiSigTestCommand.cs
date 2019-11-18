using BlockBase.Domain.Blockchain;
using BlockBase.Domain.Configurations;
using BlockBase.Domain.Eos;
using BlockBase.Network.IO;
using BlockBase.Network.IO.Enums;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using BlockBase.Runtime.Network;
using Blockbase.ProducerD.Commands.Interfaces;
using BlockBase.Utils;
using BlockBase.Utils.Crypto;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static BlockBase.Domain.Protos.NetworkMessageProto.Types;

namespace Blockbase.ProducerD.Commands
{
    public class MultiSigTestCommand : IHelperCommand
    {
        private EosStub EosStub { get; set; }
        private ProducerTestConfigurations ProducerTestConfigurations { get; set; }
        private NetworkConfigurations NetworkConfigurations { get; set; }

        public MultiSigTestCommand(ProducerTestConfigurations producerTestConfigurations, NetworkConfigurations networkConfigurations)
        {
            ProducerTestConfigurations = producerTestConfigurations;
            NetworkConfigurations = networkConfigurations;
        }

        public async Task ExecuteAsync()
        {
            // var transaction = new Transaction();
            // transaction.Timestamp = DateTime.UtcNow;

            // var transaction2 = new Transaction();
            // transaction2.Timestamp = transaction.Timestamp;

            // var testjson = Newtonsoft.Json.JsonConvert.SerializeObject(transaction);
            // var testjson2 = Newtonsoft.Json.JsonConvert.SerializeObject(transaction2);

            // var byteHashInBytes = HashHelper.Sha256Data(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(transaction)));

            // var byteHashed = HashHelper.ByteArrayToFormattedHexaString(HashHelper.Sha256Data(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(transaction))));
            // var byteHashed2 = HashHelper.ByteArrayToFormattedHexaString(HashHelper.Sha256Data(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(transaction2))));

            // var byteHashInBytes2 = HashHelper.FormattedHexaStringToByteArray(byteHashed);

            // var isEqual = byteHashInBytes.SequenceEqual(byteHashInBytes2);

            // var block = new Block();
            // var block2 = new Block();
            // var block3 = new Block();

            // block2.Transactions.Add(transaction);

            // block3.Transactions.Add(transaction);
            // block3.Transactions.Add(transaction2);

            // block.CalculateMerkleRoot();
            // block2.CalculateMerkleRoot();
            // block3.CalculateMerkleRoot();

            // var merkleRoot = block.BlockHeader.MerkleRoot;
            // var merkleRoot2 = block2.BlockHeader.MerkleRoot;
            // var merkleRoot3 = block3.BlockHeader.MerkleRoot;


            var data = new { 
                from = ProducerTestConfigurations.ClientAccountName, 
                to = ProducerTestConfigurations.Producer1Name , 
                quantity = "25.0000 SYS", 
                memo = "test proposal through eos stub" };

            await EosStub.ProposeTransaction(
                "transfer", 
                "eosio.token",
                ProducerTestConfigurations.ClientAccountName,
                ProducerTestConfigurations.Producer1Name,
                data,
                new List<string>(){ProducerTestConfigurations.Producer1Name, ProducerTestConfigurations.Producer2Name, ProducerTestConfigurations.Producer3Name},
                EosMsigConstants.ADD_BLOCK_PROPOSAL_NAME
            );

            await Task.Delay(3000);

            //var proposal = await EosStub.RetrieveProposal(ProducerTestConfigurations.Producer1Name, EosMsigConstants.ADD_BLOCK_PROPOSAL_NAME);

            // await EosStub.ApproveTransaction(
            //     ProducerTestConfigurations.Producer1Name,
            //     EosMsigConstants.ADD_BLOCK_PROPOSAL_NAME,
            //     ProducerTestConfigurations.Producer1Name
            // );

            // var eosStub2 = new EosStub(10, ProducerTestConfigurations.Producer2PrivateKey, NetworkConfigurations.EosNet);

            // await eosStub2.ApproveTransaction(
            //     ProducerTestConfigurations.Producer1Name,
            //     EosMsigConstants.ADD_BLOCK_PROPOSAL_NAME,
            //     ProducerTestConfigurations.Producer2Name
            // );

            // var approvals = await EosStub.RetrieveApprovals(ProducerTestConfigurations.Producer1Name);

            // foreach(var approval in approvals) Console.WriteLine($"Approval name: {approval.ProposalName}");

            // await EosStub.ExecuteTransaction(
            //     ProducerTestConfigurations.Producer1Name,
            //     EosMsigConstants.ADD_BLOCK_PROPOSAL_NAME,
            //     ProducerTestConfigurations.Producer1Name
            // );

            // await EosStub.CancelTransaction(
            //     ProducerTestConfigurations.Producer1Name,
            //     EosMsigConstants.ADD_BLOCK_PROPOSAL_NAME
            // );
        }

        public string GetCommandHelp()
        {
            return "testmultisig";
        }

        public bool TryParseCommand(string commandStr)
        {
            var commandData = commandStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (commandData.Length != 1) return false;
            if (commandData[0] != "testmultisig") return false;

            EosStub = new EosStub(10, ProducerTestConfigurations.Producer1PrivateKey, NetworkConfigurations.EosNet);

            return true;
        }
    }
}