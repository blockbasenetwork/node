using System.Collections.Generic;
using System.Linq;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common
{
    public class ColumnDefinition
    {
        public estring ColumnName { get; set; }

        public DataType DataType { get; set; }
        
        public IList<ColumnConstraint> ColumnConstraints { get; set; }

        public ColumnDefinition() { }

        public ColumnDefinition(estring columnName, DataType dataType, List<ColumnConstraint> columnConstraints = null)
        {
            ColumnName = columnName;
            DataType = dataType;             
            if (columnConstraints == null) ColumnConstraints = new List<ColumnConstraint>();
            else ColumnConstraints = columnConstraints;
        }


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