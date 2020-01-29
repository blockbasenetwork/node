using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using System.Collections.Generic;
using System.Linq;

namespace BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table
{
    public class SelectCoreStatement : ISqlStatement
    {
        public IList<ResultColumn> ResultColumns { get; set; }
        public IList<TableOrSubquery> TablesOrSubqueries { get; set; }
        public JoinClause JoinClause { get; set; }
        public AbstractExpression WhereExpression { get; set; }
        public bool DistinctFlag { get; set; }

        public SelectCoreStatement()
        {
            DistinctFlag = false;
            ResultColumns = new List<ResultColumn>();
            TablesOrSubqueries = new List<TableOrSubquery>();
        }

        public SelectCoreStatement Clone()
        {
            return new SelectCoreStatement()
            {

                ResultColumns = ResultColumns.Select(r => r.Clone()).ToList(),
                TablesOrSubqueries = TablesOrSubqueries.Select(t => t.Clone()).ToList(),
                JoinClause = JoinClause?.Clone(),
                WhereExpression = WhereExpression?.Clone(),
                DistinctFlag = DistinctFlag
            };
        }

        public bool TryAddTable(estring tableName)
        {
            if (TablesOrSubqueries.Count(t => t.TableName.Value == tableName.Value) == 0)
            {
                TablesOrSubqueries.Add(new TableOrSubquery(tableName));
                return true;
            }
            return false;
        }

        public bool TryAddResultColumn(TableAndColumnName tableAndColumnName)
        {
            if (ResultColumns.Count(c => c.TableName.Value == tableAndColumnName.TableName.Value && c.ColumnName.Value == tableAndColumnName.ColumnName.Value) == 0)
            {
                ResultColumns.Add(new ResultColumn(tableAndColumnName.TableName, tableAndColumnName.ColumnName));
                return true;
            }
            return false;
        }

        public void AddWhereClause(AbstractExpression expression)
        {
            WhereExpression = expression;
        }

        public string GetStatementType()
        {
            return "select core";
        }
    }
}