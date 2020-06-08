
using System.Collections.Generic;

namespace BlockBase.Runtime.Provider
{
    public class ResultsAndColumnNamesPoco
    {
        public List<IList<string>> ResultRows { get; set; }
        public  IList<string> ColumnNames  { get; set; }

        public ResultsAndColumnNamesPoco(IList<string> columnNames, List<IList<string>> resultRows)
        {
            ColumnNames = columnNames;
            ResultRows = resultRows;
        }
    }
}