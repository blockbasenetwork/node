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
    }

    public class HistoryFullProviderNodeConfig : ProviderNodeConfig
    {
        public double MaxSidechainGrowthPerMonthInMB { get; set; }
    }

    public class ProviderNodeConfig
    {
        public bool IsActive { get; set; }
        public double MinBBTPerBlock { get; set; }
        public double MaxStakeToMonthlyIncomeRatio { get; set; }
    }
}