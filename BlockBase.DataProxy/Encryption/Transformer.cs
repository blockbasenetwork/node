using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System;
using System.Collections.Generic;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Database;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using static BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.ColumnConstraint;
using BlockBase.Domain.Database.Info;
using System.Linq;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using static BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions.ComparisonExpression;
using BlockBase.Domain.Database.Sql.SqlCommand;
using static BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions.LogicalExpression;

namespace BlockBase.DataProxy.Encryption
{
    public class Transformer
    {
        private static readonly estring INFO_TABLE_NAME = new estring(InfoTableConstants.INFO_TABLE_NAME);
        private static readonly estring NAME = new estring(InfoTableConstants.NAME);
        private static readonly estring DATA = new estring(InfoTableConstants.DATA);
        private static readonly estring KEY_NAME = new estring(InfoTableConstants.KEY_NAME);
        private static readonly estring KEY_MANAGE = new estring(InfoTableConstants.KEY_MANAGE);
        private static readonly estring PARENT = new estring(InfoTableConstants.PARENT);
        private static readonly estring IV = new estring(InfoTableConstants.IV);
        private InfoRecord _databaseInfoRecord = null;

        private IEncryptor _encryptor;
        private bool _isSelectStatementNeeded;

        public Transformer(MiddleMan middleMan)
        {
            _encryptor = middleMan;
        }

        public void TransformCommand(ISqlCommand command)
        {
            var statement = command.OriginalSqlStatement;
            switch (statement)
            {
                case IfStatement ifStatement:
                    CheckIfDatabaseAlreadyChosen();
                    command.TransformedSqlStatement = new List<ISqlStatement>() { GetTransformedIfStatment(ifStatement, _databaseInfoRecord.IV) };
                    break;
                case CreateDatabaseStatement createDatabaseStatement:
                    command.TransformedSqlStatement = GetTransformedCreateDatabaseStatement(createDatabaseStatement, out _databaseInfoRecord);
                    var createDatabaseCommand = (DatabaseSqlCommand)command;
                    createDatabaseCommand.DatabaseName = ((CreateDatabaseStatement)command.TransformedSqlStatement[0]).DatabaseName.Value;
                    Console.WriteLine(_databaseInfoRecord.IV);
                    break;

                case DropDatabaseStatement dropDatabaseStatement:
                    command.TransformedSqlStatement = GetTransformedDropDatabaseStatement(dropDatabaseStatement);
                    break;

                case UseDatabaseStatement useDatabaseStatement:
                    command.TransformedSqlStatement = new List<ISqlStatement>() { GetTransformedUseDatabaseStatement(useDatabaseStatement, out _databaseInfoRecord) };
                    var useDatabaseCommand = (DatabaseSqlCommand)command;
                    useDatabaseCommand.DatabaseName = ((UseDatabaseStatement)command.TransformedSqlStatement[0]).DatabaseName.Value;
                    break;

                case CreateTableStatement createTableStatement:
                    CheckIfDatabaseAlreadyChosen();
                    command.TransformedSqlStatement = GetTransformedCreateTableStatement(createTableStatement, _databaseInfoRecord.IV);
                    break;

                case DropTableStatement dropTableStatement:
                    CheckIfDatabaseAlreadyChosen();
                    command.TransformedSqlStatement = GetTransformedDropTableStatement(dropTableStatement, _databaseInfoRecord.IV);
                    break;

                case AbstractAlterTableStatement abstractAlterTableStatement:
                    CheckIfDatabaseAlreadyChosen();
                    command.TransformedSqlStatement = GetTransformedAlterTableStatement(abstractAlterTableStatement, _databaseInfoRecord.IV);
                    break;

                case InsertRecordStatement insertRecordStatement:
                    CheckIfDatabaseAlreadyChosen();
                    command.TransformedSqlStatement = new List<ISqlStatement>() { GetTransformedInsertRecordStatement(insertRecordStatement, _databaseInfoRecord.IV) };
                    break;

                case SimpleSelectStatement simpleSelectStatement:
                    CheckIfDatabaseAlreadyChosen();
                    command.TransformedSqlStatement = new List<ISqlStatement>() { GetTransformedSimpleSelectStatement(simpleSelectStatement, _databaseInfoRecord.IV) };
                    break;

                case UpdateRecordStatement updateRecordStatement:
                    CheckIfDatabaseAlreadyChosen();
                    command.TransformedSqlStatement = GetTransformedUpdateRecordStatement(updateRecordStatement, _databaseInfoRecord.IV);
                    break;

                case DeleteRecordStatement deleteRecordStatement:
                    CheckIfDatabaseAlreadyChosen();
                    command.TransformedSqlStatement = GetTransformedDeleteRecordStatement(deleteRecordStatement, _databaseInfoRecord.IV);
                    break;

            }
        }
        #region Transform SqlStatements

        private ISqlStatement GetTransformedIfStatment(IfStatement ifStatement, string databaseIV)
        {
            return GetTransformedSimpleSelectStatement(ifStatement.SimpleSelectStatement, databaseIV);
        }
        private List<ISqlStatement> GetTransformedCreateDatabaseStatement(CreateDatabaseStatement createDatabaseStatement, out InfoRecord databaseInfoRecord)
        {
            databaseInfoRecord = _encryptor.CreateInfoRecord(createDatabaseStatement.DatabaseName, null);

            return new List<ISqlStatement>()
            {
                new CreateDatabaseStatement(new estring(databaseInfoRecord.Name)),
                CreateInfoTable(),
                CreateInsertRecordStatementForInfoTable(databaseInfoRecord)
            };
        }
        private List<ISqlStatement> GetTransformedDropDatabaseStatement(DropDatabaseStatement dropDatabaseStatement)
        {
            var infoRecord = GetInfoRecordThrowErrorIfNotExists(dropDatabaseStatement.DatabaseName, null);

            var sqlStatements = new List<ISqlStatement>()
            {
                new DropDatabaseStatement(new estring(infoRecord.Name))
            };

            //Removes all database tables and columns info records
            var childrenInfoRecords = _encryptor.FindChildren(infoRecord.IV, true);
            _encryptor.RemoveInfoRecord(infoRecord);
            foreach (var child in childrenInfoRecords) _encryptor.RemoveInfoRecord(child);

            return sqlStatements;
        }
        private UseDatabaseStatement GetTransformedUseDatabaseStatement(UseDatabaseStatement useDatabaseStatement, out InfoRecord databaseInfoRecord)
        {
            databaseInfoRecord = GetInfoRecordThrowErrorIfNotExists(useDatabaseStatement.DatabaseName, null);
            return new UseDatabaseStatement(new estring(databaseInfoRecord.Name));
        }

        private IList<ISqlStatement> GetTransformedCreateTableStatement(CreateTableStatement createTableStatement, string databaseIV)
        {
            var tableInfoRecord = _encryptor.CreateInfoRecord(createTableStatement.TableName, databaseIV);
            var transformedCreateTableStatement = new CreateTableStatement(new estring(tableInfoRecord.Name));

            var transformedStatements = new List<ISqlStatement>()
            {
                transformedCreateTableStatement,
                CreateInsertRecordStatementForInfoTable(tableInfoRecord)
            };

            //marciak - adds the information (like IV and keys) about each column 
            foreach (var columnDef in createTableStatement.ColumnDefinitions)
            {
                var additionalColAndInfoInserts = GetTransformedColumnDefinition(columnDef, tableInfoRecord.IV, databaseIV);

                ((List<ColumnDefinition>)transformedCreateTableStatement.ColumnDefinitions).AddRange(additionalColAndInfoInserts.Item1);
                transformedStatements.Add(additionalColAndInfoInserts.Item2);
            }

            return transformedStatements;
        }
        private IList<ISqlStatement> GetTransformedDropTableStatement(DropTableStatement dropTableStatement, string databaseIV)
        {
            var infoRecord = GetInfoRecordThrowErrorIfNotExists(dropTableStatement.TableName, databaseIV);

            var sqlStatements = new List<ISqlStatement>()
            {
                new DropTableStatement(new estring(infoRecord.Name))
            };

            var childrenInfoRecords = _encryptor.FindChildren(infoRecord.IV, true);
            sqlStatements.Add(CreateDeleteRecordStatementForInfoTable(infoRecord.IV));

            foreach (var child in childrenInfoRecords)
            {
                sqlStatements.Add(CreateDeleteRecordStatementForInfoTable(child.IV));
            }

            _encryptor.RemoveInfoRecord(infoRecord);

            return sqlStatements;
        }

        private IList<ISqlStatement> GetTransformedAlterTableStatement(AbstractAlterTableStatement alterTableStatement, string databaseIV)
        {
            switch (alterTableStatement)
            {
                case RenameTableStatement renameTableStatement:
                    return new List<ISqlStatement>(GetTransformedRenameTableStatement(renameTableStatement, databaseIV));

                case RenameColumnStatement renameColumnStatement:
                    return new List<ISqlStatement>(GetTransformedRenameColumnStatement(renameColumnStatement, databaseIV));

                case AddColumnStatement addColumnStatement:
                    return new List<ISqlStatement>(GetTransformedAddColumnStatement(addColumnStatement, databaseIV));

                case DropColumnStatement dropColumnStatement:
                    return new List<ISqlStatement>(GetTransformedDropColumnStatement(dropColumnStatement, databaseIV));
            }

            throw new FormatException("Alter table statement type not recognized.");
        }

        private IList<ISqlStatement> GetTransformedRenameTableStatement(RenameTableStatement renameTableStatement, string databaseIV)
        {
            var sqlStatements = new List<ISqlStatement>();

            var tableInfoRecord = GetInfoRecordThrowErrorIfNotExists(renameTableStatement.TableName, databaseIV);
            var transformedOldTableName = tableInfoRecord.Name;

            tableInfoRecord = _encryptor.ChangeInfoRecordName(tableInfoRecord, renameTableStatement.NewTableName);
            var transformedNewTableName = tableInfoRecord.Name;

            sqlStatements.Add(new RenameTableStatement(new estring(transformedOldTableName), new estring(transformedNewTableName)));

            sqlStatements.Add(CreateUpdateToChangeInfoRecordName(transformedNewTableName, tableInfoRecord.IV, tableInfoRecord.KeyName));

            return sqlStatements;
        }
        private IList<ISqlStatement> GetTransformedRenameColumnStatement(RenameColumnStatement renameColumnStatement, string databaseIV)
        {
            var sqlStatements = new List<ISqlStatement>();

            var tableInfoRecord = GetInfoRecordThrowErrorIfNotExists(renameColumnStatement.TableName, databaseIV);

            var columnInfoRecord = GetInfoRecordThrowErrorIfNotExists(renameColumnStatement.ColumnName, tableInfoRecord.IV);
            var oldInfoRecord = columnInfoRecord.Clone();

            columnInfoRecord = _encryptor.ChangeInfoRecordName(columnInfoRecord, renameColumnStatement.NewColumnName);


            sqlStatements.Add(
                new RenameColumnStatement(
                    new estring(tableInfoRecord.Name),
                    new estring(oldInfoRecord.Name),
                    new estring(columnInfoRecord.Name)));

            if (columnInfoRecord.LData.EncryptedEqualityColumnName != null)
                sqlStatements.Add(new RenameColumnStatement(new estring(tableInfoRecord.Name),
                new estring(oldInfoRecord.LData.EncryptedEqualityColumnName),
                new estring(columnInfoRecord.LData.EncryptedEqualityColumnName)));

            if (columnInfoRecord.LData.EncryptedIVColumnName != null)
                sqlStatements.Add(new RenameColumnStatement(new estring(tableInfoRecord.Name),
                new estring(oldInfoRecord.LData.EncryptedIVColumnName),
                new estring(columnInfoRecord.LData.EncryptedIVColumnName)));

            if (columnInfoRecord.LData.EncryptedRangeColumnName != null)
                sqlStatements.Add(new RenameColumnStatement(new estring(tableInfoRecord.Name),
                new estring(oldInfoRecord.LData.EncryptedRangeColumnName),
                new estring(columnInfoRecord.LData.EncryptedRangeColumnName)));

            sqlStatements.Add(CreateUpdateToChangeInfoRecordName(columnInfoRecord.Name, columnInfoRecord.IV, columnInfoRecord.KeyName));
            return sqlStatements;
        }
        private IList<ISqlStatement> GetTransformedAddColumnStatement(AddColumnStatement addColumnStatement, string databaseIV)
        {
            var sqlStatements = new List<ISqlStatement>();
            var tableInfoRecord = GetInfoRecordThrowErrorIfNotExists(addColumnStatement.TableName, databaseIV);

            var columnDefinitionsAndAdditionalInsert = GetTransformedColumnDefinition(addColumnStatement.ColumnDefinition, tableInfoRecord.IV, databaseIV);

            foreach (var columnDef in columnDefinitionsAndAdditionalInsert.Item1)
            {
                sqlStatements.Add(new AddColumnStatement(new estring(tableInfoRecord.Name), columnDef));
            }
            sqlStatements.Add(columnDefinitionsAndAdditionalInsert.Item2);
            return sqlStatements;
        }

        private IList<ISqlStatement> GetTransformedDropColumnStatement(DropColumnStatement dropColumnStatement, string databaseIV)
        {
            var tableInfoRecord = GetInfoRecordThrowErrorIfNotExists(dropColumnStatement.TableName, databaseIV);

            var columnInfoRecord = GetInfoRecordThrowErrorIfNotExists(dropColumnStatement.ColumnName, tableInfoRecord.IV);

            var sqlStatements = new List<ISqlStatement>()
            {
                new DropColumnStatement(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.Name))
            };

            if (columnInfoRecord.LData.EncryptedEqualityColumnName != null)
                sqlStatements.Add(new DropColumnStatement(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.LData.EncryptedEqualityColumnName)));

            if (columnInfoRecord.LData.EncryptedIVColumnName != null)
                sqlStatements.Add(new DropColumnStatement(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.LData.EncryptedIVColumnName)));

            if (columnInfoRecord.LData.EncryptedRangeColumnName != null)
                sqlStatements.Add(new DropColumnStatement(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.LData.EncryptedRangeColumnName)));

            sqlStatements.Add(CreateDeleteRecordStatementForInfoTable(columnInfoRecord.IV));
            _encryptor.RemoveInfoRecord(columnInfoRecord);



            return sqlStatements;
        }

        private ISqlStatement GetTransformedInsertRecordStatement(InsertRecordStatement insertRecordStatement, string databaseIV)
        {
            var tableInfoRecord = GetInfoRecordThrowErrorIfNotExists(insertRecordStatement.TableName, databaseIV);

            var transformedInsertRecordStatement = new InsertRecordStatement(new estring(tableInfoRecord.Name));

            foreach (var valuesPerColumn in insertRecordStatement.ValuesPerColumn)
            {
                var transformedValuesPerColumn = TransformColumnValues(valuesPerColumn, tableInfoRecord.IV);
                foreach (var keyPair in transformedValuesPerColumn) transformedInsertRecordStatement.ValuesPerColumn.Add(keyPair);
            }

            return transformedInsertRecordStatement;
        }

        private ISqlStatement GetTransformedSimpleSelectStatement(SimpleSelectStatement simpleSelectStatement, string databaseIV)
        {
            _isSelectStatementNeeded = false;
            var transformedSimpleSelectStatement = new SimpleSelectStatement();
            transformedSimpleSelectStatement.SelectCoreStatement = (SelectCoreStatement)GetTransformedSelectCoreStatement(simpleSelectStatement.SelectCoreStatement, databaseIV);
            if (simpleSelectStatement.Limit != null)
            {
                if (_isSelectStatementNeeded)
                    transformedSimpleSelectStatement.Limit = simpleSelectStatement.Limit + simpleSelectStatement.Offset ?? 0;
                else
                {
                    transformedSimpleSelectStatement.Limit = simpleSelectStatement.Limit;
                    transformedSimpleSelectStatement.Offset = simpleSelectStatement.Offset;
                }
            }
            return transformedSimpleSelectStatement;
        }
        private ISqlStatement GetTransformedSelectCoreStatement(SelectCoreStatement selectCoreStatement, string databaseIV)
        {
            //TODO transform for case statement
            var transformedSelectStatement = new SelectCoreStatement();
            var listOfResultColumnsFromCaseExpressions = new List<ResultColumn>();
            var listOfCaseExpressions = new List<CaseExpression>();
            var listOfAllResultColumns = new List<ResultColumn>();

            listOfAllResultColumns.AddRange(selectCoreStatement.ResultColumns);

            if(selectCoreStatement.CaseExpressions.Count != 0){
                foreach(var abstractExpression in selectCoreStatement.CaseExpressions){
                    var caseExpression = abstractExpression as CaseExpression;
                    if(!listOfAllResultColumns.Contains(caseExpression.ResultColumn)){
                        listOfAllResultColumns.Add(caseExpression.ResultColumn);
        
                    }
                    listOfResultColumnsFromCaseExpressions.Add(caseExpression.ResultColumn);
                    listOfCaseExpressions.Add(caseExpression);
                }
            }

            foreach (var resultColumn in listOfAllResultColumns)
            {
                var tableInfoRecord = GetInfoRecordThrowErrorIfNotExists(resultColumn.TableName, databaseIV);
                if(listOfResultColumnsFromCaseExpressions.Contains(resultColumn)){
                    if (!resultColumn.AllColumnsfFlag)
                    {
                        var caseExpression = listOfCaseExpressions.FirstOrDefault(x => x.ResultColumn.Equals(resultColumn));
                        var columnInfoRecordIsNotNull = false;
                        foreach(var whenThenExpression in caseExpression.WhenThenExpressions){
                            var columnInfoRecord = GetInfoRecordReturnNullIfNotExists(whenThenExpression.WhenExpression.LeftTableNameAndColumnName.ColumnName, tableInfoRecord.IV);
                            
                            columnInfoRecordIsNotNull = columnInfoRecord != null;
                        }
                        var newResultColumn = new ResultColumn(){
                            TableName = resultColumn.TableName,
                            ColumnName = resultColumn.ColumnName,
                            AllColumnsfFlag = false
                        };
                        if(columnInfoRecordIsNotNull  && !transformedSelectStatement.ResultColumns.Contains(newResultColumn)){
                            transformedSelectStatement.ResultColumns.Add(newResultColumn);
                        }
                    }
                } 
                else {

                    if (!resultColumn.AllColumnsfFlag)
                    {

                        var columnInfoRecord = GetInfoRecordReturnNullIfNotExists(resultColumn.ColumnName, tableInfoRecord.IV);

                        if(columnInfoRecord != null){
                            transformedSelectStatement.ResultColumns.Add(new ResultColumn()
                            {
                                TableName = new estring(tableInfoRecord.Name),
                                ColumnName = new estring(columnInfoRecord.Name),
                                AllColumnsfFlag = false
                            });

                            if (columnInfoRecord.LData.EncryptedIVColumnName != null)
                        {

                            transformedSelectStatement.ResultColumns.Add(new ResultColumn()
                            {
                                TableName = new estring(tableInfoRecord.Name),
                                ColumnName = new estring(columnInfoRecord.LData.EncryptedIVColumnName),
                                AllColumnsfFlag = false
                            });
                        }
                        }
                    }

                    else
                    {
                        var columnInfoRecords = _encryptor.FindChildren(tableInfoRecord.IV);

                        foreach (var columnInfoRecord in columnInfoRecords)
                        {
                            transformedSelectStatement.ResultColumns.Add(new ResultColumn()
                            {
                                TableName = new estring(tableInfoRecord.Name),
                                ColumnName = new estring(columnInfoRecord.Name),
                                AllColumnsfFlag = false
                            });

                            if (columnInfoRecord.LData.EncryptedIVColumnName != null)
                            {

                                transformedSelectStatement.ResultColumns.Add(new ResultColumn()
                                {
                                    TableName = new estring(tableInfoRecord.Name),
                                    ColumnName = new estring(columnInfoRecord.LData.EncryptedIVColumnName),
                                    AllColumnsfFlag = false
                                });
                            }
                        }
                    }
                }
            }

            if (selectCoreStatement.JoinClause != null) transformedSelectStatement.JoinClause = GetTransformedJoinClause(selectCoreStatement.JoinClause, databaseIV);

            foreach(var caseExpression in listOfCaseExpressions){
                transformedSelectStatement.CaseExpressions.Add(GetTransformedExpression(caseExpression, databaseIV, transformedSelectStatement));
            }
            transformedSelectStatement.TablesOrSubqueries = selectCoreStatement.TablesOrSubqueries.Select(t => GetTransformedTableOrSubquery(t, databaseIV)).ToList();

            //transformedSelectStatement.CaseExpression = GetTransformedExpression(selectCoreStatement.CaseExpression, databaseIV, transformedSelectStatement);

            transformedSelectStatement.WhereExpression = GetTransformedExpression(selectCoreStatement.WhereExpression, databaseIV, transformedSelectStatement);

            return transformedSelectStatement;
        }

        private IList<ISqlStatement> GetTransformedUpdateRecordStatement(UpdateRecordStatement updateRecordStatement, string databaseIV)
        {
            //TODO: UPDATE FOR OTHER EXPRESSIONS IN VALUE
            _isSelectStatementNeeded = false;
            var sqlStatements = new List<ISqlStatement>();

            var selectStatement = new SimpleSelectStatement();

            var transformedUpdateRecordStatement = new UpdateRecordStatement();

            var tableInfoRecord = GetInfoRecordThrowErrorIfNotExists(updateRecordStatement.TableName, databaseIV);

            transformedUpdateRecordStatement.TableName = new estring(tableInfoRecord.Name);
            

            foreach(var expression in updateRecordStatement.CaseExpressions){
                var caseExpression = expression as CaseExpression;
                selectStatement.SelectCoreStatement.CaseEncryptedFlag = true;
                selectStatement.SelectCoreStatement.CaseExpressions.Add(caseExpression);
                selectStatement.SelectCoreStatement.ResultColumns.Add(caseExpression.ResultColumn);
            }

            foreach (var columnValue in updateRecordStatement.ColumnNamesAndUpdateValues) //TODO remove or put case values in columnname?
            {
                var columnInfoRecord = GetInfoRecordThrowErrorIfNotExists(columnValue.Key, tableInfoRecord.IV);

                var columnDataType = columnInfoRecord.LData.DataType;
                

                if (columnValue.Value is CaseExpression)
                {
                    var columnCaseExpression = (CaseExpression)columnValue.Value;
                    if(columnDataType.DataTypeName == DataTypeEnum.ENCRYPTED)
                    {
                        if(columnInfoRecord.LData.EncryptedIVColumnName == null)
                        {
                            var listOfWhenThenExpressions = new List<WhenThenExpression>();
                            foreach(var whenThenExpression in columnCaseExpression.WhenThenExpressions)
                            {
                                var encryptedWhenThenExpression = new WhenThenExpression(
                                    whenThenExpression.WhenExpression,
                                    new LiteralValueExpression(new Value(_encryptor.EncryptUniqueValue(whenThenExpression.ThenExpression.LiteralValue.ValueToInsert,columnInfoRecord), true))
                                );
                                listOfWhenThenExpressions.Add(encryptedWhenThenExpression);
                            }
                            var elseExpression = new LiteralValueExpression(new Value(_encryptor.EncryptUniqueValue(columnCaseExpression.ElseExpression.LiteralValue.ValueToInsert, columnInfoRecord), true));
                            transformedUpdateRecordStatement.ColumnNamesAndUpdateValues.Add(
                            new estring(columnInfoRecord.Name),
                            new CaseExpression(listOfWhenThenExpressions, elseExpression)  
                            );
                        }
                        else
                        {
                            _isSelectStatementNeeded = true; 
                            selectStatement.SelectCoreStatement.ResultColumns.Add(new ResultColumn(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.Name)));
                            selectStatement.SelectCoreStatement.ResultColumns.Add(new ResultColumn(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.LData.EncryptedIVColumnName)));
                            selectStatement.SelectCoreStatement.TablesOrSubqueries.Add(new TableOrSubquery(new estring(tableInfoRecord.Name)));
                        }
                    }
                    else
                    {
                        //if (columnCaseExpression.LiteralValue.ValueToInsert.ToLower() != "null" && ( columnDataType.DataTypeName == DataTypeEnum.TEXT || columnDataType.DataTypeName == DataTypeEnum.DATETIME)) columnLiteralValue.LiteralValue.IsText = true;
                        transformedUpdateRecordStatement.ColumnNamesAndUpdateValues.Add(
                            new estring(columnInfoRecord.Name),
                            columnValue.Value
                            );
                    }
                } 
                else 
                {
                    var columnLiteralValue = (LiteralValueExpression)columnValue.Value;
                    if (columnDataType.DataTypeName == DataTypeEnum.ENCRYPTED)
                    {
                        if (columnInfoRecord.LData.EncryptedIVColumnName == null)
                            transformedUpdateRecordStatement.ColumnNamesAndUpdateValues.Add(
                            new estring(columnInfoRecord.Name),
                            new LiteralValueExpression(new Value(_encryptor.EncryptUniqueValue(columnLiteralValue.LiteralValue.ValueToInsert, columnInfoRecord), true))
                            );

                        else
                        {
                            _isSelectStatementNeeded = true; //marciak - if not unique we need to use buckets so we need to execute a select stament before
                            selectStatement.SelectCoreStatement.ResultColumns.Add(new ResultColumn(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.Name)));
                            selectStatement.SelectCoreStatement.ResultColumns.Add(new ResultColumn(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.LData.EncryptedIVColumnName)));
                            selectStatement.SelectCoreStatement.TablesOrSubqueries.Add(new TableOrSubquery(new estring(tableInfoRecord.Name)));
                        }
                    }

                    else
                    {
                        if (columnLiteralValue.LiteralValue.ValueToInsert.ToLower() != "null" && ( columnDataType.DataTypeName == DataTypeEnum.TEXT || columnDataType.DataTypeName == DataTypeEnum.DATETIME)) columnLiteralValue.LiteralValue.IsText = true;
                        transformedUpdateRecordStatement.ColumnNamesAndUpdateValues.Add(
                            new estring(columnInfoRecord.Name),
                            columnValue.Value
                            );
                    }
                }
            }
            
            selectStatement.SelectCoreStatement.WhereExpression = GetTransformedExpression(updateRecordStatement.WhereExpression, databaseIV, selectStatement.SelectCoreStatement);
            
            if(selectStatement.SelectCoreStatement.CaseEncryptedFlag) 
            {
                selectStatement.SelectCoreStatement = (SelectCoreStatement)GetTransformedSelectCoreStatement(selectStatement.SelectCoreStatement, databaseIV);
            }

            if (_isSelectStatementNeeded) sqlStatements.Add(selectStatement);

            if (transformedUpdateRecordStatement.ColumnNamesAndUpdateValues.Count != 0)
            {
                if (selectStatement.SelectCoreStatement.WhereExpression != null) transformedUpdateRecordStatement.WhereExpression = selectStatement.SelectCoreStatement.WhereExpression.Clone();
                if (selectStatement.SelectCoreStatement.CaseExpressions.Count != 0) 
                {
                    (transformedUpdateRecordStatement.CaseExpressions as List<AbstractExpression>).AddRange(selectStatement.SelectCoreStatement.CaseExpressions);

                }
                sqlStatements.Add(transformedUpdateRecordStatement);
            }

            return sqlStatements;
        }

        private IList<ISqlStatement> GetTransformedDeleteRecordStatement(DeleteRecordStatement deleteRecordStatement, string databaseIV)
        {
            _isSelectStatementNeeded = false;
            var sqlStatements = new List<ISqlStatement>();

            var selectStatement = new SimpleSelectStatement();

            var transformedDeleteRecordStatement = new DeleteRecordStatement();

            var tableInfoRecord = GetInfoRecordThrowErrorIfNotExists(deleteRecordStatement.TableName, databaseIV);

            transformedDeleteRecordStatement.TableName = new estring(tableInfoRecord.Name);

            selectStatement.SelectCoreStatement.WhereExpression = GetTransformedExpression(deleteRecordStatement.WhereExpression, databaseIV, selectStatement.SelectCoreStatement);
            if (_isSelectStatementNeeded) sqlStatements.Add(selectStatement);

            if (selectStatement.SelectCoreStatement.WhereExpression != null) transformedDeleteRecordStatement.WhereExpression = selectStatement.SelectCoreStatement.WhereExpression.Clone();
            sqlStatements.Add(transformedDeleteRecordStatement);

            return sqlStatements;
        }

        private JoinClause GetTransformedJoinClause(JoinClause joinClause, string databaseIV)
        {
            var rightTableOrSubquery = GetTransformedTableOrSubquery(joinClause.TableOrSubquery, databaseIV);
            var transformedOperationFields = new List<JoinOperationField>();

            foreach (var operationField in joinClause.JoinOperationFields)
            {
                transformedOperationFields.Add(GetTransformedJoinOperationField(operationField, databaseIV));
            }
            return new JoinClause(rightTableOrSubquery, transformedOperationFields);

        }

        private TableOrSubquery GetTransformedTableOrSubquery(TableOrSubquery tableOrSubquery, string databaseIV)
        {
            var transformedTableOrSubquery = new TableOrSubquery();

            if (tableOrSubquery.TableName != null)
            {
                transformedTableOrSubquery.TableName = new estring(GetInfoRecordThrowErrorIfNotExists(tableOrSubquery.TableName, databaseIV).Name);
            }

            if (tableOrSubquery.JoinClause != null)
            {
                transformedTableOrSubquery.JoinClause = GetTransformedJoinClause(tableOrSubquery.JoinClause, databaseIV);
            }

            if (tableOrSubquery.SimpleSelectStatement != null)
            {
                transformedTableOrSubquery.SimpleSelectStatement = (SimpleSelectStatement)GetTransformedSimpleSelectStatement(tableOrSubquery.SimpleSelectStatement, databaseIV);
            }

            foreach (var internalTableOrSubquery in tableOrSubquery.TablesOrSubqueries)
            {
                transformedTableOrSubquery.TablesOrSubqueries.Add(GetTransformedTableOrSubquery(internalTableOrSubquery, databaseIV));
            }

            return transformedTableOrSubquery;
        }
        private JoinOperationField GetTransformedJoinOperationField(JoinOperationField joinOperationField, string databaseIV)
        {
            var transformedTableOrSubquery = GetTransformedTableOrSubquery(joinOperationField.RightTableOrSubquery, databaseIV);
            var transformedJoinClauseConstraint = new JoinOperationField.JoinConstraint();

            if (joinOperationField.JoinClauseConstraint.Expression != null)
            {
                transformedJoinClauseConstraint.Expression = GetTransformedExpression(joinOperationField.JoinClauseConstraint.Expression, databaseIV, new SelectCoreStatement());
            }

            return new JoinOperationField(new List<JoinOperationField.JoinOperatorEnum>(joinOperationField.JoinOperators), transformedTableOrSubquery, transformedJoinClauseConstraint);

        }

        private AbstractExpression GetTransformedExpression(AbstractExpression expression, string databaseIV, SelectCoreStatement transformedSelectCoreStatement)
        {
            switch (expression)
            {
                case LiteralValueExpression literalValueExpression: //TODO Need to transform 
                    return literalValueExpression;
                case ComparisonExpression comparisonExpression:
                    return GetTransformedComparisonExpression(comparisonExpression, databaseIV, transformedSelectCoreStatement);

                case LogicalExpression logicalExpression:
                    var newLogicalExpression = new LogicalExpression
                    {
                        LeftExpression = GetTransformedExpression(logicalExpression.LeftExpression, databaseIV, transformedSelectCoreStatement),
                        RightExpression = GetTransformedExpression(logicalExpression.RightExpression, databaseIV, transformedSelectCoreStatement),
                        LogicalOperator = logicalExpression.LogicalOperator,
                        HasParenthesis = logicalExpression.HasParenthesis
                    };
                    return newLogicalExpression;
                case CaseExpression caseExpression:
                    
                    var listOfWhenThenExpressions = new List<WhenThenExpression>();
                    foreach(var whenThenExpression in caseExpression.WhenThenExpressions){
                        listOfWhenThenExpressions.Add(GetTransformedExpression(whenThenExpression, databaseIV, transformedSelectCoreStatement) as WhenThenExpression);
                    }
                    var newCaseExpression = new CaseExpression(){
                        WhenThenExpressions = listOfWhenThenExpressions,
                        ElseExpression = GetTransformedExpression(caseExpression.ElseExpression, databaseIV, transformedSelectCoreStatement) as LiteralValueExpression,
                        ResultColumn = caseExpression.ResultColumn  // am i supposed to transform the result column? 
                    };

                    return newCaseExpression;
                case WhenThenExpression whenThenExpression:
                    var newWhenThenExpression = new WhenThenExpression
                    {
                        WhenExpression = GetTransformedComparisonExpression(whenThenExpression.WhenExpression, databaseIV, transformedSelectCoreStatement) as ComparisonExpression,
                        ThenExpression = GetTransformedExpression(whenThenExpression.ThenExpression, databaseIV, transformedSelectCoreStatement) as LiteralValueExpression,
                        HasParenthesis = whenThenExpression.HasParenthesis
                    };
                    return newWhenThenExpression;
            }
            return null;
        }

        private AbstractExpression GetTransformedComparisonExpression(ComparisonExpression comparisonExpression, string databaseIV, SelectCoreStatement transformedSelectCoreStatement)
        {
            var leftTableInfoRecord = GetInfoRecordThrowErrorIfNotExists(comparisonExpression.LeftTableNameAndColumnName.TableName, databaseIV);

            var leftColumnInfoRecord = GetInfoRecordThrowErrorIfNotExists(comparisonExpression.LeftTableNameAndColumnName.ColumnName, leftTableInfoRecord.IV);

            var leftColumnDataType = leftColumnInfoRecord.LData.DataType;

            ComparisonExpression transformedComparisonExpression;

            if (comparisonExpression.Value == null)
            {
                // marciak - it's not possible to compare encrypted data column with another column, because for that it would be needed to get all the other column values and calculate the bucket for each
                if (leftColumnDataType.DataTypeName == DataTypeEnum.ENCRYPTED) throw new Exception("Can't compare encrypted data column with another column.");
                var rightTableInfoRecord = GetInfoRecordThrowErrorIfNotExists(comparisonExpression.RightTableNameAndColumnName.TableName, databaseIV);
                var rightColumnInfoRecord = GetInfoRecordThrowErrorIfNotExists(comparisonExpression.RightTableNameAndColumnName.ColumnName, rightTableInfoRecord.IV);

                var rightColumnDataType = rightColumnInfoRecord.LData.DataType; ;
                if (rightColumnDataType.DataTypeName == DataTypeEnum.ENCRYPTED) throw new Exception("Can't compare encrypted data column with another column.");

                transformedComparisonExpression = new ComparisonExpression(
                    new TableAndColumnName(new estring(leftTableInfoRecord.Name), new estring(leftColumnInfoRecord.Name)),
                    new TableAndColumnName(new estring(rightTableInfoRecord.Name), new estring(rightColumnInfoRecord.Name)),
                    comparisonExpression.ComparisonOperator);
                return transformedComparisonExpression;
            }

            if (comparisonExpression.Value.ValueToInsert.ToLower() != "null" && (leftColumnDataType.DataTypeName == DataTypeEnum.TEXT || leftColumnDataType.DataTypeName == DataTypeEnum.DATETIME)) comparisonExpression.Value.IsText = true;

            transformedComparisonExpression = new ComparisonExpression(
                    new TableAndColumnName(new estring(leftTableInfoRecord.Name), new estring(leftColumnInfoRecord.Name)),
                    comparisonExpression.Value,
                    comparisonExpression.ComparisonOperator);

            transformedSelectCoreStatement.TryAddResultColumn(new TableAndColumnName(new estring(leftTableInfoRecord.Name), new estring(leftColumnInfoRecord.Name)));
            transformedSelectCoreStatement.TryAddTable(new estring(leftTableInfoRecord.Name));

            if (leftColumnDataType.DataTypeName == DataTypeEnum.ENCRYPTED)
            {
                var isColumnUnique = leftColumnInfoRecord.LData.EncryptedIVColumnName == null;
                if (!isColumnUnique)
                {
                    transformedSelectCoreStatement.TryAddResultColumn(new TableAndColumnName(new estring(leftTableInfoRecord.Name), new estring(leftColumnInfoRecord.LData.EncryptedIVColumnName)));
                    _isSelectStatementNeeded = true;
                }

                // marciak - if = or !=
                if (comparisonExpression.ComparisonOperator == ComparisonOperatorEnum.Equal || comparisonExpression.ComparisonOperator == ComparisonOperatorEnum.Different)
                {
                    if (!isColumnUnique)
                    {
                        if (leftColumnInfoRecord.LData.EncryptedEqualityColumnName != null)
                        {
                            transformedComparisonExpression.LeftTableNameAndColumnName.ColumnName = new estring(leftColumnInfoRecord.LData.EncryptedEqualityColumnName);
                            transformedComparisonExpression.Value = new Value(_encryptor.CreateEqualityBktValue(comparisonExpression.Value.ValueToInsert, leftColumnInfoRecord, leftColumnDataType), true);
                        }
                        // marciak - range bucket value is used if equality bucket value was not specified
                        else
                        {
                            if (double.TryParse(comparisonExpression.Value.ValueToInsert, out double valueDoubleToInsert))
                            {
                                transformedComparisonExpression.LeftTableNameAndColumnName.ColumnName = new estring(leftColumnInfoRecord.LData.EncryptedRangeColumnName);
                                transformedComparisonExpression.Value = new Value(_encryptor.GetEqualRangeBktValue(valueDoubleToInsert, leftColumnInfoRecord, leftColumnDataType), true);
                            }
                        }
                    }
                    else
                        transformedComparisonExpression.Value = new Value(_encryptor.EncryptUniqueValue(comparisonExpression.Value.ValueToInsert, leftColumnInfoRecord), true);
                }
                else
                {
                    _isSelectStatementNeeded = true;
                    transformedComparisonExpression.LeftTableNameAndColumnName.ColumnName = new estring(leftColumnInfoRecord.LData.EncryptedRangeColumnName);
                    if (double.TryParse(comparisonExpression.Value.ValueToInsert, out double valueDoubleToInsert))
                    {
                        IList<string> bktValues;
                        if (comparisonExpression.ComparisonOperator == ComparisonOperatorEnum.BiggerOrEqualThan || comparisonExpression.ComparisonOperator == ComparisonOperatorEnum.BiggerThan)
                            bktValues = _encryptor.GetRangeBktValues(valueDoubleToInsert, leftColumnInfoRecord, leftColumnDataType, true);
                        else
                            bktValues = _encryptor.GetRangeBktValues(valueDoubleToInsert, leftColumnInfoRecord, leftColumnDataType, false);

                        return TransformBktValuesInLogicalExpression(bktValues, transformedComparisonExpression.LeftTableNameAndColumnName);
                    }
                    else
                        throw new Exception("Tried to compare variable that is not a number.");
                }
            }

            return transformedComparisonExpression;
        }


        #endregion

        private IDictionary<estring, IList<Value>> TransformColumnValues(KeyValuePair<estring, IList<Value>> columnValues, string tableIV)
        {
            var columnName = columnValues.Key;

            var valuesPerColumn = new Dictionary<estring, IList<Value>>();

            var columnInfoRecord = GetInfoRecordThrowErrorIfNotExists(columnName, tableIV);

            if (columnInfoRecord == null) throw new FieldAccessException("No column with that name.");

            var columnDataType = columnInfoRecord.LData.DataType;

            estring equalityBktColumnName = columnInfoRecord.LData.EncryptedEqualityColumnName != null ? new estring(columnInfoRecord.LData.EncryptedEqualityColumnName) : null;
            estring rangeBktColumnName = columnInfoRecord.LData.EncryptedRangeColumnName != null ? new estring(columnInfoRecord.LData.EncryptedRangeColumnName) : null;
            estring ivColumnName = columnInfoRecord.LData.EncryptedIVColumnName != null ? new estring(columnInfoRecord.LData.EncryptedIVColumnName) : null;

            bool isEncrypted = columnDataType.DataTypeName == DataTypeEnum.ENCRYPTED;
            bool isNotUnique = ivColumnName != null;

            valuesPerColumn[new estring(columnInfoRecord.Name)] = new List<Value>();

            if (isNotUnique) valuesPerColumn[ivColumnName] = new List<Value>();
            if (rangeBktColumnName != null) valuesPerColumn[rangeBktColumnName] = new List<Value>();
            if (equalityBktColumnName != null) valuesPerColumn[equalityBktColumnName] = new List<Value>();


            for (int i = 0; i < columnValues.Value.Count; i++)
            {

                if (isEncrypted)
                {
                    if (rangeBktColumnName != null)
                    {
                        bool tryParse = double.TryParse(columnValues.Value[i].ValueToInsert, out double doubleValue);
                        if (!tryParse) throw new FormatException("The value in a range column needs to be a number.");
                        var newRangeColumnValue = new Value(_encryptor.CreateRangeBktValue(doubleValue, columnInfoRecord, columnDataType), true);
                        valuesPerColumn[rangeBktColumnName].Add(newRangeColumnValue);
                    }
                    if (equalityBktColumnName != null)
                    {
                        var equalityBktValue = new Value(_encryptor.CreateEqualityBktValue(columnValues.Value[i].ValueToInsert, columnInfoRecord, columnDataType), true);
                        valuesPerColumn[equalityBktColumnName].Add(equalityBktValue);
                    }
                    if (isNotUnique)
                    {
                        valuesPerColumn[new estring(columnInfoRecord.Name)].Add(new Value(_encryptor.EncryptNormalValue(columnValues.Value[i].ValueToInsert, columnInfoRecord, out string generatedIV), true));
                        valuesPerColumn[ivColumnName].Add(new Value(generatedIV, true));
                    }
                    else
                    {
                        valuesPerColumn[new estring(columnInfoRecord.Name)].Add(new Value(_encryptor.EncryptUniqueValue(columnValues.Value[i].ValueToInsert, columnInfoRecord), true));
                    }
                }
                else
                    valuesPerColumn[new estring(columnInfoRecord.Name)].Add(new Value(columnValues.Value[i].ValueToInsert, columnValues.Value[i].IsText && (columnDataType.DataTypeName == DataTypeEnum.TEXT || columnDataType.DataTypeName == DataTypeEnum.DATETIME)));

            }
            return valuesPerColumn;
        }

        private Tuple<IList<ColumnDefinition>, InsertRecordStatement> GetTransformedColumnDefinition(ColumnDefinition columnDefinition, string tableIV, string databaseIV)
        {
            var columnInfoRecord = _encryptor.CreateColumnInfoRecord(columnDefinition.ColumnName, tableIV, columnDefinition);


            var transformedColumnDefinition = new ColumnDefinition(
                new estring(columnInfoRecord.Name),
                columnDefinition.DataType,
                new List<ColumnConstraint>());


            foreach (var columnConstraint in columnDefinition.ColumnConstraints)
            {
                transformedColumnDefinition.ColumnConstraints.Add(
                    new ColumnConstraint()
                    {
                        //TODO: encrypt column constraint name
                        Name = columnConstraint.Name,
                        ColumnConstraintType = columnConstraint.ColumnConstraintType,
                        ForeignKeyClause = columnConstraint.ForeignKeyClause != null ?
                        TransformForeignKeyClause(columnConstraint.ForeignKeyClause, databaseIV) : null
                    })
                    ;
            }

            var columnDefinitions = new List<ColumnDefinition>()
            {
                transformedColumnDefinition
            };

            if (columnInfoRecord.LData.EncryptedEqualityColumnName != null)
                columnDefinitions.Add(new ColumnDefinition(new estring(columnInfoRecord.LData.EncryptedEqualityColumnName), new DataType(DataTypeEnum.TEXT)));

            if (columnInfoRecord.LData.EncryptedIVColumnName != null)
                columnDefinitions.Add(new ColumnDefinition(new estring(columnInfoRecord.LData.EncryptedIVColumnName), new DataType(DataTypeEnum.TEXT)));

            if (columnInfoRecord.LData.EncryptedRangeColumnName != null)
                columnDefinitions.Add(new ColumnDefinition(new estring(columnInfoRecord.LData.EncryptedRangeColumnName), new DataType(DataTypeEnum.TEXT)));

            return new Tuple<IList<ColumnDefinition>, InsertRecordStatement>(columnDefinitions, CreateInsertRecordStatementForInfoTable(columnInfoRecord));
        }
        private ForeignKeyClause TransformForeignKeyClause(ForeignKeyClause foreignKeyClause, string databaseIV)
        {
            var tableInfoRecord = GetInfoRecordThrowErrorIfNotExists(foreignKeyClause.TableName, databaseIV);

            var transformedForeignKeyClause = new ForeignKeyClause(new estring(tableInfoRecord.Name));
            foreach (var columnName in foreignKeyClause.ColumnNames)
            {
                var columnInfoRecord = GetInfoRecordThrowErrorIfNotExists(columnName, tableInfoRecord.IV);
                transformedForeignKeyClause.ColumnNames.Add(new estring(columnInfoRecord.Name));
            }
            return transformedForeignKeyClause;
        }

        private CreateTableStatement CreateInfoTable()
        {
            return new CreateTableStatement()
            {
                TableName = INFO_TABLE_NAME,
                ColumnDefinitions = new List<ColumnDefinition>() {
                    new ColumnDefinition( NAME, new DataType(DataTypeEnum.TEXT), new List<ColumnConstraint>() { new ColumnConstraint { ColumnConstraintType = ColumnConstraintTypeEnum.NotNull } }),
                    new ColumnDefinition( IV, new DataType(DataTypeEnum.TEXT), new List<ColumnConstraint>() { new ColumnConstraint { ColumnConstraintType = ColumnConstraintTypeEnum.PrimaryKey } }),
                    new ColumnDefinition( PARENT, new DataType(DataTypeEnum.TEXT) ),
                    new ColumnDefinition( KEY_NAME, new DataType(DataTypeEnum.TEXT) ),
                    new ColumnDefinition( KEY_MANAGE, new DataType(DataTypeEnum.TEXT), new List<ColumnConstraint>() { new ColumnConstraint { ColumnConstraintType = ColumnConstraintTypeEnum.NotNull } }),
                    new ColumnDefinition( DATA, new DataType(DataTypeEnum.TEXT) )
                   }
            };
        }
        private InsertRecordStatement CreateInsertRecordStatementForInfoTable(InfoRecord infoRecord)
        {
            return new InsertRecordStatement()
            {
                TableName = INFO_TABLE_NAME,
                ValuesPerColumn = new Dictionary<estring, IList<Value>>()
                {
                    { NAME, new List<Value>() { new Value(infoRecord.Name, true) }  },
                    { IV, new List<Value>() { new Value(infoRecord.IV, true) }  },
                    { PARENT, infoRecord.ParentIV != null ? new List<Value>() { new Value(infoRecord.ParentIV, true) } : new List<Value>() { new Value("null", false) } },
                    { KEY_NAME, infoRecord.KeyName != null ? new List<Value>() { new Value(infoRecord.KeyName, true) } : new List<Value>() { new Value("null", false) }  },
                    { KEY_MANAGE, new List<Value>() { new Value(infoRecord.KeyManage, true) }  },
                    { DATA, infoRecord.Data != null ? new List<Value>() { new Value(infoRecord.Data, true) } : new List<Value>() { new Value("null", false) }  },
                }
            };
        }

        private DeleteRecordStatement CreateDeleteRecordStatementForInfoTable(string iv)
        {
            return new DeleteRecordStatement(INFO_TABLE_NAME,
                new ComparisonExpression(new TableAndColumnName(INFO_TABLE_NAME, IV), new Value(iv, true), ComparisonExpression.ComparisonOperatorEnum.Equal)
                    );
        }

        private UpdateRecordStatement CreateUpdateToChangeInfoRecordName(string name, string iv, string keyName)
        {
            return new UpdateRecordStatement(
                    INFO_TABLE_NAME,
                    new Dictionary<estring, AbstractExpression>() {
                        { NAME, new LiteralValueExpression(new Value(name, true)) },
                        { KEY_NAME, keyName != null ? new LiteralValueExpression(new Value(keyName, true)) : new LiteralValueExpression(new Value("null", false)) }
                    },
                    new ComparisonExpression(new TableAndColumnName(INFO_TABLE_NAME, IV),
                        new Value(iv, true),
                        ComparisonExpression.ComparisonOperatorEnum.Equal)
                    );

        }

        private AbstractExpression TransformBktValuesInLogicalExpression(IList<string> bktValues, TableAndColumnName tableAndColumnName)
        {
            var comparisonExpression = new ComparisonExpression(tableAndColumnName, new Value(bktValues[0], true), ComparisonOperatorEnum.Equal);
            if (bktValues.Count == 1) return comparisonExpression;

            var logicalExpression = new LogicalExpression(comparisonExpression, null, LogicalOperatorEnum.OR);
            AddBktValueExpression(bktValues, logicalExpression, 1, bktValues.Count - 1, tableAndColumnName);
            logicalExpression.HasParenthesis = true;
            return logicalExpression;
        }

        private AbstractExpression AddBktValueExpression(IList<string> bktValues, LogicalExpression logicalExpression, int depth, int maxDepth, TableAndColumnName tableAndColumnName)
        {
            if (depth == maxDepth)
            {
                logicalExpression.RightExpression = new ComparisonExpression(tableAndColumnName, new Value(bktValues[maxDepth], true), ComparisonOperatorEnum.Equal);
                return logicalExpression;
            }
            var newLogicalExpression = new LogicalExpression(new ComparisonExpression(tableAndColumnName, new Value(bktValues[depth], true), ComparisonOperatorEnum.Equal), null, LogicalOperatorEnum.OR);
            logicalExpression.RightExpression = newLogicalExpression;
            depth++;
            return AddBktValueExpression(bktValues, newLogicalExpression, depth, maxDepth, tableAndColumnName);
        }
        private void CheckIfDatabaseAlreadyChosen()
        {
            if (_databaseInfoRecord == null) throw new FormatException("Please use or create a database first.");
        }
        private InfoRecord GetInfoRecordThrowErrorIfNotExists(estring name, string parentIV)
        {
            var infoRecord = _encryptor.FindInfoRecord(name, parentIV);
            if (infoRecord == null) throw new Exception("Relation '" + name.Value + "' does not exist.");
            return infoRecord;
        }

        private InfoRecord GetInfoRecordReturnNullIfNotExists(estring name, string parentIV)
        {
            var infoRecord = _encryptor.FindInfoRecord(name, parentIV);
            if (infoRecord == null) return null;
            return infoRecord;
        }

    }
}
