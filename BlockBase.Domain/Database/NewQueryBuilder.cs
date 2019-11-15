using System.Collections.Generic;
using BlockBase.Domain.Database.QueryResults;

namespace BlockBase.Domain.Database
{
    public class NewQueryBuilder
    {
        public List<SelectField> SelectFields { get; set; }
        public HashSet<string> FromTables { get; set; }
        public IList<WhereField> WhereFields { get; private set; }
        public IList<JoinField> JoinFields { get; set; }

        public NewQueryBuilder()
        {
            SelectFields = new List<SelectField>();
            FromTables = new HashSet<string>();
            WhereFields = new List<WhereField>();
            JoinFields = new List<JoinField>();
        }

        public NewQueryBuilder(string query)
        {

        }

        public NewQueryBuilder Select(string column, string table)
        {
            SelectFields.Add(new SelectField { Column = column, Table = table });
            FromTables.Add(table);
            return this;
        }
        
        // private NewQueryBuilder MapQueryStringToQueryBuilder(string query)
        // {


        // }


    }
}