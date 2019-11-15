using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain
{
    enum NetworkMessageTypeEnum
    {
        Unknown,
        RequestInfo,
        RequestProducerIdentification,
        RequestNodesInfo,
        RequestBlocks,
        RequestBlockHeaders,
        RequestTransactions,
        RequestCurrentHeight,
        RequestMaxBlockSize,
        RequestProducers,
        RequestProducersList,

        SendBlock,
        SendSelectedCandidatesInfo,
        SendProducerIdentification,
        SendNodeInfo,
        SendNodesInfo,
        SendMinedBlock,
        SendBlocks,
        SendBlockHeaders,
        SendTransaction,
        SendTransactions,
        SendCurrentHeight,
        SendCurrentMaxBlockSize,

        SearchQueryRequest,
        SendQueryResult,
        Ping,
        Pong
    }
}
