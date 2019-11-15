using System;
using System.Collections.Generic;
using System.Linq;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements
{
    public class JoinClause : IDataSource
    {

        public TableOrSubquery TableOrSubquery { get; set; }
        public IList<Tuple<IList<JoinOperatorEnum>, TableOrSubquery, JoinConstraint>> JoinOperationFields { get; set; }

        public enum JoinOperatorEnum
        {
            NATURAL,
            LEFT,
            OUTER,
            INNER,
            CROSS
        }

        public class JoinConstraint
        {
            public AbstractExpression Expression { get; set; }
            public IList<estring> ColumnNames { get; set; }

            public JoinConstraint Clone()
            {
                return new JoinConstraint() { Expression = Expression.Clone(), ColumnNames = ColumnNames.Select(c => c.Clone()).ToList() };
            }
        }

        public JoinClause Clone()
        {
            var joinClauseClone = new JoinClause() { TableOrSubquery = TableOrSubquery?.Clone(), JoinOperationFields = new List<Tuple<IList<JoinOperatorEnum>, TableOrSubquery, JoinConstraint>>() };
            foreach (var entry in JoinOperationFields)
            {
                joinClauseClone.JoinOperationFields.Add(new Tuple<IList<JoinOperatorEnum>, TableOrSubquery, JoinConstraint>(
                        new List<JoinOperatorEnum>(entry.Item1),
                        entry.Item2.Clone(),
                        entry.Item3.Clone()
                        )
                    );
            }

            return joinClauseClone;
        }
    }
}