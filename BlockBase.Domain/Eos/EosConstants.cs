using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BlockBase.Domain.Eos
{
    public class EosTableNames
    {
        public static readonly string IP_ADDRESS_TABLE_NAME = "ipaddress";
        public static readonly string CONTRACT_STATE_TABLE_NAME = "contractst";
        public static readonly string CANDIDATES_TABLE_NAME = "candidates";
        public static readonly string PRODUCERS_TABLE_NAME = "producers";
        public static readonly string CONTRACT_INFO_TABLE_NAME = "contractinfo";
        public static readonly string BLOCKHEADERS_TABLE_NAME = "blockheaders";
        public static readonly string BLOCKCOUNT_TABLE_NAME = "blockscount";
        public static readonly string CURRENT_PRODUCER_TABLE_NAME = "currentprod";
        public static readonly string CLIENT_TABLE_NAME = "client";
        public static readonly string TOKEN_LEDGER_TABLE_NAME = "ledger"; // alterado porque no meu smart contract a tabela tem nome de ledger nao de ledgers
        public static readonly string TOKEN_TABLE_NAME = "accounts";
        public static readonly string PENDING_REWARD_TABLE = "rewards";
        public static readonly string BLACKLIST_TABLE = "blacklist";
        
    }    
    
    public class EosMsigConstants
    {
        public static readonly string EOSIO_MSIG_ACCOUNT_NAME = "eosio.msig";
        public static readonly string EOSIO_MSIG_PROPOSE_ACTION = "propose";
        public static readonly string EOSIO_MSIG_APPROVE_ACTION = "approve";
        public static readonly string EOSIO_MSIG_EXEC_ACTION = "exec";
        public static readonly string EOSIO_MSIG_CANCEL_ACTION = "cancel";
        public static readonly string EOSIO_MSIG_APPROVALS_TABLE_NAME = "approvals";
        public static readonly string EOSIO_MSIG_PROPOSAL_TABLE_NAME = "proposal";
        public static readonly string ADD_BLOCK_PROPOSAL_NAME = "bbaddblock";
        public static readonly string VERIFY_BLOCK_PERMISSION = "verifyblock";
    }

    public class EosMethodNames
    {
        public static readonly string START_CHAIN = "startchain";
        public static readonly string END_CHAIN = "endservice";
        public static readonly string CONFIG_CHAIN = "configchain";
        public static readonly string START_CANDIDATURE_TIME = "startcandtime";
        public static readonly string START_SEND_TIME = "startsendtime";
        public static readonly string START_RECEIVE_TIME = "startrectime";
        public static readonly string START_SECRET_TIME = "secrettime";
        public static readonly string CHANGE_CURRENT_PRODUCER = "changecprod";
        public static readonly string PRODUTION_TIME = "prodtime";
        public static readonly string RESET_CHAIN = "resetchain";
        public static readonly string ADD_CANDIDATE = "addcandidate";
        public static readonly string ADD_SECRET = "addsecret";
        public static readonly string ADD_ENCRYPTED_IP = "addencryptip";
        public static readonly string ADD_BLOCK = "addblock";
        public static readonly string VERIFY_BLOCK = "verifyblock";
        public static readonly string ADD_STAKE = "addstake";
        public static readonly string I_AM_READY = "iamready";
        public static readonly string CLAIM_REWARD = "claimreward";
        public static readonly string EXIT_REQUEST = "exitrequest";
        public static readonly string LINKAUTH = "linkauth";
        public static readonly string UPDATEAUTH = "updateauth";
    }

    public class EosParameterNames
    {
        public static readonly string OWNER = "owner";
        public static readonly string CANDIDATE = "candidate";
        public static readonly string PRODUCER = "producer";
        public const string NAME = "name";
        public static readonly string KEY = "key";
        public static readonly string SIDECHAIN = "sidechain";
        public static readonly string CONFIG_INFO_JSON = "infojson";
        public static readonly string CANDIDATURE_END_DATE = "candidatureenddate";
        public static readonly string SECRET_END_DATE = "secretenddate";
        public static readonly string SEND_END_DATE = "ipsendenddate";
        public static readonly string RECEIVE_END_DATE = "ipreceiveenddate";
        public static readonly string BLOCK_TIME_DURATION = "blocktimeduration";
        public static readonly string SIZE_OF_BLOCK_IN_BYTES = "sizeofblockinbytes";
        public static readonly string MINIMUM_CANDIDATE_STAKE = "minimumcandidatestake";
        public static readonly string STAKE = "stake";
        public static readonly string PUBLIC_KEY = "publickey";
        public static readonly string PAYMENT = "paymentperblock";
        public static readonly string PRODUCERS_NUMBER = "requirednumberofproducers";
        public static readonly string CANDIDATURE_TIME = "candidaturetime";
        public static readonly string SECRET_TIME = "sendsecrettime";
        public static readonly string SECRET_HASH = "secrethash";
        public static readonly string SECRET = "secret";
        public static readonly string SEND_TIME = "ipsendtime";
        public static readonly string RECEIVE_TIME = "ipreceivetime";
        public static readonly string BLOCKS_BETWEEN_SETTLEMENT = "blocksbetweensettlement";
        public static readonly string WORK_TIME_IN_SECONDS = "worktimeinseconds";
        public const string BLOCK = "block";
        public static readonly string BLOCK_HASH = "blockhash";
        public static readonly string PREVIOUS_BLOCK_HASH = "previousblockhash";
        public static readonly string SEQUENCE_NUMBER = "sequencenumber";
        public static readonly string TIMESTAMP = "timestamp";
        public static readonly string NUMBER_OF_TRANSACTIONS = "transactionnumber";
        public static readonly string PRODUCER_SIGNATURE = "producersignature";
        public static readonly string IS_VERIFIED = "isverified";
        public static readonly string IS_LAST_BLOCK = "islastblock";
        public static readonly string MERKLE_TREE_ROOT_HASH = "merkletreeroothash";
        public static readonly string BAD_BEHAVIOUR = "badbehaviour";
        public static readonly string PRODUCER_SIGNATURES = "producersignatures";
        public static readonly string PROPOSER = "proposer";
        public static readonly string PROPOSAL_NAME = "proposal_name";
        public static readonly string EXECUTER = "executer";
        public static readonly string CANCELER = "canceler";
        public static readonly string PERMISSION_LEVEL = "level";
        public static readonly string REQUESTED_PERMISSIONS = "requested";
        public static readonly string TRANSACTION = "trx";
        public static readonly string ENCRYPTED_IPS = "encryptedips";
        public static readonly string PROPOSAL_HASH = "proposal_hash";
        public static readonly string CLAIMER = "claimer";
        public static readonly string CONTRACT = "contract";
        public static readonly string ACCOUNT = "account";
        public static readonly string PERMISSION = "permission";
        public static readonly string PARENT = "parent";
        public static readonly string AUTH = "auth";
        public static readonly string CODE = "code";
        public static readonly string TYPE = "type";
        public static readonly string REQUIREMENT = "requirement";
    }
    public class EosAtributeNames
    {
        public static readonly string STATE_PRODUCTION_TIME = "productiontime";
        public static readonly string BLOCKBASE_TOKEN_ACRONYM = " BBT";
        public static readonly string EOSIO = "eosio";
    }
}