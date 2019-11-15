using System.Collections.Generic;
using System.Threading.Tasks;
using BlockBase.Domain.Configurations;
using BlockBase.Network.History.Pocos;
using BlockBase.Network.Mainchain;
using BlockBase.Network.Mainchain.Pocos;
using Microsoft.Extensions.Options;
using System.IO;
using System.Net;
using BlockBase.Utils.Operation;
using System.Linq;
using BlockBase.Domain.Eos;
using System;
using System.Globalization;

namespace BlockBase.Network.History
{
    public class HistoryService : IHistoryService
    {
        private NetworkConfigurations NetworkConfigurations;
        private NodeConfigurations NodeConfigurations;
        private EosStub EosStub;
        private const int MAX_NUMBER_OF_TRIES = 5;


        public HistoryService(IOptions<NetworkConfigurations> networkConfigurations, IOptions<NodeConfigurations> nodeConfigurations)
        {
            NetworkConfigurations = networkConfigurations.Value;
            NodeConfigurations = nodeConfigurations.Value;
            EosStub = new EosStub(NetworkConfigurations.ConnectionExpirationTimeInSeconds, NodeConfigurations.ActivePrivateKey, NetworkConfigurations.EosNet);
        }

        public async Task<List<Sidechains>> GetSidechainList()
        {
            ActionDB ActionObject = GetActionFromHistoryNode(NetworkConfigurations.BlockBaseOperationsContract);
            List<Sidechains> sidechainList = new List<Sidechains>();
            foreach (var action in ActionObject.ActionList)
            {
                if (action.ActionTrace.ActionInformation.ActionName.Equals(EosMethodNames.START_CHAIN.ToLower()))
                {
                    Sidechains sidechains = new Sidechains();
                    Dictionary<string, bool> contractState = GetContractState(action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount).Result;
                    sidechains.Name = action.ActionTrace.ActionInformation.AuthorizationList.FirstOrDefault().SenderAccount;
                    sidechains.State = contractState.First().Key;
                    sidechains.IsProductionTime = contractState.First().Value;
                    sidechains.CreationDate = action.BlockTime;

                    var contractInformation = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ContractInformationTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.CONTRACT_INFO_TABLE_NAME, action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount), MAX_NUMBER_OF_TRIES);
                    if (contractInformation.Count() > 0)
                    {
                        sidechains.Reward = contractInformation.ElementAt(0).Payment + EosAtributeNames.BLOCKBASE_TOKEN_ACRONYM;
                        sidechains.NeededProducers = contractInformation.ElementAt(0).ProducersNumber;
                    }
                    var producersList = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ProducerInTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.PRODUCERS_TABLE_NAME, action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount), MAX_NUMBER_OF_TRIES);
                    sidechains.ActualProducers = Convert.ToUInt32(producersList.Count());

                    var blockHeadersList = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<BlockheaderTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.BLOCKHEADERS_TABLE_NAME, action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount), MAX_NUMBER_OF_TRIES);
                    if (blockHeadersList.Count() > 0)
                    {
                        sidechains.BlockHeader = blockHeadersList.ElementAtOrDefault(0).BlockHash;
                        sidechains.TotalBlocks = blockHeadersList.ElementAtOrDefault(0).SequenceNumber;
                    }
                    sidechainList.Add(sidechains);
                }
            }
            return sidechainList;
        }

        public async Task<List<Producers>> GetProducerList()
        {
            ActionDB actionObject = GetActionFromHistoryNode(NetworkConfigurations.BlockBaseOperationsContract);
            List<Producers> producersModelList = new List<Producers>();
            foreach (var action in actionObject.ActionList)
            {
                if (action.ActionTrace.ActionInformation.ActionName.Equals(EosMethodNames.START_CHAIN.ToLower()))
                {
                    var candidateList = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<CandidateTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.CANDIDATES_TABLE_NAME, action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount), MAX_NUMBER_OF_TRIES);
                    if (candidateList.Count > 0) producersModelList = await FindProducer(candidateList, producersModelList, action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount);

                    var producerList = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ProducerInTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.PRODUCERS_TABLE_NAME, action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount), MAX_NUMBER_OF_TRIES);
                    if (producerList.Count > 0) producersModelList = await FindProducer(producerList, producersModelList, action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount);

                    var blackList = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<BlackListTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.BLACKLIST_TABLE, action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount), MAX_NUMBER_OF_TRIES);
                    if (blackList.Count > 0) producersModelList = await FindProducer(blackList, producersModelList, action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount);
                }
            }
            return producersModelList;
        }

        public async Task<List<ProducerDetail>> GetProducerDetail(string accountName)
        {
            ActionDB ActionObject = GetActionFromHistoryNode(NetworkConfigurations.BlockBaseOperationsContract);
            List<ProducerDetail> producerSidechains = new List<ProducerDetail>();
            foreach (var action in ActionObject.ActionList)
            {
                if (action.ActionTrace.ActionInformation.ActionName.Equals(EosMethodNames.START_CHAIN))
                {
                    var candidateList = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<CandidateTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.CANDIDATES_TABLE_NAME, action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount), MAX_NUMBER_OF_TRIES);
                    var producersList = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ProducerInTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.PRODUCERS_TABLE_NAME, action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount), MAX_NUMBER_OF_TRIES);
                    bool isInCandidateList = candidateList.Any(candidate => candidate.Key == accountName);
                    bool isInProducerList = producersList.Any(producer => producer.Key == accountName);
                    if (isInCandidateList || isInProducerList)
                    {
                        ProducerDetail producerDetail = new ProducerDetail();
                        var contractState = await GetContractState(action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount);
                        producerDetail.SidechainName = action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount;
                        producerDetail.SidechainState = contractState.First().Key;
                        producerDetail.IsSidechainInProduction = contractState.First().Value;
                        var tokenledger = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<TokenLedgerTable>(NetworkConfigurations.BlockBaseTokenContract, EosTableNames.TOKEN_LEDGER_TABLE_NAME, accountName), MAX_NUMBER_OF_TRIES);
                        producerDetail.StakeCommited = tokenledger.Where(b => b.Sidechain == action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount).SingleOrDefault().Stake; // VERIFICAR SE ESTA CORRECTO
                        if (isInCandidateList)
                        {
                            var candidate = candidateList.Single(cand => cand.Key == accountName);
                            producerDetail.ProducerStateInChain = "Waiting";
                            producerDetail.WorkTime = new DateTime(candidate.WorkTimeInSeconds); // CORRIGIR
                            producerDetail.IsRewardAvailable = false;
                        }
                        else
                        {
                            var producer = producersList.Single(prod => prod.Key == accountName);
                            producerDetail.ProducerStateInChain = "Producing";
                            producerDetail.WorkTime = new DateTime(producer.WorkTimeInSeconds); // CORRIGIR
                            var rewardTable = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<PendingRewardTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.PENDING_REWARD_TABLE, action.ActionTrace.ActionInformation.AuthorizationList.ElementAt(0).SenderAccount), MAX_NUMBER_OF_TRIES);

                            if (rewardTable.Count() != 0 && (Convert.ToInt32(rewardTable.Where(b => b.Key == accountName).SingleOrDefault().Reward)) > 0) producerDetail.IsRewardAvailable = true;
                            else producerDetail.IsRewardAvailable = false;
                        }
                        producerSidechains.Add(producerDetail);
                    }
                }
            }
            return producerSidechains;
        }
        
        //TODO TESTAR A VER SE FUNCIONA TUDO CORRECTAMENTE
        //TODO verificar se o contractstate necessita de verificação.
        public async Task<SidechainDetail> GetSidechainDetail(string chainName)
        {
            SidechainDetail sidechainDetail = new SidechainDetail();
            List<ContractInformationTable> contractInformation = new List<ContractInformationTable>();

            contractInformation = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ContractInformationTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.CONTRACT_INFO_TABLE_NAME, chainName), MAX_NUMBER_OF_TRIES);
            if (contractInformation.Count() > 0)
            {
                sidechainDetail.NeededProducerNumber = contractInformation.First().ProducersNumber;
                sidechainDetail.Reward = contractInformation.First().Payment.ToString();
                sidechainDetail.MinCandidateStake = contractInformation.First().Stake.ToString();
                sidechainDetail.BlockThreshold = Convert.ToUInt32(contractInformation.First().BlocksBetweenSettlement / 2) + 1;
            }

            var contractState = await GetContractState(chainName);
            sidechainDetail.State = contractState.First().Key;
            sidechainDetail.IsProducting = contractState.First().Value;

            var blockHeadersList = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<BlockheaderTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.BLOCKHEADERS_TABLE_NAME, chainName), MAX_NUMBER_OF_TRIES);
            if (blockHeadersList.Count() > 0)
            {
                sidechainDetail.CurrentBlockHeaderHash = blockHeadersList.First().BlockHash;
                sidechainDetail.TotalBlocks = blockHeadersList.First().SequenceNumber;
            }
            var producersList = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ProducerInTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.PRODUCERS_TABLE_NAME, chainName), MAX_NUMBER_OF_TRIES);

            if (producersList.Count() > 0)
            {
                sidechainDetail.ActualProducerNumber = Convert.ToUInt32(producersList.Count());
                var totalStake = 0 + EosAtributeNames.BLOCKBASE_TOKEN_ACRONYM;
                foreach (var producer in producersList)
                {
                    var tokenledger = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<TokenLedgerTable>(NetworkConfigurations.BlockBaseTokenContract, EosTableNames.TOKEN_LEDGER_TABLE_NAME, producer.Key), MAX_NUMBER_OF_TRIES);
                    totalStake = AddRewardsTogheter(totalStake, tokenledger.Where(b => b.Owner == producer.Key && b.Sidechain == chainName).SingleOrDefault().Stake);
                }
                sidechainDetail.TotalStake = totalStake;
            }
            var currentProducer = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<CurrentProducerTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.CURRENT_PRODUCER_TABLE_NAME, chainName), MAX_NUMBER_OF_TRIES);
            if (currentProducer.Count() > 0) sidechainDetail.CurrentProducer = currentProducer.First().Producer;
            return sidechainDetail;
        }

        public async Task<List<SidechainBlockHeader>> GetBlockHeaderList(string chainName)
        {
            var blockHeadersTableList = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<BlockheaderTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.BLOCKHEADERS_TABLE_NAME, chainName), MAX_NUMBER_OF_TRIES);
            var blockHeadersList = new List<SidechainBlockHeader>();
            foreach (var blockheader in blockHeadersTableList)
            {
                SidechainBlockHeader blockHeaderToInsert = new SidechainBlockHeader();
                blockHeaderToInsert.BlockHash = blockheader.BlockHash;
                blockHeaderToInsert.PreviousBlockHash = blockheader.PreviousBlockHash;
                blockHeaderToInsert.TransactionsNumber = 123; // temporario visto que temos de adicionar um campo ao SC.
                blockHeaderToInsert.BlockNumber = blockheader.SequenceNumber;
                blockHeaderToInsert.Producer = blockheader.Producer;
                blockHeaderToInsert.CreationDate = new DateTime(blockheader.Timestamp);
                blockHeadersList.Add(blockHeaderToInsert);
            }
            return blockHeadersList;
        }

        //TODO Depois da passagem do metodo para adicionar bad actors para uma ação, finalizar o blocknumber o blockheader e a data.
        //TODO Testar se funciona com os novos campos da ActionDB(Data), porque ainda nao testei.
        //TODO Testar restante, visto que nao consegui testar tudo e rever smart contract para verificar se esta tudo bem implementado para poder testar o blacklist
        public async Task<List<SidechainBlackList>> GetBlackLists(string chainName)
        {
            var blackTableList = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<SidechainBlackList>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.BLACKLIST_TABLE, chainName), MAX_NUMBER_OF_TRIES);
            var blackList = new List<SidechainBlackList>();
            foreach (var badActor in blackTableList)
            {
                SidechainBlackList badActorToInsert = new SidechainBlackList();
                badActorToInsert.ProducerName = badActor.ProducerName;
                badActorToInsert.Date = new DateTime();
                badActorToInsert.BlockHeader = "jas2B3s78aF21we1";
                badActorToInsert.BlockNumber = 312;
                ActionDB ActionObject = GetActionFromHistoryNode(NetworkConfigurations.BlockBaseOperationsContract);
                foreach (var action in ActionObject.ActionList)
                {
                    if (action.ActionTrace.ActionInformation.ActionName.Equals(EosMethodNames.ADD_STAKE))
                    {
                        if (action.ActionTrace.ActionInformation.Data.sidechain == chainName && action.ActionTrace.ActionInformation.Data.owner == badActor.ProducerName)
                        {
                            badActorToInsert.StakeLost = action.ActionTrace.ActionInformation.Data.stake;
                            break;
                        }
                    }
                }
                blackTableList.Add(badActorToInsert);
            }
            return blackTableList;
        }

        //TODO Depois da implementacao do scrapper passar o date para o tempo de inicio visto que vamos seguir o add block, e tambem verificar como implementar o warning
        public async Task<List<SidechainProducer>> GetSidechainProducerList(string chainName)
        {
            List<SidechainProducer> producersInSidechainList = new List<SidechainProducer>();
            var producersList = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ProducerInTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.PRODUCERS_TABLE_NAME, chainName), MAX_NUMBER_OF_TRIES);
            foreach (var producer in producersList)
            {
                SidechainProducer producerInSidechain = new SidechainProducer();
                producerInSidechain.ProducerName = producer.Key;
                producerInSidechain.WorkTime = producer.WorkTimeInSeconds;
                producerInSidechain.BlocksProduced = 1212;
                var warning = "Clear";
                if (producer.Warning == 1) warning = "Flagged";
                producerInSidechain.Warning = warning;
                var tokenledger = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<TokenLedgerTable>(NetworkConfigurations.BlockBaseTokenContract, EosTableNames.TOKEN_LEDGER_TABLE_NAME, producer.Key), MAX_NUMBER_OF_TRIES);
                producerInSidechain.StakeCommited = tokenledger.Where(b => b.Sidechain == chainName).SingleOrDefault().Stake; // VERIFICAR SE ESTA CORRECTO
                producerInSidechain.Date = new DateTime();
                producersInSidechainList.Add(producerInSidechain);
            }
            return producersInSidechainList;
        }

        private ActionDB GetActionFromHistoryNode(string accountName)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(NetworkConfigurations.HistoryNodeEndPoint);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                var jsonBody = new { account_name = accountName };
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonBody);
                streamWriter.Write(json);
            }
            var response = (HttpWebResponse)httpWebRequest.GetResponse();
            ActionDB ActionObject;
            using (var streamReader = new StreamReader(response.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                ActionObject = Newtonsoft.Json.JsonConvert.DeserializeObject<ActionDB>(result);
            }
            return ActionObject;
        }

        private async Task<Dictionary<string, bool>> GetContractState(string chainName)
        {
            var contractStateRequest = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<ContractStateTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.CONTRACT_STATE_TABLE_NAME, chainName), MAX_NUMBER_OF_TRIES);
            var contractState = contractStateRequest.SingleOrDefault();
            string stateString = "";
            bool isProductionTime = false;
            Dictionary<string, bool> contractStateDictionary = new Dictionary<string, bool>();
            for (int i = 0; i < contractState.GetType().GetProperties().Count(); i++)
            {
                if (contractState.GetType().GetProperties().ElementAt(i).GetValue(contractState).Equals(true))
                {
                    if (contractState.GetType().GetProperties().ElementAt(i).Name.ToLower() == EosAtributeNames.STATE_PRODUCTION_TIME)
                    {
                        isProductionTime = true;
                        if (!stateString.Equals("") && !stateString.ToLower().Equals(EosMethodNames.START_CHAIN)) break;
                    }
                    stateString = contractState.GetType().GetProperties().ElementAt(i).Name;
                }
            }
            contractStateDictionary.Add(stateString, isProductionTime);
            return contractStateDictionary;
        }

        //TODO Quando resolver o scrapping das ações adicionar à logica os campos last activity e membersince.
        public async Task<List<Producers>> FindProducer<T>(IList<T> list, List<Producers> prodList, string listSidechainOwner)
        {
            var propertyKey = typeof(T).GetProperty(EosParameterNames.KEY);
            foreach (var participant in list)
            {
                var isInList = false;
                var participantName = participant.GetType().GetProperties().ElementAt(0).GetValue(participant);
                if (prodList.Count() >= 0)
                {
                    var producer = new Producers();
                    if (prodList.Any(prod => prod.Name == participantName.ToString()))
                    {
                        producer = prodList.Single(prod => prod.Name == participantName.ToString());
                        producer.TotalSidechains++;
                        isInList = true;
                    }
                    else
                    {
                        producer.Name = participantName.ToString();
                        producer.TotalSidechains = 1;
                        producer.MemberSince = new DateTime();
                        producer.LastActivity = new DateTime();
                        producer.TotalReward = 0 + EosAtributeNames.BLOCKBASE_TOKEN_ACRONYM;
                        producer.ActiveSidechains = 0;
                        producer.NumberOfBlackLists = 0;
                    }
                    if (typeof(T) == typeof(ProducerInTable))
                    {
                        producer.ActiveSidechains++;
                        var rewardTable = await Repeater.TryAgain(async () => await EosStub.GetRowsFromSmartContractTable<PendingRewardTable>(NetworkConfigurations.BlockBaseOperationsContract, EosTableNames.PENDING_REWARD_TABLE, listSidechainOwner), MAX_NUMBER_OF_TRIES);
                        if (rewardTable.Any(rwd => rwd.Key == participantName.ToString()))
                        {
                            var reward = rewardTable.Single(rwd => rwd.Key == participantName.ToString());
                            producer.TotalReward = AddRewardsTogheter(reward.Reward + EosAtributeNames.BLOCKBASE_TOKEN_ACRONYM, producer.TotalReward);
                        }
                        else
                        {
                            producer.TotalReward = 0 + EosAtributeNames.BLOCKBASE_TOKEN_ACRONYM;
                        }
                    }
                    else if (typeof(T) == typeof(BlackListTable)) { producer.NumberOfBlackLists++; }
                    if (!isInList) prodList.Add(producer);
                }
            }
            return prodList;
        }

        //TODO temporario, verificar melhor sistema de rewards e verificar se é assim ou necessita mudança
        private string AddRewardsTogheter(string rewardToAdd, string rewardToBeAdded)
        {
            string[] rewardToBeAddedSplit = rewardToBeAdded.Split(' ');
            string[] rewardToAddSplit = rewardToAdd.Split(' ');
            var rewardAdded = Double.Parse(rewardToAddSplit[0]) + Double.Parse(rewardToBeAddedSplit[0]);
            return rewardAdded + EosAtributeNames.BLOCKBASE_TOKEN_ACRONYM;
        }
    }
}
