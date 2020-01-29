using System.Collections.Generic;

namespace BlockBase.Domain.Results
{
    public class QueryResult
    {
        public IList<string> Columns { get; set; }
        public IList<IList<string>> Data { get; set; }
        

        public QueryResult(IList<IList<string>> data, IList<string> columns)
        {
            Data = data;
            Columns = columns;
        }
    }
}