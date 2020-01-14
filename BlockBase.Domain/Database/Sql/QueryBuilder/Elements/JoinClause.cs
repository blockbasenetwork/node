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
        public IList<JoinOperationField>  JoinOperationFields { get; set; }

        public JoinClause() { }

        public JoinClause(TableOrSubquery tableOrSubquery, IList<JoinOperationField> joinOperationFields)
        {
            TableOrSubquery = tableOrSubquery;
            JoinOperationFields = joinOperationFields;
        }

        public JoinClause Clone()
        {
            var joinClauseClone = new JoinClause() { TableOrSubquery = TableOrSubquery?.Clone(), JoinOperationFields = new List<JoinOperationField>() };
            foreach (var joinOperationField in JoinOperationFields)
            {
                joinClauseClone.JoinOperationFields.Add( new JoinOperationField(
                        new List<JoinOperationField.JoinOperatorEnum> (joinOperationField.JoinOperators),
                        joinOperationField.RightTableOrSubquery.Clone(),
                        joinOperationField.JoinClauseConstraint.Clone()
                        )
                    );
            }

            return joinClauseClone;
        }
    }
}