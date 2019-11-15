using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.QueryResults
{

    public class NewSelectField
    {
        public string Column { get; set; }
        public string Table { get; set; }

        
        public string Build()
        {
            var select = "SELECT ";
            if(Table != null) return select + Table + "." + Column;
            else return select + Column;
        }
    }
}
