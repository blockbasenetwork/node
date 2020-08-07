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

        public ColumnConstraint Clone()
        {
            return new ColumnConstraint() { Name = Name.Clone(), ColumnConstraintType = ColumnConstraintType, ForeignKeyClause = ForeignKeyClause.Clone() };
        }
    }
}