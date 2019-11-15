using System;
using System.Collections.Generic;

namespace BlockBase.Domain.Database
{
    public class QueryAuxiliarData
    {
        public byte[] QueryHash {get; set;}
        public List<IResult> Values {get; set;}
        public int NumberOfNewSelects {get; set;}
        public List<Tuple<int, int, int>> WherePositions {get; set;}
        public List<StringQueryResult> Results {get; set;}
        public string EncryptedQuery {get; set;}

        public QueryAuxiliarData()
        {
            Results = new List<StringQueryResult>();
            Values = new List<IResult>();
            WherePositions = new List<Tuple<int, int, int>>();
        }
    }

}