﻿using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Transaction;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.Generators
{
    public class PSqlGenerator : IGenerator
    {
        public string BuildString(CreateDatabaseStatement createDatabaseStatement)
        {
            return "CREATE DATABASE " + createDatabaseStatement.DatabaseName.Value;
        }

        public string BuildString(DropDatabaseStatement dropDatabaseStatement)
        {
            return "DROP DATABASE " + dropDatabaseStatement.DatabaseName.Value;
        }

        public string BuildString(UseDatabaseStatement useDatabaseStatement)
        {
            return "USE " + useDatabaseStatement.DatabaseName.Value;
        }
        public string BuildString(BeginStatement beginStatement)
        {
            return "BEGIN";
        }
        public string BuildString(CommitStatement commitStatement)
        {
            return "COMMIT";
        }

        public string BuildString(RollbackStatement rollbackStatement)
        {
            return "ROLLBACK";
        }

        public string BuildString(CreateTableStatement createTableStatement)
        {
            var psqlString = "CREATE TABLE " + createTableStatement.TableName.Value + " ( " + BuildString(createTableStatement.ColumnDefinitions[0]);
            for (int i = 1; i < createTableStatement.ColumnDefinitions.Count; i++)
            {
                psqlString += ", " + BuildString(createTableStatement.ColumnDefinitions[i]);
            }
            return psqlString + " )";
        }

        public string BuildString(AbstractAlterTableStatement alterTableStatement)
        {
            var psqlString = "ALTER TABLE " + alterTableStatement.TableName.Value;

            if (alterTableStatement is RenameTableStatement renameTableStatement)
                psqlString += " RENAME TO " + renameTableStatement.NewTableName.Value;
            else if (alterTableStatement is AddColumnStatement addColumnStatement)
                psqlString += " ADD COLUMN " + BuildString(addColumnStatement.ColumnDefinition);
            else if (alterTableStatement is DropColumnStatement dropColumnStatement)
                psqlString += " DROP COLUMN " + dropColumnStatement.ColumnName.Value;
            else if (alterTableStatement is RenameColumnStatement renameColumnStatement)
                psqlString += " RENAME " + renameColumnStatement.ColumnName.Value + " TO " + renameColumnStatement.NewColumnName.Value;
            return psqlString;
        }

        public string BuildString(DropTableStatement dropTableStatement)
        {
            return "DROP TABLE " + dropTableStatement.TableName.Value;
        }
        
        public string BuildString(InsertRecordStatement insertRecordStatement)
        {
            var psqlString = "INSERT INTO " + insertRecordStatement.TableName.Value + " ( ";

            var columnNames = new List<estring>(insertRecordStatement.ValuesPerColumn.Keys);

            for (int i = 0; i < columnNames.Count; i++)
            {
                if (i != 0) psqlString += ", ";
                psqlString += columnNames[i].Value;
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
                    psqlString += BuildString(insertRecordStatement.ValuesPerColumn[columnNames[j]][i]);
                }
                psqlString += " )";
            }
            return psqlString;
        }

        public string BuildString(UpdateRecordStatement updateRecordStatement)
        {
            var psqlString = "UPDATE " + updateRecordStatement.TableName.Value + " SET ";

            var first = true;

            //TODO BUilD FROM CASE EXPRESSION
            foreach (var keyValuePair in updateRecordStatement.ColumnNamesAndUpdateValues)
            {
                if (first) first = false;
                else psqlString += ", ";
                psqlString += keyValuePair.Key.Value + " = " + BuildString(keyValuePair.Value);
            }

            if (updateRecordStatement.WhereExpression != null)
            {
                psqlString += " WHERE " + BuildString(updateRecordStatement.WhereExpression);
            }

            return psqlString;
        }

        public string BuildString(DeleteRecordStatement deleteRecordStatement)
        {
            var psqlString = "DELETE FROM " + deleteRecordStatement.TableName.Value;
            if (deleteRecordStatement.WhereExpression != null)
            {
                psqlString += " WHERE " + BuildString(deleteRecordStatement.WhereExpression);
            }
            return psqlString;
        }

        public string BuildString(TransactionStatement transactionStatement){
            var psqlString ="";
            foreach(var operation in transactionStatement.OperationStatements){
                if(operation is InsertRecordStatement insertRecordStatement){
                    psqlString+= BuildString(insertRecordStatement) +";";
                } else if(operation is UpdateRecordStatement updateRecordStatement){
                    psqlString+= BuildString(updateRecordStatement)+";";
                } else if(operation is DeleteRecordStatement deleteRecordStatement){
                    psqlString+= BuildString(deleteRecordStatement)+";";
                }
            }
            psqlString = psqlString.Remove(psqlString.Length-1);
            return psqlString;
        }   

        /*private string BuildString(ISqlStatement operationStatement){
            if(operationStatement is InsertRecordStatement insertRecordStatement){
                return BuildString(insertRecordStatement);
            } else if(operationStatement is UpdateRecordStatement updateRecordStatement){
                return BuildString(updateRecordStatement);
            } else if(operationStatement is DeleteRecordStatement deleteRecordStatement){
                return BuildString(deleteRecordStatement);
            }
            return "";
        }*/

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
            //TODO build string for case statement 
            var psqlString = "SELECT ";

            if (selectCoreStatement.DistinctFlag) psqlString += "DISTINCT ";

            for (int i = 0; i < selectCoreStatement.ResultColumns.Count; i++)
            {
                var isCase = false;
                if (i != 0) psqlString += ", ";
                if(selectCoreStatement.CaseExpressions.Count != 0){
                    foreach(var expression in selectCoreStatement.CaseExpressions){
                        var caseExpression = expression as CaseExpression;
                        var whenThenExpressionsList = new List<WhenThenExpression>();
                        whenThenExpressionsList.AddRange(caseExpression.WhenThenExpressions);
                        //var caseFromResultColumn = whenThenExpressionsList.Find(e => e.WhenExpression.LeftTableNameAndColumnName.TableName.Equals(selectCoreStatement.ResultColumns[i].TableName) 
                        //&& e.WhenExpression.LeftTableNameAndColumnName.ColumnName.Equals(selectCoreStatement.ResultColumns[i].ColumnName));
                        if(caseExpression.ResultColumn.ColumnName.Equals(selectCoreStatement.ResultColumns[i].ColumnName)
                        && caseExpression.ResultColumn.TableName.Equals(selectCoreStatement.ResultColumns[i].TableName)){
                            psqlString += BuildString(caseExpression);
                            psqlString += selectCoreStatement.ResultColumns[i].ColumnName.Value;
                            isCase = true;
                        }
                    }
                }
                if(isCase) continue;
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

        public string BuildStringToSimpleSelectStatement(UpdateRecordStatement updateRecordStatement){
            var psqlString = "SELECT ";

            var commaAuxiliar = 0;

            foreach(var entry in updateRecordStatement.ColumnNamesAndUpdateValues){
                if(commaAuxiliar!=0) psqlString += ", ";
                commaAuxiliar++;
                var caseExpression = entry.Value as CaseExpression;
                var commaWhenAuxiliar = 0;
                foreach(var whenThenExpression in caseExpression.WhenThenExpressions){
                    ComparisonExpression whenExpression = null;
                    if(whenThenExpression.WhenExpression is ComparisonExpression comparisonExpression){
                        whenExpression = comparisonExpression;
                    }else if(whenThenExpression.WhenExpression is LogicalExpression logicalExpression){
                        var leftExpression = logicalExpression.LeftExpression as ComparisonExpression;
                        whenExpression = leftExpression;
                    }
                    if(commaWhenAuxiliar!= 0) psqlString += ", ";
                    commaWhenAuxiliar++;
                    var resultColumn = whenExpression.LeftTableNameAndColumnName;
                    psqlString += BuildString(new ResultColumn(resultColumn.TableName,resultColumn.ColumnName));
                }
                if(entry.Key != null){
                    psqlString += ", " +BuildString(new ResultColumn(updateRecordStatement.TableName, new estring(entry.Key.Value)));
                }
                psqlString += ", " +BuildString(caseExpression);
            }

            psqlString += "FROM " +updateRecordStatement.TableName.Value +";";

            return psqlString;
        }

        public string BuildString(JoinClause joinClause)
        {
            var psqlString = BuildString(joinClause.TableOrSubquery);
            for (int i = 0; i < joinClause.JoinOperationFields.Count; i++)
            {
                psqlString += " ";

                var joinOperators = joinClause.JoinOperationFields[i].JoinOperators;
                var tableOrSubquery = joinClause.JoinOperationFields[i].RightTableOrSubquery;
                var joinConstraint = joinClause.JoinOperationFields[i].JoinClauseConstraint;

                foreach (var joinOperator in joinOperators)
                    psqlString += joinOperator + " ";

                psqlString += "JOIN " + BuildString(tableOrSubquery) + " " + BuildString(joinConstraint);
            }
            return psqlString;
        }
        public string BuildString(JoinOperationField.JoinConstraint joinConstraint)
        {
            //if (joinConstraint.Expression != null)
            return "ON " + BuildString(joinConstraint.Expression);
            //var psqlString = "USING ";
            //for (int i = 0; i < joinConstraint.ColumnNames.Count; i++)
            //{
            //    if (i != 0) psqlString += ", ";
            //    psqlString += joinConstraint.ColumnNames[i].Value;
            //}

        }
        public string BuildString(ResultColumn resultColumn)
        {
            if (resultColumn.AllColumnsfFlag) return "*";
            return resultColumn.TableName != null ? resultColumn.TableName.Value + "." + resultColumn.ColumnName.Value
                : resultColumn.ColumnName.Value;
        }
        public string BuildString(TableOrSubquery tableOrSubquery)
        {
            if (tableOrSubquery.TableName != null) return tableOrSubquery.TableName.Value;
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
            string exprString = "";
            if (expression is ComparisonExpression comparisonExpression)
            {
                var finalPart = comparisonExpression.Value != null ?
                    BuildString(comparisonExpression.Value) :
                    comparisonExpression.RightTableNameAndColumnName.TableName.Value + "." + comparisonExpression.RightTableNameAndColumnName.ColumnName.Value;
                exprString = comparisonExpression.LeftTableNameAndColumnName.TableName.Value + "." + comparisonExpression.LeftTableNameAndColumnName.ColumnName.Value + " "
                    + BuildString(comparisonExpression.ComparisonOperator) + " "
                    + finalPart;
            }

            else if (expression is LogicalExpression logicalExpression)
                exprString = BuildString(logicalExpression.LeftExpression) + " "
                   + logicalExpression.LogicalOperator + " "
                   + BuildString(logicalExpression.RightExpression);

            else if (expression is LiteralValueExpression literalValueExpression)
                exprString = "'" + literalValueExpression.LiteralValue.ValueToInsert + "'";

            else if (expression is CaseExpression caseExpression) {
                exprString = " CASE";
                foreach(var whenThenExpression in caseExpression.WhenThenExpressions){
                    exprString += BuildString(whenThenExpression);
                }
                if(caseExpression.ElseExpression != null){
                    exprString += " ELSE " +BuildString(caseExpression.ElseExpression);
                }
                exprString += " END ";
            }
            else if (expression is WhenThenExpression whenThenExpression)
                exprString = " WHEN " + BuildString(whenThenExpression.WhenExpression) + " THEN "  +BuildString(whenThenExpression.ThenExpression);
                

            if (expression.HasParenthesis) return "(" + exprString + ")";
            else return exprString;
        }

        public string BuildString(ColumnDefinition columnDefinition)
        {
            var psqlString = columnDefinition.ColumnName.Value + " " + BuildString(columnDefinition.DataType.DataTypeName);
            foreach (var columnConstraint in columnDefinition.ColumnConstraints)
            {
                psqlString += BuildString(columnConstraint);
            }
            return psqlString;
        }
        public string BuildString(DataTypeEnum dataType)
        {
            if (dataType == DataTypeEnum.DOUBLE) return "REAL";
            if (dataType == DataTypeEnum.DATETIME) return "TIMESTAMP";
            if (dataType == DataTypeEnum.DURATION) return "INTERVAL";
            if (dataType == DataTypeEnum.ENCRYPTED) return "TEXT";
            if (dataType == DataTypeEnum.UUID) return "uuid";
            return dataType + "";
        }

        public string BuildString(ColumnConstraint columnConstraint)
        {
            var psqlString = "";

            if (columnConstraint.Name != null)
                psqlString += " CONSTRAINT " + columnConstraint.Name.Value;

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
            var psqlString = " REFERENCES " + foreignKeyClause.TableName.Value + " ( " + foreignKeyClause.ColumnNames[0].Value;
            for (int i = 1; i < foreignKeyClause.ColumnNames.Count; i++)
            {
                psqlString += ", " + foreignKeyClause.ColumnNames[i].Value;
            }
            return psqlString + " )";
        }

        public string BuildString(Value value)
        {
            if (value.IsText) return "'" + value.ValueToInsert + "'";
            if (value.ValueToInsert == null) return "null";
            return value.ValueToInsert;
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

                case ComparisonExpression.ComparisonOperatorEnum.IsNot:
                    return "IS NOT";

                case ComparisonExpression.ComparisonOperatorEnum.Is:
                    return "IS";
            }
            throw new FormatException("Comparison operator not recognized.");
        }

    }
}
