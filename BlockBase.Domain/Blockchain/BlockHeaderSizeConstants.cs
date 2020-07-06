namespace BlockBase.Domain.Blockchain
{
    public class BlockHeaderSizeConstants
    {
        public const uint BLOCKHASH_SIZE = 32;
        public const uint MERKLE_ROOT_SIZE = 32;
        public const uint EOS_NAME_MAX_SIZE = 12 * 2;
        public const uint SIGNATURE_SIZE = 7 * 2 + 75;
        public const uint TIMESTAMP_SIZE = 8;
        public const uint SEQUENCE_NUMBER_SIZE = 8;
        public const uint TRANSACTION_COUNT_SIZE = 4;
        public const uint BLOCK_SIZE_IN_BYTES_SIZE = 8;

        public const uint BLOCKHEADER_MAX_SIZE = BLOCKHASH_SIZE + BLOCKHASH_SIZE + EOS_NAME_MAX_SIZE + SIGNATURE_SIZE + MERKLE_ROOT_SIZE
        + TIMESTAMP_SIZE + SEQUENCE_NUMBER_SIZE + SEQUENCE_NUMBER_SIZE + TRANSACTION_COUNT_SIZE + BLOCK_SIZE_IN_BYTES_SIZE;

    }
}