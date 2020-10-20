using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BlockBase.Domain.Endpoints
{
    public class BlockBaseNetworkEndpoints
    {
        public const string GET_ALL_TRACKER_SIDECHAINS = "https://blockbase.network/api/NodeSupport/GetAllTrackerSidechains";
        public const string GET_TOP_21_PRODUCERS_ENDPOINTS = "https://blockbase.network/api/NodeSupport/GetTop21ProducersAndEndpoints";
        public const string GET_CURRENT_BBT_VALUE = "https://blockbase.network/api/NodeSupport/GetCurrentBBTTokenValue";
    }
}