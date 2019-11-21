using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.Generators
{
    public class PSqlGenerator : IGenerator
    {
        public string BuildString(CreateDatabaseStatement createDatabaseStatement)
        {
            return "CREATE DATABASE " + createDatabaseStatement.DatabaseName.GetFinalString();
        }

        public string BuildString(DropDatabaseStatement dropDatabaseStatement)
        {
            return "DROP DATABASE " + dropDatabaseStatement.DatabaseName.GetFinalString();
        }

        public string BuildString(UseDatabaseStatement useDatabaseStatement)
        {
            return "USE " + useDatabaseStatement.DatabaseName.GetFinalString();
        }

        public string BuildString(CreateTableStatement createTableStatement)
        {
            var psqlString = "CREATE TABLE " + createTableStatement.TableName.GetFinalString() + " ( " + BuildString(createTableStatement.ColumnDefinitions[0]);
            for (int i = 1; i < createTableStatement.ColumnDefinitions.Count; i++)
            {
                psqlString += ", " + BuildString(createTableStatement.ColumnDefinitions[i]);
            }
            return psqlString + " )";
        }

        public string BuildString(AbstractAlterTableStatement alterTableStatement)
        {
            var psqlString = "ALTER TABLE " + alterTableStatement.TableName.GetFinalString();

            if (alterTableStatement is RenameTableStatement renameTableStatement)
                psqlString += " RENAME TO " + renameTableStatement.NewTableName.GetFinalString();
            else if (alterTableStatement is AddColumnStatement addColumnStatement)
                psqlString += " ADD COLUMN " + BuildString(addColumnStatement.ColumnDefinition);
            else if (alterTableStatement is DropColumnStatement dropColumnStatement)
                psqlString += " DROP COLUMN " + dropColumnStatement.ColumnName.GetFinalString();
            else if (alterTableStatement is RenameColumnStatement renameColumnStatement)
                psqlString += " RENAME " + renameColumnStatement.ColumnName.GetFinalString() + " TO " + renameColumnStatement.NewColumnName.GetFinalString();
            return psqlString;
        }

        public string BuildString(DropTableStatement dropTableStatement)
        {
            return "DROP TABLE " + dropTableStatement.TableName.GetFinalString();
        }
        
        //TODO: need to know the type of column to know if I add " or not
        public string BuildString(InsertRecordStatement insertRecordStatement)
        {
            var psqlString = "INSERT INTO " + insertRecordStatement.TableName.GetFinalString() + " ( ";

            var columnNames = new List<estring>(insertRecordStatement.ValuesPerColumn.Keys);

            for (int i = 0; i < columnNames.Count; i++)
            {
                if (i != 0) psqlString += ", ";
                psqlString += columnNames[i].GetFinalString();
            }
            psqlString += " ) VALUES ";

            int numberRows = insertRecordStatement.ValuesPerColumn[columnNames[0]].Count;

            for (int i = 0; i < numberRows; i++)
            {
                if (i != 0) psqlString += ", ";
                psqlString += "( ";

                for (int j = 0; j < columnNames.Count; j++)
                {
                    if (j != 0) psqlString += ", ";
                    psqlString += insertRecordStatement.ValuesPerColumn[columnNames[j]][i];
                }
                psqlString += " )";
            }
            return psqlString;
        }

        public string BuildString(UpdateRecordStatement updateRecordStatement)
        {
            var psqlString = "UPDATE " + updateRecordStatement.TableName.GetFinalString() + " SET ";

            var first = true;
            foreach (var keyValuePair in updateRecordStatement.ColumnNamesAndUpdateValues)
            {
                if (first) first = false;
                else psqlString += ", ";
                psqlString += keyValuePair.Key.GetFinalString() + " = " + keyValuePair.Value;
            }

            if (updateRecordStatement.WhereClause != null)
            {
                psqlString += " WHERE " + BuildString(updateRecordStatement.WhereClause);
            }

            return psqlString;
        }

        public string BuildString(DeleteRecordStatement deleteRecordStatement)
        {
            var psqlString = "DELETE FROM " + deleteRecordStatement.TableName.GetFinalString();
            if (deleteRecordStatement.WhereClause != null)
            {
                psqlString += " WHERE " + BuildString(deleteRecordStatement.WhereClause);
            }
            return psqlString;
        }

        public string BuildString(SimpleSelectStatement simpleSelectStatement)
        {
            var psqlString = BuildString(simpleSelectStatement.SelectCoreStatement);

            if (simpleSelectStatement.OrderingTerms != null && simpleSelectStatement.OrderingTerms.Count != 0)
                psqlString += " ORDER BY ";

            for (int i = 0; i < simpleSelectStatement.OrderingTerms.Count; i++)
            {
                if (i != 0) psqlString += ", ";
                psqlString += BuildString(simpleSelectStatement.OrderingTerms[i]);
            }

            if (simpleSelectStatement.Limit != null)
            {
                psqlString += " LIMIT " + simpleSelectStatement.Limit;
                if (simpleSelectStatement.Offset != null)
                    psqlString += " OFFSET " + simpleSelectStatement.Offset;
            }
            return psqlString;
        }

        public string BuildString(SelectCoreStatement selectCoreStatement)
        {
            var psqlString = "SELECT ";

            if (selectCoreStatement.DistinctFlag) psqlString += "DISTINCT ";

            for (int i = 0; i < selectCoreStatement.ResultColumns.Count; i++)
            {
                if (i != 0) psqlString += ", ";
                psqlString += BuildString(selectCoreStatement.ResultColumns[i]);
            }

            psqlString += " FROM ";

            if (selectCoreStatement.JoinClause != null)
            {
                psqlString += BuildString(selectCoreStatement.JoinClause);
            }

            else
            {
                for (int i = 0; i < selectCoreStatement.TablesOrSubqueries.Count; i++)
                {
                    if (i != 0) psqlString += ", ";
                    psqlString += BuildString(selectCoreStatement.TablesOrSubqueries[i]);
                }
            }
           
            if (selectCoreStatement.WhereExpression != null)
                psqlString += " WHERE " + BuildString(selectCoreStatement.WhereExpression);

            return psqlString;
        }

        public string BuildString(JoinClause joinClause)
        {
            var psqlString = BuildString(joinClause.TableOrSubquery);
            for (int i = 0; i < joinClause.JoinOperationFields.Count; i++)
            {
                if (i == 0) psqlString += " ";
                else psqlString += ", ";

                var joinOperators = joinClause.JoinOperationFields[i].Item1;
                var tableOrSubquery = joinClause.JoinOperationFields[i].Item2;
                var joinConstraint = joinClause.JoinOperationFields[i].Item3;

                foreach (var joinOperator in joinOperators)
                    psqlString += joinOperator + " ";

                psqlString += "JOIN " + BuildString(tableOrSubquery) + " " + BuildString(joinConstraint);
            }
            return psqlString;
        }
        public string BuildString(JoinClause.JoinConstraint joinConstraint)
        {
            if (joinConstraint.Expression != null)
                return "ON " + BuildString(joinConstraint.Expression);
            var psqlString = "USING ";
            for (int i = 0; i < joinConstraint.ColumnNames.Count; i++)
            {
                if (i != 0) psqlString += ", ";
                psqlString += joinConstraint.ColumnNames[i].GetFinalString();
            }
            return psqlString;
        }
        public string BuildString(ResultColumn resultColumn)
        {
            if (resultColumn.AllColumnsfFlag) return "*";
            return resultColumn.TableName != null ? resultColumn.TableName.GetFinalString() + "." + resultColumn.ColumnName.GetFinalString()
                : resultColumn.ColumnName.GetFinalString();
        }
        public string BuildString(TableOrSubquery tableOrSubquery)
        {
            if (tableOrSubquery.TableName != null) return tableOrSubquery.TableName.GetFinalString();
            if (tableOrSubquery.SimpleSelectStatement != null) return "( " + BuildString(tableOrSubquery.SimpleSelectStatement) + " )";

            var psqlString = "( ";

            if (tableOrSubquery.JoinClause != null)
                psqlString += BuildString(tableOrSubquery.JoinClause);

            else
            {
                for (int i = 0; i < tableOrSubquery.TablesOrSubqueries.Count; i++)
                {
                    if (i != 0) psqlString += ", ";
                    psqlString += BuildString(tableOrSubquery.TablesOrSubqueries[i]);
                }
            }
           
            return psqlString + " )";
        }

        public string BuildString(OrderingTerm orderingTerm)
        {
            return orderingTerm.IsAscending ? BuildString(orderingTerm.Expression) : BuildString(orderingTerm.Expression) + " DESC";
        }
        public string BuildString(AbstractExpression expression)
        {
            if (expression is ComparisonExpression comparisonExpression)
                return comparisonExpression.TableName.GetFinalString()+ "." + comparisonExpression.ColumnName.GetFinalString() + " "
                    + BuildString(comparisonExpression.ComparisonOperator) + " "
                    + comparisonExpression.Value;

            if (expression is LogicalExpression logicalExpression)
                return BuildString(logicalExpression.LeftExpression) + " "
                   + logicalExpression.LogicalOperator + " "
                   + BuildString(logicalExpression.RightExpression);

            throw new FormatException("Expression type not recognized");
        }

        public string BuildString(ColumnDefinition columnDefinition)
        {
            var psqlString = columnDefinition.ColumnName.GetFinalString() + " " + BuildString(columnDefinition.DataType.DataTypeName);
            foreach (var columnConstraint in columnDefinition.ColumnConstraints)
            {
                psqlString += BuildString(columnConstraint);
            }
            return psqlString;
        }
        public string BuildString(DataTypeEnum dataType)
        {
            if (dataType == DataTypeEnum.DATETIME) return "TIMESTAMP";
            if (dataType == DataTypeEnum.DURATION) return "INTERVAL";
            if (dataType == DataTypeEnum.ENCRYPTED) return "TEXT";
            return dataType + "";
        }

        public string BuildString(ColumnConstraint columnConstraint)
        {
            var psqlString = "";

            if (columnConstraint.Name != null)
                psqlString += " CONSTRAINT " + columnConstraint.Name.GetFinalString();

            if (columnConstraint.ColumnConstraintType == ColumnConstraint.ColumnConstraintTypeEnum.PrimaryKey)
                return psqlString + " PRIMARY KEY";

            if (columnConstraint.ColumnConstraintType == ColumnConstraint.ColumnConstraintTypeEnum.NotNull)
                return psqlString + " NOT NULL";

            if (columnConstraint.ColumnConstraintType == ColumnConstraint.ColumnConstraintTypeEnum.Null)
                return psqlString + " NULL";

            if (columnConstraint.ColumnConstraintType == ColumnConstraint.ColumnConstraintTypeEnum.Unique)
                return psqlString + " UNIQUE";

            if (columnConstraint.ColumnConstraintType == ColumnConstraint.ColumnConstraintTypeEnum.ForeignKey)
                return psqlString + BuildString(columnConstraint.ForeignKeyClause);

            throw new FormatException("Column constraint type not recognized.");
        }

        public string BuildString(ForeignKeyClause foreignKeyClause)
        {
            var psqlString = " REFERENCES " + foreignKeyClause.ForeignTableName.GetFinalString() + " ( " + foreignKeyClause.ColumnNames[0].GetFinalString();
            for (int i = 1; i < foreignKeyClause.ColumnNames.Count; i++)
            {
                psqlString += ", " + foreignKeyClause.ColumnNames[i].GetFinalString();
            }
            return psqlString + " )";
        }

        public string BuildString(ComparisonExpression.ComparisonOperatorEnum comparisonOperator)
        {
            switch (comparisonOperator)
            {
                case ComparisonExpression.ComparisonOperatorEnum.BiggerOrEqualThan:
                    return ">=";

                case ComparisonExpression.ComparisonOperatorEnum.BiggerThan:
                    return ">";

                case ComparisonExpression.ComparisonOperatorEnum.Different:
                    return "!=";

                case ComparisonExpression.ComparisonOperatorEnum.Equal:
                    return "=";

                case ComparisonExpression.ComparisonOperatorEnum.SmallerOrEqualThan:
                    return "<=";

                case ComparisonExpression.ComparisonOperatorEnum.SmallerThan:
                    return "<";
            }
            throw new FormatException("Comparison operator not recognized.");
        }

    }
}
