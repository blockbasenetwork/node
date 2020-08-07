namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common
{
    public class ColumnConstraint
    {
        public estring Name { get; set; }

        public ColumnConstraintTypeEnum ColumnConstraintType { get; set; }

        public ForeignKeyClause ForeignKeyClause { get; set; }

        public enum ColumnConstraintTypeEnum
        {
            PrimaryKey,
            NotNull,
            Null,
            Unique,
            ForeignKey
        }

        public override string ToString()
        {
            string str = "";
            if(Name != null) str += Name.Value + " ";

            str += ColumnConstraintType;
            bool first = true;
            if(ForeignKeyClause != null){
            foreach(var column in ForeignKeyClause.ColumnNames)
            {
                if (first) str += " ";
                else str += ", ";
                str += ForeignKeyClause.TableName.Value + "." + column.Value;
            }
            }            
            return str;
        }

        public ColumnConstraint Clone()
        {
            return new ColumnConstraint() { Name = Name.Clone(), ColumnConstraintType = ColumnConstraintType, ForeignKeyClause = ForeignKeyClause.Clone() };
        }
    }
}