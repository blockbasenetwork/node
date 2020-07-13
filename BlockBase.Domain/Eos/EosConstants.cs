using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BlockBase.Domain.Eos
{
    public class EosTableNames
    {
        public const string IP_ADDRESS_TABLE_NAME = "ipaddress";
        public const string CONTRACT_STATE_TABLE_NAME = "contractst";
        public const string CANDIDATES_TABLE_NAME = "candidates";
        public const string PRODUCERS_TABLE_NAME = "producers";
        public const string CONTRACT_INFO_TABLE_NAME = "contractinfo";
        public const string VERSION_TABLE_NAME = "version";
        public const string BLOCKHEADERS_TABLE_NAME = "blockheaders";
        public const string BLOCKCOUNT_TABLE_NAME = "blockscount";
        public const string CURRENT_PRODUCER_TABLE_NAME = "currentprod";
        public const string CLIENT_TABLE_NAME = "client";
        public const string TOKEN_LEDGER_TABLE_NAME = "ledger";
        public const string TOKEN_TABLE_NAME = "accounts";
        public const string PENDING_REWARD_TABLE = "rewards";
        public const string BLACKLIST_TABLE = "blacklist";
        public const string HISTORY_VALIDATION_TABLE = "histval";
        public const string VERIFY_SIGNATURE_TABLE = "verifysig";
        public const string RESERVED_SEATS_TABLE = "reservedseat";
        public const string WARNING_TABLE = "warnings";
    }

    public class EosTableValues
    {
        public const int WARNING_PUNISH = 0;
        public const int WARNING_BLOCKS_FAILED = 1;
        public const int WARNING_HISTORY_VALIDATION_FAILED = 2;
    }

    public class EosMsigConstants
    {
        public const string EOSIO_MSIG_ACCOUNT_NAME = "eosio.msig";
        public const string EOSIO_MSIG_PROPOSE_ACTION = "propose";
        public const string EOSIO_MSIG_APPROVE_ACTION = "approve";
        public const string EOSIO_MSIG_EXEC_ACTION = "exec";
        public const string EOSIO_MSIG_CANCEL_ACTION = "cancel";
        public const string EOSIO_MSIG_APPROVALS_TABLE_NAME = "approvals2";
        public const string EOSIO_MSIG_PROPOSAL_TABLE_NAME = "proposal";
        public const string ADD_BLOCK_PROPOSAL_NAME = "bbaddblock";
        public const string VERIFY_BLOCK_PERMISSION = "verifyblock";
        public const string VERIFY_HISTORY_PERMISSION = "verifyhist";
    }

    public class EosMethodNames
    {
        public const string START_CHAIN = "startchain";
        public const string END_CHAIN = "endservice";
        public const string CONFIG_CHAIN = "configchain";
        public const string START_CANDIDATURE_TIME = "startcandtime";
        public const string START_SEND_TIME = "startsendtime";
        public const string START_RECEIVE_TIME = "startrectime";
        public const string START_SECRET_TIME = "secrettime";
        public const string CHANGE_CURRENT_PRODUCER = "changecprod";
        public const string PRODUCTION_TIME = "startprodtime";
        public const string ADD_CANDIDATE = "addcandidate";
        public const string ADD_SECRET = "addsecret";
        public const string ADD_ENCRYPTED_IP = "addencryptip";
        public const string ADD_BLOCK = "addblock";
        public const string VERIFY_BLOCK = "verifyblock";
        public const string ADD_STAKE = "addstake";
        public const string CLAIM_STAKE = "claimstake";
        public const string I_AM_READY = "iamready";
        public const string CLAIM_REWARD = "claimreward";
        public const string EXIT_REQUEST = "exitrequest";
        public const string LINKAUTH = "linkauth";
        public const string UPDATEAUTH = "updateauth";
        public const string PUNISH_PRODUCERS = "prodpunish";
        public const string BLACKLIST_PRODUCERS = "blacklistprod";
        public const string REQUEST_HISTORY_VALIDATION = "reqhistval";
        public const string HISTORY_VALIDATE = "histvalidate";
        public const string ADD_BLOCK_BYTE = "addblckbyte";
        public const string ADD_VERIFY_SIGNATURE = "addversig";
        public const string ADD_HIST_SIG = "addhistsig";
        public const string UNLINK_AUTH = "unlinkauth";
        public const string DELETE_AUTH = "deleteauth";
        public const string REMOVE_CANDIDATE = "rcandidate";
        public const string REMOVE_BLACKLISTED = "removeblisted";
        public const string ADD_RESERVED_SEATS = "addreseats";
        public const string REMOVE_RESERVED_SEATS = "rreservseats";
    }

    public class EosParameterNames
    {
        public const string OWNER = "owner";
        public const string CANDIDATE = "candidate";
        public const string PRODUCER = "producer";
        public const string NAME = "name";
        public const string CONFIG_INFO_JSON = "infoJson";
        public const string RESERVED_SEATS = "reservedSeats";
        public const string SEATS_TO_ADD = "seatsToAdd";
        public const string SEATS_TO_REMOVE = "seatsToRemove";
        public const string PUBLIC_KEY = "publicKey";
        public const string SECRET_HASH = "secretHash";
        public const string PRODUCER_TYPE = "producerType";
        public const string PRODUCER_TO_VALIDATE = "producerToValidade";
        public const string SECRET = "secret";
        public const string WORK_TIME_IN_SECONDS = "workDurationInSeconds";
        public const string BLOCK = "block";
        public const string BLOCK_HASH = "blockHash";
        public const string PROPOSER = "proposer";
        public const string PROPOSAL_NAME = "proposal_name";
        public const string EXECUTER = "executer";
        public const string CANCELER = "canceler";
        public const string PERMISSION_LEVEL = "level";
        public const string REQUESTED_PERMISSIONS = "requested";
        public const string TRANSACTION = "trx";
        public const string ENCRYPTED_IPS = "encryptedIps";
        public const string PROPOSAL_HASH = "proposal_hash";
        public const string CLAIMER = "claimer";
        public const string CONTRACT = "contract";
        public const string ACCOUNT = "account";
        public const string PERMISSION = "permission";
        public const string PARENT = "parent";
        public const string AUTH = "auth";
        public const string CODE = "code";
        public const string TYPE = "type";
        public const string REQUIREMENT = "requirement";
        public const string SIDECHAIN = "sidechain";
        public const string STAKE = "stake";
        public const string BYTE_INDEX = "byteIndex";
        public const string BYTE_IN_HEXADECIMAL = "byteInHex";
        public const string PACKED_TRANSACTION = "packedTransaction";
        public const string VERIFY_SIGNATURE = "verifySignature";
        public const string SOFTWARE_VERSION = "softwareVersion";
    }

    public class EosAtributeNames
    {
        public const string EOSIO = "eosio";
        public const string KEY = "key";
        public const string BLOCKS_FAILED = "num_blocks_failed";
        public const string BLOCKS_PRODUCED = "num_blocks_produced";
        public const string BLOCK_HASH = "block_hash";
        public const string SOFTWARE_VERSION = "software_version";
        public const string PREVIOUS_BLOCK_HASH = "previous_block_hash";
        public const string SEQUENCE_NUMBER = "sequence_number";
        public const string TIMESTAMP = "timestamp";
        public const string TRANSACTIONS_COUNT = "transactions_count";
        public const string LAST_TRANSACTION_SEQUENCE_NUMBER = "last_trx_sequence_number";
        public const string PRODUCER_SIGNATURE = "producer_signature";
        public const string MERKLETREE_ROOT_HASH = "merkletree_root_hash";
        public const string IS_VERIFIED = "is_verified";
        public const string IS_LATEST_BLOCK = "is_latest_block";
        public const string PRODUCER = "producer";
        public const string PUBLIC_KEY = "public_key";
        public const string STAKE = "stake";
        public const string SECRET_HASH = "secret_hash";
        public const string SECRET = "secret";
        public const string WORK_DURATION_IN_SECONDS = "work_duration_in_seconds";
        public const string PRODUCER_TYPE = "producer_type";
        public const string MAX_PAYMENT_PER_BLOCK_VALIDATOR_PRODUCERS = "max_payment_per_block_validator_producers";
        public const string MAX_PAYMENT_PER_BLOCK_HISTORY_PRODUCERS = "max_payment_per_block_history_producers";
        public const string MAX_PAYMENT_PER_BLOCK_FULL_PRODUCERS = "max_payment_per_block_full_producers";
        public const string MIN_PAYMENT_PER_BLOCK_VALIDATOR_PRODUCERS = "min_payment_per_block_validator_producers";
        public const string MIN_PAYMENT_PER_BLOCK_HISTORY_PRODUCERS = "min_payment_per_block_history_producers";
        public const string MIN_PAYMENT_PER_BLOCK_FULL_PRODUCERS = "min_payment_per_block_full_producers";
        public const string MIN_CANDIDATURE_STAKE = "min_candidature_stake";
        public const string NUMBER_OF_VALIDATOR_PRODUCERS_REQUIRED = "number_of_validator_producers_required";
        public const string NUMBER_OF_HISTORY_PRODUCERS_REQUIRED = "number_of_history_producers_required";
        public const string NUMBER_OF_FULL_PRODUCERS_REQUIRED = "number_of_full_producers_required";
        public const string CANDIDATURE_PHASE_END_DATE_IN_SECONDS = "candidature_phase_end_date_in_seconds";
        public const string SECRET_SENDING_PHASE_END_DATE_IN_SECONDS = "secret_sending_phase_end_date_in_seconds";
        public const string IP_SENDING_PHASE_END_DATE_IN_SECONDS = "ip_sending_phase_end_date_in_seconds";
        public const string IP_RETRIEVAL_PHASE_END_DATE_IN_SECONDS = "ip_retrieval_phase_end_date_in_seconds";
        public const string CANDIDATURE_PHASE_DURATION_IN_SECONDS = "candidature_phase_duration_in_seconds";
        public const string IP_SENDING_PHASE_DURATION_IN_SECONDS = "ip_sending_phase_duration_in_seconds";
        public const string IP_RETRIEVAL_PHASE_DURATION_IN_SECONDS = "ip_retrieval_phase_duration_in_seconds";
        public const string SECRET_SENDING_PHASE_DURATION_IN_SECONDS = "secret_sending_phase_duration_in_seconds";
        public const string BLOCK_TIME_IN_SECONDS = "block_time_in_seconds";
        public const string NUM_BLOCKS_BETWEEN_SETTLEMENTS = "num_blocks_between_settlements";
        public const string BLOCK_SIZE_IN_BYTES = "block_size_in_bytes";
        public const string HAS_CHAIN_STARTED = "has_chain_started";
        public const string IS_CONFIGURATION_PHASE = "is_configuration_phase";
        public const string IS_CANDIDATURE_PHASE = "is_candidature_phase";
        public const string IS_SECRET_SENDING_PHASE = "is_secret_sending_phase";
        public const string IS_IP_SENDING_PHASE = "is_ip_sending_phase";
        public const string IS_IP_RETRIEVING_PHASE = "is_ip_retrieving_phase";
        public const string IS_PRODUCTION_PHASE = "is_production_phase";
        public const string IS_READY_TO_PRODUCE = "is_ready_to_produce";
        public const string PRODUCTION_START_DATE_IN_SECONDS = "production_start_date_in_seconds";
        public const string HAS_PRODUCED_BLOCK = "has_produced_block";
        public const string ENCRYPTED_IPS = "encrypted_ips";
        public const string REWARD = "reward";
        public const string WARNING_TYPE = "warning_type";
        public const string SIDECHAIN_START_DATE_IN_SECONDS = "sidechain_start_date_in_seconds";
        public const string SIDECHAIN_CREATION_TIMESTAMP = "sidechain_creation_timestamp";
        public const string PRODUCER_STAKE = "producerstake";
        public const string BALANCE = "balance";
        public const string OWNER = "owner";
        public const string SIDECHAIN = "sidechain";
        public const string PROPOSAL_NAME = "proposal_name";
        public const string REQUESTED_APPROVALS = "requested_approvals";
        public const string PROVIDED_APPROVALS = "provided_approvals";
        public const string PACKED_TRANSACTION = "packed_transaction";
        public const string TIME = "time";
        public const string LEVEL = "level";
        public const string BLOCK_BYTE_IN_HEXADECIMAL = "block_byte_in_hex";
        public const string VERIFY_SIGNATURE = "verify_signature";
        public const string VERIFY_SIGNATURES = "verify_signatures";
        public const string SIGNED_PRODUCERS = "signed_producers";
        public const string WARNING_CREATION_DATE_IN_SECONDS = "warning_creation_date_in_seconds";
        public const string PRODUCER_EXIT_DATE_IN_SECONDS = "producer_exit_date_in_seconds";
    }

    public class EosErrors
    {
        public const string ALREADY_LINKED_AUTH_ERROR = "new requirement is same as old";
    }

    public class EosNetworkIds
    {
        public const string MAINNET_ID = "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906";
        public const string JUNGLE_ID = "e70aaab8997e1dfce58fbfac80cbbb8fecec7b99cf982a9444273cbc64c41473";
    }

    public class EosNetworkNames
    {
        public const string MAINNET = "Mainnet";
        public const string JUNGLE = "Jungle";
        public static string GetNetworkName(string networkId)
        {
            switch(networkId)
            {
                case EosNetworkIds.JUNGLE_ID:
                    return JUNGLE;
                case EosNetworkIds.MAINNET_ID:
                    return MAINNET;
                default:
                    return "All";
            }
        }
    }
}