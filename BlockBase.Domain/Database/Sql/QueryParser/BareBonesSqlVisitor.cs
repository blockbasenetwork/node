﻿using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Transaction;
using BlockBase.Domain.Database.Sql.QueryParser;
using System;
using System.Collections.Generic;
using System.Linq;
using static BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions.LogicalExpression;
using static BlockBase.Domain.Database.Sql.QueryParser.BareBonesSqlParser;

namespace BlockBase.Domain.Database.QueryParser
{
    public class BareBonesSqlVisitor : BareBonesSqlBaseVisitor<object>
    {
        private estring _databaseName { get; set; }
        public override object VisitSql_stmt_list([NotNull] Sql.QueryParser.BareBonesSqlParser.Sql_stmt_listContext context)
        {
            if (context.exception != null) throw new Exception("Error parsing script.");

            var builder = new Builder();
            var stms = context.sql_stmt();

            foreach (var stm in stms)
            {
                try
                {
                    var sqlStatement = (ISqlStatement)Visit(stm);
                    if (sqlStatement == null) throw new Exception("The sql command:'" + stm.GetText() + "' was not recognized.");
                    builder.AddStatement(sqlStatement);
                }
                catch (Antlr4.Runtime.InputMismatchException)
                {
                    throw new Exception("Error parsing command:'" + stm.GetText() + "'.");
                }
            }
            return builder;
        }

        #region Visit Statements

        public override object VisitIf_stmt(If_stmtContext context)
        {
            try
            {
                var ifBuilder = new Builder();
                var simpleSelectStatement = (SimpleSelectStatement)Visit(context.simple_select_stmt());
                foreach (var stm in context.sql_stmt())
                {
                    var sqlStatement = (ISqlStatement)Visit(stm);
                    if (sqlStatement == null) throw new Exception("The sql command:'" + stm.GetText() + "' was not recognized.");
                    ifBuilder.AddStatement(sqlStatement);
                }
                return new IfStatement(ifBuilder, simpleSelectStatement);
            }
            catch (Antlr4.Runtime.InputMismatchException)
            {
                throw new Exception("Error parsing command:'" + context.GetText() + "'.");
            }
        }
        
        public override object VisitUse_database_stmt(Use_database_stmtContext context)
        {
            ThrowIfParserHasException(context);
            _databaseName = (estring)Visit(context.database_name().complex_name());
            return new UseDatabaseStatement() { DatabaseName = _databaseName };
        }

        public override object VisitCurrent_database_stmt(Current_database_stmtContext context)
        {
            return new CurrentDatabaseStatement();
        }

        public override object VisitList_databases_stmt(List_databases_stmtContext context)
        {
            return new ListDatabasesStatement();
        }

        public override object VisitGet_structure_stmt(Get_structure_stmtContext context)
        {
            throw new NotImplementedException();
        }

        public override object VisitTransaction_sql_stmt(Transaction_sql_stmtContext context){
            ThrowIfParserHasException(context);
            var transactionStatement = new TransactionStatement();
            foreach(var operationContext in context.operation_sql_stmt()){
                if(operationContext.insert_stmt() != null){
                    transactionStatement.OperationStatements.Add((InsertRecordStatement)(Visit(operationContext.insert_stmt())));
                } else if(operationContext.update_stmt() != null){
                    transactionStatement.OperationStatements.Add((UpdateRecordStatement)Visit(operationContext.update_stmt()));
                } else if(operationContext.delete_stmt()!= null){
                    transactionStatement.OperationStatements.Add((DeleteRecordStatement)Visit(operationContext.delete_stmt()));
                }
            }
            return transactionStatement;
        }

        public override object VisitBegin_stmt(Begin_stmtContext context){
            ThrowIfParserHasException(context);
            return new BeginStatement();
        }

        public override object VisitCommit_stmt(Commit_stmtContext context){
            ThrowIfParserHasException(context);
            return new CommitStatement();
        }

        public override object VisitRollback_stmt(Rollback_stmtContext context){
            ThrowIfParserHasException(context);
            return new RollbackStatement();
        }

        public override object VisitCreate_database_stmt([NotNull] Create_database_stmtContext context)
        {
            ThrowIfParserHasException(context);
            _databaseName = (estring)Visit(context.database_name().complex_name());
            return new CreateDatabaseStatement() { DatabaseName = _databaseName };
        }

        public override object VisitDrop_database_stmt([NotNull] Drop_database_stmtContext context)
        {
            ThrowIfParserHasException(context);
            var dropDatabaseName = (estring)Visit(context.database_name().complex_name());
            if (dropDatabaseName == _databaseName) _databaseName = null;
            return new DropDatabaseStatement() { DatabaseName = dropDatabaseName };
        }

        public override object VisitCreate_table_stmt(Create_table_stmtContext context)
        {
            ThrowIfParserHasException(context);

            return new CreateTableStatement()
            {
                TableName = (estring)Visit(context.table_name().complex_name()),
                ColumnDefinitions = context.column_def().Select(c => (ColumnDefinition)Visit(c)).ToList()
            };
        }

        public override object VisitDrop_table_stmt(Drop_table_stmtContext context)
        {
            
            ThrowIfParserHasException(context);
            return new DropTableStatement() { TableName = (estring)Visit(context.table_name().complex_name()) };
        }

        public override object VisitAlter_table_stmt(Alter_table_stmtContext context)
        {
            
            ThrowIfParserHasException(context);

            var tableNameEstring = (estring)Visit(context.table_name().complex_name());
            var newTableName = context.new_table_name();
            var columnDef = context.column_def();
            var newColumnName = context.new_column_name();
            var columnName = context.column_name();

            if (context.K_RENAME() != null && context.K_TO() != null && newTableName != null)
            {
                return new RenameTableStatement()
                {
                    TableName = tableNameEstring,
                    NewTableName = (estring)Visit(newTableName.complex_name())
                };
            }
            if (context.K_ADD() != null && context.K_COLUMN() != null && columnDef != null)
            {
                return new AddColumnStatement()
                {
                    TableName = tableNameEstring,
                    ColumnDefinition = (ColumnDefinition)Visit(columnDef)
                };
            }
            if (context.K_RENAME() != null && columnName != null && context.K_TO() != null && newColumnName != null)
            {
                return new RenameColumnStatement()
                {
                    TableName = tableNameEstring,
                    ColumnName = (estring)Visit(columnName.complex_name()),
                    NewColumnName = (estring)Visit(context.new_column_name().complex_name())
                };
            }
            if (context.K_DROP() != null && context.K_COLUMN() != null && columnName != null)
            {
                return new DropColumnStatement()
                {
                    TableName = tableNameEstring,
                    ColumnName = (estring)Visit(columnName.complex_name())
                };
            }
            throw new FormatException("Alter statement not recognized.");
        }

        public override object VisitInsert_stmt(Insert_stmtContext context)
        {
            
            ThrowIfParserHasException(context);
            var insertRecordStatement = new InsertRecordStatement()
            {
                TableName = (estring)Visit(context.table_name().complex_name()),
                ValuesPerColumn = new Dictionary<estring, IList<Value>>()
            };

            if (context.literal_value().Length % context.column_name().Length != 0)
                throw new FormatException("Badly formatted insert statement.");

            for (int i = 0; i < context.column_name().Length; i++)
            {
                var columnName = (estring)Visit(context.column_name()[i].complex_name());

                insertRecordStatement.ValuesPerColumn[columnName] = new List<Value>();

                for (int j = i; j < context.literal_value().Length; j += context.column_name().Length)
                {
                    var valueTxt = context.literal_value()[j].GetText().Trim('\'');
                    Value value = new Value(valueTxt, true);
                    if(valueTxt.ToLower() == "null") value = new Value("null", false);
                    insertRecordStatement.ValuesPerColumn[columnName].Add(value);
                }
            }
            return insertRecordStatement;
        }

        public override object VisitUpdate_stmt(Update_stmtContext context)
        {
            
            ThrowIfParserHasException(context);
            var updateRecordStatement = new UpdateRecordStatement()
            {
                TableName = (estring)Visit(context.table_name().complex_name()),
                ColumnNamesAndUpdateValues = new Dictionary<estring, AbstractExpression>()
            };

            if (context.K_WHERE() != null)
            {
                updateRecordStatement.WhereExpression = (AbstractExpression)Visit(context.expr().Last());
            }

            var expressions = context.expr();
            var expressionLength = context.K_WHERE() != null ? expressions.Length-1 : expressions.Length;
            for (int i = 0; i < expressionLength; i++)
            {
                updateRecordStatement.ColumnNamesAndUpdateValues.Add(
                    (estring)Visit(context.column_name()[i].complex_name()),
                    (AbstractExpression)Visit(expressions[i])
                    //new Value(context.expr()[i].GetText().Trim('\''))
                    );
            }
            var caseExpressions = context.case_expr();
            for(int i = 0; i < caseExpressions.Length; i++){
                updateRecordStatement.ColumnNamesAndUpdateValues.Add(
                    (estring)Visit(context.column_name()[i].complex_name()),
                    (AbstractExpression)Visit(caseExpressions[i])
                );
                updateRecordStatement.CaseExpressions.Add((AbstractExpression)Visit(caseExpressions[i]));
            }

            return updateRecordStatement;
        }

        public override object VisitDelete_stmt(Delete_stmtContext context)
        {
            
            ThrowIfParserHasException(context);
            return new DeleteRecordStatement()
            {
                TableName = (estring)Visit(context.table_name().complex_name()),
                WhereExpression = context.expr() != null ? (AbstractExpression)Visit(context.expr()) : null
            };
        }

        public override object VisitSimple_select_stmt([NotNull] Simple_select_stmtContext context)
        {
            
            ThrowIfParserHasException(context);
            var simpleSelectStatement = new SimpleSelectStatement()
            {
                SelectCoreStatement = (SelectCoreStatement)Visit(context.select_core()),
                OrderingTerms = context.ordering_term().Select(o => (OrderingTerm)Visit(o)).ToList()
            };

            if (context.K_LIMIT() != null)
                simpleSelectStatement.Limit = Int32.Parse(context.literal_value()[0].GetText().Trim('\''));
            if (context.K_OFFSET() != null)
                simpleSelectStatement.Offset = Int32.Parse(context.literal_value()[1].GetText().Trim('\''));

            return simpleSelectStatement;
        }

        public override object VisitSelect_core(Select_coreContext context)
        {
            ThrowIfParserHasException(context);
            var selectCoreStatement = new SelectCoreStatement();
            if (context.K_DISTINCT() != null) selectCoreStatement.DistinctFlag = true;

            foreach (var resultColumnContext in context.result_column())
            {
                selectCoreStatement.ResultColumns.Add((ResultColumn)Visit(resultColumnContext));
            }
            foreach (var tableOrSubqueryContext in context.table_or_subquery())
            {
                selectCoreStatement.TablesOrSubqueries.Add((TableOrSubquery)Visit(tableOrSubqueryContext));
            }
            if (context.join_clause() != null) selectCoreStatement.JoinClause = (JoinClause)Visit(context.join_clause());
            if (context.case_expr().Length != 0){
                foreach(var contextExpr in context.case_expr()){
                    if(contextExpr.K_CASE() != null) {
                        selectCoreStatement.CaseExpressions.Add((AbstractExpression)Visit(contextExpr));
                    }
                }
            }
            if (context.expr() != null){
                selectCoreStatement.WhereExpression = (AbstractExpression)Visit(context.expr());
            }
            if (context.K_ENCRYPTED() != null) selectCoreStatement.Encrypted = true;
            return selectCoreStatement;
        }

        public override object VisitColumn_def(Column_defContext columnDefContext)
        {
            ThrowIfParserHasException(columnDefContext);

            var columnDef = new ColumnDefinition
            {
                ColumnName = (estring)Visit(columnDefContext.column_name().complex_name()),
                DataType = (DataType)Visit(columnDefContext.data_type()),
                ColumnConstraints = columnDefContext.column_constraint().Select(c => (ColumnConstraint)Visit(c)).ToList()
            };

            if (columnDef.DataType.DataTypeName == DataTypeEnum.ENCRYPTED
            && columnDef.DataType.BucketInfo.EqualityNumberOfBuckets == null
            && columnDef.DataType.BucketInfo.RangeNumberOfBuckets == null
            && columnDef.ColumnConstraints.Count(c => c.ColumnConstraintType == ColumnConstraint.ColumnConstraintTypeEnum.PrimaryKey ||
                c.ColumnConstraintType == ColumnConstraint.ColumnConstraintTypeEnum.Unique) == 0)
            {
                throw new FormatException("If the column is not unique or primary key you need to specify the number of equality buckets you desire.");
            }

            if (columnDef.DataType.DataTypeName == DataTypeEnum.ENCRYPTED && columnDef.DataType.BucketInfo.EqualityNumberOfBuckets != null
                && columnDef.ColumnConstraints.Count(c => c.ColumnConstraintType == ColumnConstraint.ColumnConstraintTypeEnum.PrimaryKey ||
                c.ColumnConstraintType == ColumnConstraint.ColumnConstraintTypeEnum.Unique) != 0)
            {
                throw new FormatException("If the column is unique or primary key it doesn't need equality bucket size.");
            }

            return columnDef;
        }

        public override object VisitColumn_constraint(Column_constraintContext columnConstraintContext)
        {
            ThrowIfParserHasException(columnConstraintContext);
            var columnConstraint = new ColumnConstraint()
            {
                Name = columnConstraintContext.name() != null && columnConstraintContext.name().complex_name() != null ?
                    (estring)Visit(columnConstraintContext.name().complex_name()) : null
            };

            if (columnConstraintContext.K_PRIMARY() != null)
                columnConstraint.ColumnConstraintType = ColumnConstraint.ColumnConstraintTypeEnum.PrimaryKey;
            else if (columnConstraintContext.K_NOT() != null && columnConstraintContext.K_NULL() != null)
                columnConstraint.ColumnConstraintType = ColumnConstraint.ColumnConstraintTypeEnum.NotNull;
            else if (columnConstraintContext.K_NULL() != null)
                columnConstraint.ColumnConstraintType = ColumnConstraint.ColumnConstraintTypeEnum.Null;
            else if (columnConstraintContext.K_UNIQUE() != null)
                columnConstraint.ColumnConstraintType = ColumnConstraint.ColumnConstraintTypeEnum.Unique;
            else if (columnConstraintContext.foreign_key_clause() != null)
            {
                columnConstraint.ColumnConstraintType = ColumnConstraint.ColumnConstraintTypeEnum.ForeignKey;
                columnConstraint.ForeignKeyClause = (ForeignKeyClause)Visit(columnConstraintContext.foreign_key_clause());
            }
            else
                throw new FormatException("Badly formatted column constraint.");
            return columnConstraint;
        }

        public override object VisitForeign_key_clause(Foreign_key_clauseContext foreignKeyClauseContext)
        {
            ThrowIfParserHasException(foreignKeyClauseContext);
            return new ForeignKeyClause()
            {
                TableName = (estring)Visit(foreignKeyClauseContext.foreign_table().complex_name()),
                ColumnNames = foreignKeyClauseContext.column_name().Select(c => (estring)Visit(c.complex_name())).ToList()
            };
        }

        public override object VisitComplex_name(Complex_nameContext complexNameContext)
        {
            ThrowIfParserHasException(complexNameContext);
            return new estring() { Value = complexNameContext.any_name().GetText(), ToEncrypt = complexNameContext.K_NOT_TO_ENCRYPT() == null };
        }

        public override object VisitData_type(Data_typeContext dataTypeContext)
        {
            ThrowIfParserHasException(dataTypeContext);
            if (dataTypeContext.K_BOOL() != null) return new DataType() { DataTypeName = DataTypeEnum.BOOL };
            if (dataTypeContext.K_DATETIME() != null) return new DataType() { DataTypeName = DataTypeEnum.DATETIME };
            if (dataTypeContext.K_DECIMAL() != null) return new DataType() { DataTypeName = DataTypeEnum.DECIMAL };
            if (dataTypeContext.K_DOUBLE() != null) return new DataType() { DataTypeName = DataTypeEnum.DOUBLE };
            if (dataTypeContext.K_DURATION() != null) return new DataType() { DataTypeName = DataTypeEnum.DURATION };
            if (dataTypeContext.K_INT() != null) return new DataType() { DataTypeName = DataTypeEnum.INT };
            if (dataTypeContext.K_TEXT() != null) return new DataType() { DataTypeName = DataTypeEnum.TEXT };
            if (dataTypeContext.K_BIGINT() != null) return new DataType() { DataTypeName = DataTypeEnum.BIGINT };
            if (dataTypeContext.K_SERIAL() != null) return new DataType() { DataTypeName = DataTypeEnum.SERIAL };
            if (dataTypeContext.K_UUID() != null) return new DataType() { DataTypeName = DataTypeEnum.UUID };
            if (dataTypeContext.K_ENCRYPTED() != null)
            {
                var dataType = new DataType() { DataTypeName = DataTypeEnum.ENCRYPTED };
                if (dataTypeContext.bucket_number() != null)
                {
                    dataType.BucketInfo.EqualityNumberOfBuckets = Int32.Parse(dataTypeContext.bucket_number().NUMERIC_LITERAL().GetText());
                }

                if (dataTypeContext.K_RANGE() != null)
                {
                    var bktSizeRange = (Tuple<int, int, int>)Visit(dataTypeContext.bucket_range());
                    dataType.BucketInfo.RangeNumberOfBuckets = bktSizeRange.Item1;
                    dataType.BucketInfo.BucketMinRange = bktSizeRange.Item2;
                    dataType.BucketInfo.BucketMaxRange = bktSizeRange.Item3;
                    if (dataType.BucketInfo.BucketMinRange >= dataType.BucketInfo.BucketMaxRange)
                        throw new FormatException("Bucket min range cannot be bigger than max range. (bucketSize, minRange, maxRange)");
                }
                return dataType;
            }

            throw new FormatException("DataType not recognized.");
        }

        public override object VisitBucket_range(Bucket_rangeContext bucketRangeContext)
        {
            ThrowIfParserHasException(bucketRangeContext);

            var size = Int32.Parse(bucketRangeContext.NUMERIC_LITERAL()[0].GetText());
            var min = Int32.Parse(bucketRangeContext.NUMERIC_LITERAL()[1].GetText());
            var max = Int32.Parse(bucketRangeContext.NUMERIC_LITERAL()[2].GetText());

            return new Tuple<int, int, int>(size, min, max);
        }

        public override object VisitCase_expr(Case_exprContext caseExpr)
        {
            ThrowIfParserHasException(caseExpr);
            var exprLength = caseExpr.expr().Length;
            var whenThenExpressions = new List<WhenThenExpression>();
            
            
            for(var expressionIndex = 0; expressionIndex < exprLength; expressionIndex = expressionIndex + 2){
                if(exprLength%2 != 0 && expressionIndex == exprLength - 1) continue;
                var newWhenThenExpression = new WhenThenExpression{
                    WhenExpression = (ComparisonExpression)Visit(caseExpr.expr()[expressionIndex]),
                    ThenExpression = (LiteralValueExpression)Visit(caseExpr.expr()[expressionIndex+1])
                };
                whenThenExpressions.Add(newWhenThenExpression);
            }
            estring tableName = null;
            if(whenThenExpressions.FirstOrDefault().WhenExpression is ComparisonExpression comparisonExpression){
                tableName = comparisonExpression.LeftTableNameAndColumnName.TableName;
            }else if(whenThenExpressions.FirstOrDefault().WhenExpression  is LogicalExpression logicalExpression){
                var leftExpression = logicalExpression.LeftExpression as ComparisonExpression;
                tableName = leftExpression.LeftTableNameAndColumnName.TableName;
            }
            var tableNameWhen = whenThenExpressions.FirstOrDefault().WhenExpression as ComparisonExpression;
            if(caseExpr.K_ELSE() !=null && caseExpr.result_column() != null){ 
                return new CaseExpression(){
                    WhenThenExpressions = whenThenExpressions,
                    ElseExpression = (LiteralValueExpression)Visit(caseExpr.expr().LastOrDefault()),
                    ResultColumn = (ResultColumn)Visit(caseExpr.result_column())
                };
            } else if(caseExpr.result_column() != null && caseExpr.K_ELSE() ==null){
                var auxExpression = new CaseExpression(){
                    WhenThenExpressions = whenThenExpressions,
                    ElseExpression = null,
                    ResultColumn = (ResultColumn)Visit(caseExpr.result_column())
                };
                return auxExpression;
            } else if(caseExpr.result_column() == null && caseExpr.K_ELSE() != null){
                return new CaseExpression(){
                    WhenThenExpressions = whenThenExpressions,
                    ElseExpression = (LiteralValueExpression)Visit(caseExpr.expr().LastOrDefault()),
                    ResultColumn = new ResultColumn(){
                        TableName = tableName,
                        ColumnName = new estring("caseColumn"),
                        AllColumnsfFlag = false
                    }
                };
            } else {
                return new CaseExpression(){
                    WhenThenExpressions = whenThenExpressions,
                    ElseExpression = null,
                    ResultColumn = new ResultColumn(){
                        TableName = tableName,
                        ColumnName = new estring("caseColumn"),
                        AllColumnsfFlag = false
                    }
                };
            }
        }

        public override object VisitExpr(ExprContext expr)
        {
            ThrowIfParserHasException(expr);
            if (expr.K_AND() != null && expr.expr().Length == 2)
            {
                return new LogicalExpression()
                {
                    LogicalOperator = LogicalOperatorEnum.AND,
                    LeftExpression = (AbstractExpression)Visit(expr.expr()[0]),
                    RightExpression = (AbstractExpression)Visit(expr.expr()[1])
                };
            }

            if (expr.K_OR() != null && expr.expr().Length == 2)
            {
                return new LogicalExpression()
                {
                    LogicalOperator = LogicalOperatorEnum.OR,
                    LeftExpression = (AbstractExpression)Visit(expr.expr()[0]),
                    RightExpression = (AbstractExpression)Visit(expr.expr()[1])
                };
            }

            var exprLength = expr.expr().Length;

            var exprString = expr.GetText();
            var exprOperator = "";
            if (expr.@operator() != null) exprOperator = expr.@operator().GetText();
            else if (expr.K_IS() != null && expr.K_NOT()!= null) exprOperator = "IS NOT";
            else if (expr.K_IS() != null && expr.K_NOT()== null) exprOperator = "IS";

            if (expr.table_name() != null && expr.column_name() != null && expr.literal_value() != null
                && (exprOperator.Contains("<") || exprOperator.Contains("<=") || exprOperator.Contains(">")
                || exprOperator.Contains(">=") || exprOperator.Contains("=") || exprOperator.Contains("!=")))
            {
                var comparisonExpression = new ComparisonExpression(
                    new TableAndColumnName(
                        (estring)Visit(expr.table_name().complex_name()),
                        (estring)Visit(expr.column_name().complex_name())),
                    new Value(expr.literal_value().GetText().Trim('\''), expr.literal_value().GetText().Contains("'")),
                    GetComparisonOperatorFromString(exprOperator));

                return comparisonExpression;
            }
            
            if (expr.table_name() != null && expr.column_name() != null && expr.literal_value() == null
                && (exprOperator.Contains("IS")))
            {
                var comparisonExpression = new ComparisonExpression(
                    new TableAndColumnName(
                        (estring)Visit(expr.table_name().complex_name()),
                        (estring)Visit(expr.column_name().complex_name())),
                    new Value(null),
                    GetComparisonOperatorFromString(exprOperator));

                return comparisonExpression;


            }

            if (expr.table_column_name() != null && expr.table_column_name().Count() == 2
               && (exprString.Contains("<") || exprString.Contains("<=") || exprString.Contains(">")
               || exprString.Contains(">=") || exprString.Contains("=") || exprString.Contains("!=")))
            {

                var comparisonExpression = new ComparisonExpression(
                    new TableAndColumnName(
                        (estring)Visit(expr.table_column_name()[0].table_name().complex_name()),
                        (estring)Visit(expr.table_column_name()[0].column_name().complex_name())),
                    new TableAndColumnName(
                        (estring)Visit(expr.table_column_name()[1].table_name().complex_name()),
                        (estring)Visit(expr.table_column_name()[1].column_name().complex_name())),
                    GetComparisonOperatorFromString(exprOperator));

                return comparisonExpression;
            }


            if (exprString.Contains("(") && exprString.Contains(")"))
            {
                var exprWithParenthesis = (AbstractExpression)Visit(expr.expr()[0]);
                exprWithParenthesis.HasParenthesis = true;
                return exprWithParenthesis;
            }

            //If nothing else than it is literal value expression
            return new LiteralValueExpression(new Value(expr.literal_value().GetText().Trim('\''), expr.literal_value().GetText().Contains("'")));
        }

        public override object VisitOrdering_term(Ordering_termContext orderingTermContext)
        {
            ThrowIfParserHasException(orderingTermContext);
            return new OrderingTerm()
            {
                Expression = (AbstractExpression)Visit(orderingTermContext.expr()),
                IsAscending = orderingTermContext.K_DESC() == null
            };
        }

        public override object VisitResult_column(Result_columnContext resultColumnContext)
        {
            ThrowIfParserHasException(resultColumnContext);
            var allColumns = resultColumnContext.table_column_name() == null;
            return new ResultColumn()
            {
                ColumnName = !allColumns ? (estring)Visit(resultColumnContext.table_column_name().column_name().complex_name()) : null,
                TableName = !allColumns ? (estring)Visit(resultColumnContext.table_column_name().table_name().complex_name()) : (estring)Visit(resultColumnContext.table_name().complex_name()),
                AllColumnsfFlag = allColumns
            };
        }

        public override object VisitTable_or_subquery(Table_or_subqueryContext tableOrSubqueryContext)
        {
            ThrowIfParserHasException(tableOrSubqueryContext);
            return new TableOrSubquery()
            {
                TableName = (estring)Visit(tableOrSubqueryContext.table_name().complex_name()),
                TablesOrSubqueries = tableOrSubqueryContext.table_or_subquery().Select(t => (TableOrSubquery)Visit(t)).ToList(),
                JoinClause = tableOrSubqueryContext.join_clause() != null ? (JoinClause)Visit(tableOrSubqueryContext.join_clause()) : null,
                SimpleSelectStatement = tableOrSubqueryContext.simple_select_stmt() != null ? (SimpleSelectStatement)Visit(tableOrSubqueryContext.simple_select_stmt()) : null
            };
        }

        public override object VisitJoin_clause(Join_clauseContext joinClauseContext)
        {
            ThrowIfParserHasException(joinClauseContext);
            var joinClause = new JoinClause()
            {
                TableOrSubquery = (TableOrSubquery)Visit(joinClauseContext.table_or_subquery()[0]),
                JoinOperationFields = new List<JoinOperationField>()
            };

            for (int i = 0; i < joinClauseContext.join_operator().Length; i++)
            {
                var joinOperationField = new JoinOperationField
                (
                    (List<JoinOperationField.JoinOperatorEnum>)Visit(joinClauseContext.join_operator()[i]),
                    (TableOrSubquery)Visit(joinClauseContext.table_or_subquery()[i + 1]),
                    (JoinOperationField.JoinConstraint)Visit(joinClauseContext.join_constraint()[i])
                );
                joinClause.JoinOperationFields.Add(joinOperationField);
            }
            return joinClause;
        }

        public override object VisitJoin_operator(Join_operatorContext joinOperatorContext)
        {
            ThrowIfParserHasException(joinOperatorContext);
            var joinOperatorEnumList = new List<JoinOperationField.JoinOperatorEnum>();
            if (joinOperatorContext.K_NATURAL() != null) joinOperatorEnumList.Add(JoinOperationField.JoinOperatorEnum.NATURAL);
            if (joinOperatorContext.K_LEFT() != null) joinOperatorEnumList.Add(JoinOperationField.JoinOperatorEnum.LEFT);
            if (joinOperatorContext.K_RIGHT() != null) joinOperatorEnumList.Add(JoinOperationField.JoinOperatorEnum.RIGHT);
            if (joinOperatorContext.K_FULL() != null) joinOperatorEnumList.Add(JoinOperationField.JoinOperatorEnum.FULL);
            if (joinOperatorContext.K_OUTER() != null) joinOperatorEnumList.Add(JoinOperationField.JoinOperatorEnum.OUTER);
            if (joinOperatorContext.K_CROSS() != null) joinOperatorEnumList.Add(JoinOperationField.JoinOperatorEnum.CROSS);
            return joinOperatorEnumList;
        }

        public override object VisitJoin_constraint(Join_constraintContext joinConstraintContext)
        {
            ThrowIfParserHasException(joinConstraintContext);
            return new JoinOperationField.JoinConstraint()
            {
                Expression = (AbstractExpression)Visit(joinConstraintContext.expr())
                //ColumnNames = joinConstraintContext.column_name().Select(c => (estring)Visit(c.complex_name())).ToList()
            };
        }

        #endregion Visit Statements

        #region Auxiliar Methods

        private ComparisonExpression.ComparisonOperatorEnum GetComparisonOperatorFromString(string exprString)
        {
            if (exprString =="<=")
                return ComparisonExpression.ComparisonOperatorEnum.SmallerOrEqualThan;
            if (exprString == ">=")
                return ComparisonExpression.ComparisonOperatorEnum.BiggerOrEqualThan;
            if (exprString == "<")
                return ComparisonExpression.ComparisonOperatorEnum.SmallerThan;
            if (exprString == ">")
                return ComparisonExpression.ComparisonOperatorEnum.BiggerThan;
            if (exprString == "!=")
                return ComparisonExpression.ComparisonOperatorEnum.Different;
            if (exprString == "=")
                return ComparisonExpression.ComparisonOperatorEnum.Equal;
            if (exprString == "IS NOT")
                return ComparisonExpression.ComparisonOperatorEnum.IsNot;
            if (exprString == "IS")
                return ComparisonExpression.ComparisonOperatorEnum.Is;
            

            throw new FormatException("No comparison operator in string.");
        }

        private void ThrowIfParserHasException(ParserRuleContext context)
        {
            if (context.exception != null) throw context.exception;
        }


        #endregion Auxiliar Methods
    }
}