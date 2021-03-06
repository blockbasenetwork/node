using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Configurations
{
    public class ProviderConfigurations
    {

        public ProviderAutomaticConfiguration AutomaticProduction { get; set; }
    }

    public class ProviderAutomaticConfiguration
    {
        public ProviderNodeConfig ValidatorNode { get; set; }
        public HistoryFullProviderNodeConfig HistoryNode { get; set; }
        public HistoryFullProviderNodeConfig FullNode { get; set; }
        public int MaxNumberOfSidechains { get; set; }
        public double MaxGrowthPerMonthInMB { get; set; }
        public double MaxRatioToStake { get; set; }
        public bool BBTValueAutoConfig { get; set; }
        public bool AutomaticExitRequest { get; set; }
    }

    public class HistoryFullProviderNodeConfig : ProviderNodeConfig
    {
        public double MaxSidechainGrowthPerMonthInMB { get; set; }
    }

    public class ProviderNodeConfig
    {
        public bool IsActive { get; set; }
        public double MinBBTPerEmptyBlock { get; set; }
        public double MinBBTPerMBRatio { get; set; }
        public double MaxStakeToMonthlyIncomeRatio { get; set; }
    }
}