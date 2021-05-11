using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static BlockBase.Domain.Database.Sql.QueryBuilder.Elements.JoinClause;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements
{
    public class JoinOperationField
    {
        public IList<JoinOperatorEnum> JoinOperators { get; set; }

        public TableOrSubquery RightTableOrSubquery { get; set; }

        public JoinConstraint JoinClauseConstraint { get; set; }

        public JoinOperationField() { }

        public JoinOperationField(IList<JoinOperatorEnum> joinOperators, TableOrSubquery rightTableOrSubquery,  JoinConstraint joinConstraint )
        {
            JoinOperators = joinOperators;
            RightTableOrSubquery = rightTableOrSubquery;
            JoinClauseConstraint = joinConstraint;
        }
       

        public enum JoinOperatorEnum
        {
            NATURAL,
            LEFT,
            RIGHT,
            FULL,
            OUTER,
            INNER,
            CROSS
        }

        public class JoinConstraint
        {
            public AbstractExpression Expression { get; set; }
            //public IList<estring> ColumnNames { get; set; }

            public JoinConstraint Clone()
            {
                return new JoinConstraint() { Expression = Expression.Clone()
                    //ColumnNames = ColumnNames.Select(c => c.Clone()).ToList()
                };
            }
        }
    }
}
