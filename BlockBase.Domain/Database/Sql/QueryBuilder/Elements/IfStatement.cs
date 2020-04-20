using System.Collections.Generic;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using static BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions.ComparisonExpression;
using static BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions.LogicalExpression;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements
{
    public class IfStatement : ISqlStatement
    {
        public Builder Builder { get; set; }
        public SimpleSelectStatement SimpleSelectStatement { get; set; }
        public IfStatement(Builder builder, SimpleSelectStatement simpleSelectStatement)
        {
            Builder = builder;
            SimpleSelectStatement = simpleSelectStatement;
        }
        public string GetStatementType()
        {
            return "if statement";
        }
    }
}