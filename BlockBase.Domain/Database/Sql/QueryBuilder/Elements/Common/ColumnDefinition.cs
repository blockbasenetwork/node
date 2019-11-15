using System.Collections.Generic;
using System.Linq;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common
{
    public class ColumnDefinition
    {
        public estring ColumnName { get; set; }

        public DataType DataType { get; set; }
        
        public IList<ColumnConstraint> ColumnConstraints { get; set; }



        public ColumnDefinition Clone()
        {
            return new ColumnDefinition()
            {
                ColumnName = ColumnName.Clone(),
                DataType = DataType.Clone(),
                ColumnConstraints = ColumnConstraints.Select(c => c.Clone()).ToList()
            };
        }
    }
}