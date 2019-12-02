using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using BlockBase.Domain.Database.Sql.Generators;
using BlockBase.Domain.Database.Sql.QueryBuilder;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
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
        public estring DatabaseName { get; set; }
        public override object VisitSql_stmt_list([NotNull] Sql.QueryParser.BareBonesSqlParser.Sql_stmt_listContext context)
        {
            var builder = new Builder();

            var stms = context.sql_stmt();

            foreach (var stm in stms)
            {
                var sqlStatement = (ISqlStatement)Visit(stm);
                //var psqlGenerator = new PSqlGenerator();
                //Console.WriteLine(psqlGenerator.BuildString(sqlStatement));
                if (sqlStatement is DropDatabaseStatement dropDatabaseStatement)
                    builder.AddStatement(sqlStatement, dropDatabaseStatement.DatabaseName);
                else
                    builder.AddStatement(sqlStatement, DatabaseName);
            }

            return builder;
        }

        #region Visit Statements

        public override object VisitUse_database_stmt(Use_database_stmtContext context)
        {
            CheckIfParserThrowedException(context);
            DatabaseName = (estring)Visit(context.database_name().complex_name());
            return new UseDatabaseStatement() { DatabaseName = DatabaseName };
        }
        public override object VisitCurrent_database_stmt(Current_database_stmtContext context)
        {
            throw new NotImplementedException();
        }
        public override object VisitList_databases_stmt(List_databases_stmtContext context)
        {
            throw new NotImplementedException();
        }
        public override object VisitGet_structure_stmt(Get_structure_stmtContext context)
        {
            throw new NotImplementedException();
        }
        public override object VisitCreate_database_stmt([NotNull] Create_database_stmtContext context)
        {
            CheckIfParserThrowedException(context);
            DatabaseName = (estring)Visit(context.database_name().complex_name());
            return new CreateDatabaseStatement() { DatabaseName = DatabaseName };
        }
        public override object VisitDrop_database_stmt([NotNull] Drop_database_stmtContext context)
        {
            CheckIfParserThrowedException(context);
            DatabaseName = null;
            return new DropDatabaseStatement() { DatabaseName = (estring)Visit(context.database_name().complex_name()) };
        }


        public override object VisitCreate_table_stmt(Create_table_stmtContext context)
        {
            CheckIfDatabaseAlreadyChosen();
            CheckIfParserThrowedException(context);

            return new CreateTableStatement()
            {
                TableName = (estring)Visit(context.table_name().complex_name()),
                ColumnDefinitions = context.column_def().Select(c => (ColumnDefinition)Visit(c)).ToList()
            };
        }
        public override object VisitDrop_table_stmt(Drop_table_stmtContext context)
        {
            CheckIfDatabaseAlreadyChosen();
            CheckIfParserThrowedException(context);
            return new DropTableStatement() { TableName = (estring)Visit(context.table_name().complex_name()) };
        }
        public override object VisitAlter_table_stmt(Alter_table_stmtContext context)
        {
            CheckIfDatabaseAlreadyChosen();
            CheckIfParserThrowedException(context);

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
            CheckIfDatabaseAlreadyChosen();
            CheckIfParserThrowedException(context);
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
                    insertRecordStatement.ValuesPerColumn[columnName].Add( new Value(context.literal_value()[j].GetText()));
                }
            }
            return insertRecordStatement;

        }
        public override object VisitUpdate_stmt(Update_stmtContext context)
        {
            CheckIfDatabaseAlreadyChosen();
            CheckIfParserThrowedException(context);
            var updateRecordStatement = new UpdateRecordStatement()
            {
                TableName = (estring)Visit(context.table_name().complex_name()),
                ColumnNamesAndUpdateValues = new Dictionary<estring, Value>()
            };

            if (context.K_WHERE() != null)
                if (context.K_WHERE() != null)
                {
     
                    updateRecordStatement.WhereExpression = (AbstractExpression)Visit(context.expr());
                }

            for (int i = 0; i < context.literal_value().Length; i++)
            {

                updateRecordStatement.ColumnNamesAndUpdateValues.Add(
                    (estring)Visit(context.column_name()[i].complex_name()),
                    new Value(context.literal_value()[i].GetText())
                    );
            }

            return updateRecordStatement;
        }
        public override object VisitDelete_stmt(Delete_stmtContext context)
        {
            CheckIfDatabaseAlreadyChosen();
            CheckIfParserThrowedException(context);
            return new DeleteRecordStatement()
            {
                TableName = (estring)Visit(context.table_name().complex_name()),
                WhereClause = context.expr() != null ? (AbstractExpression)Visit(context.expr()) : null
            };

        }

        public override object VisitSimple_select_stmt([NotNull] Simple_select_stmtContext context)
        {
            CheckIfDatabaseAlreadyChosen();
            CheckIfParserThrowedException(context);
            var simpleSelectStatement = new SimpleSelectStatement()
            {
                SelectCoreStatement = (SelectCoreStatement)Visit(context.select_core()),
                OrderingTerms = context.ordering_term().Select(o => (OrderingTerm)Visit(o)).ToList()
            };

            if (context.K_LIMIT() != null)
                simpleSelectStatement.Limit = Int32.Parse(context.literal_value()[0].GetText());
            if (context.K_OFFSET() != null)
                simpleSelectStatement.Offset = Int32.Parse(context.literal_value()[1].GetText());

            return simpleSelectStatement;
        }
        public override object VisitSelect_core(Select_coreContext context)
        {
            CheckIfDatabaseAlreadyChosen();
            CheckIfParserThrowedException(context);
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
            if (context.expr() != null) selectCoreStatement.WhereExpression = (AbstractExpression)Visit(context.expr());
            return selectCoreStatement;

        }


        public override object VisitColumn_def(Column_defContext columnDefContext)
        {
            CheckIfParserThrowedException(columnDefContext);

            var columnDef = new ColumnDefinition
            {
                ColumnName = (estring)Visit(columnDefContext.column_name().complex_name()),
                DataType = (DataType)Visit(columnDefContext.data_type()),
                ColumnConstraints = columnDefContext.column_constraint().Select(c => (ColumnConstraint)Visit(c)).ToList()
            };

            if (columnDef.DataType.DataTypeName == DataTypeEnum.ENCRYPTED && columnDef.DataType.BucketInfo.EqualityBucketSize == null
                && columnDef.ColumnConstraints.Count(c => c.ColumnConstraintType == ColumnConstraint.ColumnConstraintTypeEnum.PrimaryKey ||
                c.ColumnConstraintType == ColumnConstraint.ColumnConstraintTypeEnum.Unique) == 0)
            {
                throw new FormatException("If the column is not unique or primary key you need to specify the number of equality buckets you desire.");
            }

            if (columnDef.DataType.DataTypeName == DataTypeEnum.ENCRYPTED && columnDef.DataType.BucketInfo.EqualityBucketSize != null
                && columnDef.ColumnConstraints.Count(c => c.ColumnConstraintType == ColumnConstraint.ColumnConstraintTypeEnum.PrimaryKey ||
                c.ColumnConstraintType == ColumnConstraint.ColumnConstraintTypeEnum.Unique) != 0)
            {
                throw new FormatException("If the column is unique or primary key it doesn't need equality bucket size.");
            }

            return columnDef;

        }
        public override object VisitColumn_constraint(Column_constraintContext columnConstraintContext)
        {
            CheckIfParserThrowedException(columnConstraintContext);
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
            CheckIfParserThrowedException(foreignKeyClauseContext);
            return new ForeignKeyClause()
            {
                ForeignTableName = (estring)Visit(foreignKeyClauseContext.foreign_table().complex_name()),
                ColumnNames = foreignKeyClauseContext.column_name().Select(c => (estring)Visit(c.complex_name())).ToList()
            };
        }
        public override object VisitComplex_name(Complex_nameContext complexNameContext)
        {
            CheckIfParserThrowedException(complexNameContext);
            return new estring() { Value = complexNameContext.any_name().GetText(), ToEncrypt = complexNameContext.K_NOT_TO_ENCRYPT() == null };
        }

        public override object VisitData_type(Data_typeContext dataTypeContext)
        {
            CheckIfParserThrowedException(dataTypeContext);
            if (dataTypeContext.K_BOOL() != null) return new DataType() { DataTypeName = DataTypeEnum.BOOL };
            if (dataTypeContext.K_DATETIME() != null) return new DataType() { DataTypeName = DataTypeEnum.DATETIME };
            if (dataTypeContext.K_DECIMAL() != null) return new DataType() { DataTypeName = DataTypeEnum.DECIMAL };
            if (dataTypeContext.K_DOUBLE() != null) return new DataType() { DataTypeName = DataTypeEnum.DOUBLE };
            if (dataTypeContext.K_DURATION() != null) return new DataType() { DataTypeName = DataTypeEnum.DURATION };
            if (dataTypeContext.K_INT() != null) return new DataType() { DataTypeName = DataTypeEnum.INT };
            if (dataTypeContext.K_TEXT() != null) return new DataType() { DataTypeName = DataTypeEnum.TEXT };
            if (dataTypeContext.K_ENCRYPTED() != null)
            {
                var dataType = new DataType() { DataTypeName = DataTypeEnum.ENCRYPTED, BucketInfo = new BucketInfo() };
                if (dataTypeContext.bucket_size() != null)
                {
                    dataType.BucketInfo.EqualityBucketSize = Int32.Parse(dataTypeContext.bucket_size().NUMERIC_LITERAL().GetText());                    
                }

                if (dataTypeContext.K_RANGE() != null)
                {
                    var bktSizeRange = (Tuple<int, int, int>)Visit(dataTypeContext.bucket_range());
                    dataType.BucketInfo.RangeBucketSize = bktSizeRange.Item1;
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
            CheckIfParserThrowedException(bucketRangeContext);
         
            var size = Int32.Parse(bucketRangeContext.NUMERIC_LITERAL()[0].GetText());
            var min = Int32.Parse(bucketRangeContext.NUMERIC_LITERAL()[1].GetText());
            var max = Int32.Parse(bucketRangeContext.NUMERIC_LITERAL()[2].GetText());

            return new Tuple<int, int, int> (size, min, max);
         
        }

        public override object VisitExpr(ExprContext expr)
        {
            CheckIfParserThrowedException(expr);
           
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

            var exprString = expr.GetText();
            if ( expr.table_name() != null && expr.column_name() != null && expr.literal_value() != null 
                && (exprString.Contains("<") || exprString.Contains("<=") || exprString.Contains(">")
                || exprString.Contains(">=") || exprString.Contains("==") || exprString.Contains("!=")))
            {
                var comparisonExpression = new ComparisonExpression()
                {
                    TableName = (estring) Visit(expr.table_name().complex_name()),
                    ColumnName = (estring)Visit(expr.column_name().complex_name()),
                    Value = new Value(expr.literal_value().GetText()),
                    ComparisonOperator = GetLogicalOperatorFromString(exprString)
                };
                return comparisonExpression;
            }

            if (exprString.Contains("(") && exprString.Contains(")"))
            {
                return (AbstractExpression)Visit(expr.expr()[0]);
            }

            throw new FormatException("Expression not recognized.");
        }

        public override object VisitOrdering_term(Ordering_termContext orderingTermContext)
        {
            CheckIfParserThrowedException(orderingTermContext);
            return new OrderingTerm()
            {
                Expression = (AbstractExpression)Visit(orderingTermContext.expr()),
                IsAscending = orderingTermContext.K_DESC() == null
            };
        }

        public override object VisitResult_column(Result_columnContext resultColumnContext)
        {
            CheckIfParserThrowedException(resultColumnContext);
            var allColumns = resultColumnContext.table_column_name().column_name() == null;
            return new ResultColumn()
            {
                ColumnName = !allColumns ? (estring)Visit(resultColumnContext.table_column_name().column_name().complex_name()) : null,
                TableName = !allColumns ? (estring)Visit(resultColumnContext.table_column_name().table_name().complex_name()) : null,
                AllColumnsfFlag = allColumns
            };
        }
        public override object VisitTable_or_subquery(Table_or_subqueryContext tableOrSubqueryContext)
        {
            CheckIfParserThrowedException(tableOrSubqueryContext);
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
            CheckIfParserThrowedException(joinClauseContext);
            var joinClause = new JoinClause()
            {
                TableOrSubquery = (TableOrSubquery)Visit(joinClauseContext.table_or_subquery()[0]),
                JoinOperationFields = new List<Tuple<IList<JoinClause.JoinOperatorEnum>, TableOrSubquery, JoinClause.JoinConstraint>>()
            };

            for (int i = 0; i < joinClauseContext.join_operator().Length; i++)
            {
                var joinOperationField = new Tuple<IList<JoinClause.JoinOperatorEnum>, TableOrSubquery, JoinClause.JoinConstraint>
                (
                    (List<JoinClause.JoinOperatorEnum>)Visit(joinClauseContext.join_operator()[i]),
                    (TableOrSubquery)Visit(joinClauseContext.table_or_subquery()[i + 1]),
                    (JoinClause.JoinConstraint)Visit(joinClauseContext.join_constraint()[i])
                );
                joinClause.JoinOperationFields.Add(joinOperationField);
            }
            return joinClause;
        }

        public override object VisitJoin_operator(Join_operatorContext joinOperatorContext)
        {
            CheckIfParserThrowedException(joinOperatorContext);
            var joinOperatorEnumList = new List<JoinClause.JoinOperatorEnum>();
            if (joinOperatorContext.K_NATURAL() != null) joinOperatorEnumList.Add(JoinClause.JoinOperatorEnum.NATURAL);
            if (joinOperatorContext.K_LEFT() != null) joinOperatorEnumList.Add(JoinClause.JoinOperatorEnum.LEFT);
            if (joinOperatorContext.K_OUTER() != null) joinOperatorEnumList.Add(JoinClause.JoinOperatorEnum.OUTER);
            if (joinOperatorContext.K_CROSS() != null) joinOperatorEnumList.Add(JoinClause.JoinOperatorEnum.CROSS);
            return joinOperatorEnumList;
        }

        public override object VisitJoin_constraint(Join_constraintContext joinConstraintContext)
        {
            CheckIfParserThrowedException(joinConstraintContext);
            return new JoinClause.JoinConstraint()
            {
                Expression = (AbstractExpression)Visit(joinConstraintContext.expr()),
                ColumnNames = joinConstraintContext.column_name().Select(c => (estring)Visit(c.complex_name())).ToList()
            };
        }

        #endregion Visit Statements

        #region Auxiliar Methods
        private ComparisonExpression.ComparisonOperatorEnum GetLogicalOperatorFromString(string exprString)
        {
            if (exprString.Contains("<="))
                return ComparisonExpression.ComparisonOperatorEnum.SmallerOrEqualThan;
            if (exprString.Contains(">="))
                return ComparisonExpression.ComparisonOperatorEnum.BiggerOrEqualThan;
            if (exprString.Contains("<"))
                return ComparisonExpression.ComparisonOperatorEnum.SmallerThan;
            if (exprString.Contains(">"))
                return ComparisonExpression.ComparisonOperatorEnum.BiggerThan;
            if (exprString.Contains("=="))
                return ComparisonExpression.ComparisonOperatorEnum.Equal;
            if (exprString.Contains("!="))
                return ComparisonExpression.ComparisonOperatorEnum.Different;

            throw new FormatException("No comparison operator in string.");
        }

        private void CheckIfDatabaseAlreadyChosen()
        {
            if (DatabaseName == null) throw new FormatException("Please use or create a database first.");
        }

        private void CheckIfParserThrowedException(ParserRuleContext context)
        {
            if (context.exception != null) throw context.exception;
        }

        #endregion Auxiliar Methods
    }
}