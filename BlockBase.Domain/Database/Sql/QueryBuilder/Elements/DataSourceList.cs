using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System.Collections.Generic;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements
{
    public class DataSourceList : IDataSource
    {
        private List<IDataSource> _dataSources;

        public IEnumerable<IDataSource> DataSources { get { return _dataSources; } }

        public DataSourceList()
        {
            _dataSources = new List<IDataSource>();
        }

        public DataSourceList(params IDataSource[] dataSources)
        {
            _dataSources.AddRange(dataSources);
        }
    }
}