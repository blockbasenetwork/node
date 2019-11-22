using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record
{
    public class Value
    {
        public string ValueToInsert { get; set; }
        public bool? IsText { get; set; }

        public Value() { }

        public Value(string valueToInsert, bool? isText = null)
        {
            ValueToInsert = valueToInsert;
            IsText = isText;
        }


        public Value Clone()
        {
            return new Value(ValueToInsert, IsText);
        }
    }
}
