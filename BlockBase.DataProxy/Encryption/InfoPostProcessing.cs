﻿using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using BlockBase.Domain.Database.Sql.SqlCommand;
using BlockBase.Domain.Pocos;
using System;
using System.Collections.Generic;
using System.Linq;


namespace BlockBase.DataProxy.Encryption
{
    public class InfoPostProcessing
    {
        private IEncryptor _encryptor;
        public InfoPostProcessing(MiddleMan middleMan)
        {
            _encryptor = middleMan;
        }


        public int TranslateSelectResults(SimpleSelectStatement originalSqlStatement, SimpleSelectStatement transformedSimpleSelectStatement, IList<IList<string>> allResults, string databaseName, List<IList<string>> alreadyReceivedRows, out IList<string> finalColumnNames, bool extraParsingNotNeeded)
        {
            if (!originalSqlStatement.SelectCoreStatement.Encrypted)
            {
                //marciak - decrypts all results received
                var decryptedResults = DecryptRows(transformedSimpleSelectStatement, allResults, databaseName, out IList<TableAndColumnName> columnNames);
                //marciak - filters extra values using original expression
                var filteredResults = FilterExpression(originalSqlStatement.SelectCoreStatement.WhereExpression, decryptedResults, columnNames);
                //marciak - removes extra info columns (equality and range columns)
                var removedExtraColumns = FilterSelectColumns(originalSqlStatement.SelectCoreStatement.ResultColumns, filteredResults, columnNames, databaseName, out IList<string> columnsToMantain);

                alreadyReceivedRows.AddRange(removedExtraColumns);
                finalColumnNames = columnsToMantain;

                if (extraParsingNotNeeded ||
                    (!originalSqlStatement.Offset.HasValue &&
                    (!originalSqlStatement.Limit.HasValue || originalSqlStatement.Limit == alreadyReceivedRows.Count())))
                    return 0;


                else if (originalSqlStatement.Offset.HasValue &&
                        (originalSqlStatement.Offset + originalSqlStatement.Limit == alreadyReceivedRows.Count() ||
                        allResults.Count() == 0))
                {
                    //marciak - removes offset rows
                    alreadyReceivedRows.RemoveRange(0, originalSqlStatement.Offset.Value <= alreadyReceivedRows.Count() ? originalSqlStatement.Offset.Value : alreadyReceivedRows.Count());
                    return 0;
                }
                else
                    return originalSqlStatement.Limit ?? 0;
            }
            //marciak - in encrypted mode, it doesn't guarantee the right offset and limit
            var encryptedColumnNames = transformedSimpleSelectStatement.SelectCoreStatement.ResultColumns.Select(r => r.TableName.Value + "." + r.ColumnName.Value).ToList();
            finalColumnNames = encryptedColumnNames;
            alreadyReceivedRows.AddRange(allResults);

            return 0;
        }

        public IList<IChangeRecordStatement> UpdateChangeRecordStatement(ChangeRecordSqlCommand changeRecordSqlCommand, IList<IList<string>> allResults, string databaseName)
        {
            var originalChangeRecordStatement = (IChangeRecordStatement)changeRecordSqlCommand.OriginalSqlStatement;
            var transformedSimpleSelectStatement = (SimpleSelectStatement)changeRecordSqlCommand.TransformedSqlStatement[0];
            var originalUpdateStatement = changeRecordSqlCommand.OriginalSqlStatement as UpdateRecordStatement;

            var decryptedResults = DecryptRows(transformedSimpleSelectStatement, allResults, databaseName, out IList<TableAndColumnName> columnNames);
            IList<IList<string>> filteredResults;
            if(originalUpdateStatement.CaseExpressions.Count != 0){ //TODO Only deals with 1 case at a time
                var firstCaseExpression = originalUpdateStatement.CaseExpressions.FirstOrDefault() as CaseExpression;
                var firstWhenThenExpression = firstCaseExpression.WhenThenExpressions.FirstOrDefault() as WhenThenExpression;
                filteredResults = FilterExpression(firstWhenThenExpression.WhenExpression, decryptedResults, columnNames);
                if(firstCaseExpression.ElseExpression!= null){
                    (filteredResults as List<IList<string>>).AddRange(FilterExpression(firstCaseExpression.ElseExpression,decryptedResults,columnNames));
                }
            } else {
                filteredResults = FilterExpression(originalChangeRecordStatement.WhereExpression, decryptedResults, columnNames);
            }

            var databaseInfoRecord = _encryptor.FindInfoRecord(new estring(databaseName), null);
            var tableInfoRecord = _encryptor.FindInfoRecord(originalChangeRecordStatement.TableName, databaseInfoRecord.IV);

            var changeRecordStatements = new List<IChangeRecordStatement>();
            //marciak - adding additional updates like ivs and new encrypted values
            if (changeRecordSqlCommand.OriginalSqlStatement is UpdateRecordStatement originalUpdateRecordStatement)
            {
                var additionalUpdateRecordStatements = GetAdditionalUpdateRecordStatements(originalUpdateRecordStatement, columnNames, tableInfoRecord, filteredResults);
                changeRecordStatements.AddRange(additionalUpdateRecordStatements);
            }

            //marciak - these are needed to remove wrong results on the first update, EXAMPLE one AND with a non unique column
            var wrongResults = decryptedResults.Except(filteredResults).ToList();
            if (changeRecordSqlCommand.TransformedSqlStatement.Count == 2)
            {
                var transformedChangeRecordStatement = (IChangeRecordStatement)changeRecordSqlCommand.TransformedSqlStatement[1];
                changeRecordStatements.Add(transformedChangeRecordStatement);
                if (wrongResults.Count() != 0)
                {
                    foreach (var tableColumn in columnNames)
                    {
                        var columnInfoRecord = _encryptor.FindInfoRecord(tableColumn.ColumnName, tableInfoRecord.IV);
                        if (columnInfoRecord != null && columnInfoRecord.LData.EncryptedIVColumnName != null)
                        {
                            foreach (var row in wrongResults)
                            {
                                var decryptedTableName = tableInfoRecord.KeyName != null ? _encryptor.DecryptName(tableInfoRecord) : tableInfoRecord.Name;
                                var ivIndexColumn = columnNames.Select(c => c.ToString()).ToList().IndexOf(decryptedTableName + "." + columnInfoRecord.LData.EncryptedIVColumnName);

                                if (transformedChangeRecordStatement.WhereExpression != null)
                                {
                                    transformedChangeRecordStatement.WhereExpression.HasParenthesis = true;
                                    //marciak - this wrong rows are removed by adding AND NOT using IV's   
                                    transformedChangeRecordStatement.WhereExpression = new LogicalExpression(
                                        transformedChangeRecordStatement.WhereExpression,
                                        new ComparisonExpression(new TableAndColumnName(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.LData.EncryptedIVColumnName)),
                                        new Value(row[ivIndexColumn], true),
                                        ComparisonExpression.ComparisonOperatorEnum.Different),
                                        LogicalExpression.LogicalOperatorEnum.AND);
                                }
                            }
                            break;
                        }
                    }
                }
            }
            return changeRecordStatements;
        }

        private IList<UpdateRecordStatement> GetAdditionalUpdateWithCaseRecordStatements(UpdateRecordStatement originalUpdateRecordStatement, IList <TableAndColumnName> columnNames, InfoRecord tableInfoRecord, IList<IList<string>> filteredResults){
            var updateRecordStatements = new List<UpdateRecordStatement>();
            foreach (var columnValue in originalUpdateRecordStatement.ColumnNamesAndUpdateValues)
            {
                var columnInfoRecord = _encryptor.FindInfoRecord(columnValue.Key, tableInfoRecord.IV);
                if (columnInfoRecord.LData.EncryptedIVColumnName == null) continue;

                var decryptedTableName = tableInfoRecord.KeyName != null ? _encryptor.DecryptName(tableInfoRecord) : tableInfoRecord.Name;
                var ivIndexColumn = columnNames.Select(c => c.ToString()).ToList().IndexOf(decryptedTableName + "." + columnInfoRecord.LData.EncryptedIVColumnName);

                foreach (var row in filteredResults)
                {
                    var additionalUpdateRecordStatement = new UpdateRecordStatement();
                    additionalUpdateRecordStatement.TableName = new estring(tableInfoRecord.Name);

                    var encryptedValue = new Value(_encryptor.EncryptNormalValue(((LiteralValueExpression)columnValue.Value).LiteralValue.ValueToInsert, columnInfoRecord, out string generatedIV), true);
                    additionalUpdateRecordStatement.ColumnNamesAndUpdateValues.Add(new estring(columnInfoRecord.Name), new LiteralValueExpression(encryptedValue));
                    additionalUpdateRecordStatement.ColumnNamesAndUpdateValues.Add(new estring(columnInfoRecord.LData.EncryptedIVColumnName), new LiteralValueExpression(new Value(generatedIV, true)));

                    var oldIV = row[ivIndexColumn];
                    additionalUpdateRecordStatement.WhereExpression = new ComparisonExpression(
                        new TableAndColumnName(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.LData.EncryptedIVColumnName)),
                        new Value(oldIV, true),
                        ComparisonExpression.ComparisonOperatorEnum.Equal);

                    updateRecordStatements.Add(additionalUpdateRecordStatement);
                }
            }
            return updateRecordStatements;
            
        }

        private IList<UpdateRecordStatement> GetAdditionalUpdateRecordStatements(UpdateRecordStatement originalUpdateRecordStatement, IList<TableAndColumnName> columnNames, InfoRecord tableInfoRecord, IList<IList<string>> filteredResults)
        {
            //if(originalUpdateRecordStatement.CaseExpressions.Count!=0){
            //   return GetAdditionalUpdateWithCaseRecordStatements(originalUpdateRecordStatement, columnNames, tableInfoRecord, filteredResults);
            //}
            var updateRecordStatements = new List<UpdateRecordStatement>();
            foreach (var columnValue in originalUpdateRecordStatement.ColumnNamesAndUpdateValues)
            {
                if(columnValue.Value is CaseExpression caseExpression)
                {
                    var columnInfoRecord = _encryptor.FindInfoRecord(columnValue.Key, tableInfoRecord.IV);
                    if (columnInfoRecord.LData.EncryptedIVColumnName == null) continue;

                    var decryptedTableName = tableInfoRecord.KeyName != null ? _encryptor.DecryptName(tableInfoRecord) : tableInfoRecord.Name;
                    var ivIndexColumn = columnNames.Select(c => c.ToString()).ToList().IndexOf(decryptedTableName + "." + columnInfoRecord.LData.EncryptedIVColumnName);
                    //foreach (var row in filteredResults)
                    //{
                        foreach(var whenThenExpression in caseExpression.WhenThenExpressions){
                            var additionalUpdateRecordStatement = new UpdateRecordStatement();
                            additionalUpdateRecordStatement.TableName = new estring(tableInfoRecord.Name);

                            var encryptedValue = new Value(_encryptor.EncryptNormalValue(((LiteralValueExpression)whenThenExpression.ThenExpression).LiteralValue.ValueToInsert, columnInfoRecord, out string generatedIV), true);
                            additionalUpdateRecordStatement.ColumnNamesAndUpdateValues.Add(new estring(columnInfoRecord.Name), new LiteralValueExpression(encryptedValue));
                            additionalUpdateRecordStatement.ColumnNamesAndUpdateValues.Add(new estring(columnInfoRecord.LData.EncryptedIVColumnName), new LiteralValueExpression(new Value(generatedIV, true)));

                            additionalUpdateRecordStatement.WhereExpression = whenThenExpression.WhenExpression;

                            updateRecordStatements.Add(additionalUpdateRecordStatement);
                        }
                        if(caseExpression.ElseExpression != null){
                            var additionalUpdateRecordStatement = new UpdateRecordStatement();
                            additionalUpdateRecordStatement.TableName = new estring(tableInfoRecord.Name);

                            var encryptedValue = new Value(_encryptor.EncryptNormalValue(((LiteralValueExpression)caseExpression.ElseExpression).LiteralValue.ValueToInsert, columnInfoRecord, out string generatedIV), true);
                            additionalUpdateRecordStatement.ColumnNamesAndUpdateValues.Add(new estring(columnInfoRecord.Name), new LiteralValueExpression(encryptedValue));
                            additionalUpdateRecordStatement.ColumnNamesAndUpdateValues.Add(new estring(columnInfoRecord.LData.EncryptedIVColumnName), new LiteralValueExpression(new Value(generatedIV, true)));

                            updateRecordStatements.Add(additionalUpdateRecordStatement);
                        }
                    //}
                } 
                else {
                    var columnInfoRecord = _encryptor.FindInfoRecord(columnValue.Key, tableInfoRecord.IV);
                    if (columnInfoRecord.LData.EncryptedIVColumnName == null) continue;

                    var decryptedTableName = tableInfoRecord.KeyName != null ? _encryptor.DecryptName(tableInfoRecord) : tableInfoRecord.Name;
                    var ivIndexColumn = columnNames.Select(c => c.ToString()).ToList().IndexOf(decryptedTableName + "." + columnInfoRecord.LData.EncryptedIVColumnName);

                    foreach (var row in filteredResults)
                    {
                        var additionalUpdateRecordStatement = new UpdateRecordStatement();
                        additionalUpdateRecordStatement.TableName = new estring(tableInfoRecord.Name);

                        var encryptedValue = new Value(_encryptor.EncryptNormalValue(((LiteralValueExpression)columnValue.Value).LiteralValue.ValueToInsert, columnInfoRecord, out string generatedIV), true);
                        additionalUpdateRecordStatement.ColumnNamesAndUpdateValues.Add(new estring(columnInfoRecord.Name), new LiteralValueExpression(encryptedValue));
                        additionalUpdateRecordStatement.ColumnNamesAndUpdateValues.Add(new estring(columnInfoRecord.LData.EncryptedIVColumnName), new LiteralValueExpression(new Value(generatedIV, true)));

                        var oldIV = row[ivIndexColumn];
                        additionalUpdateRecordStatement.WhereExpression = new ComparisonExpression(
                            new TableAndColumnName(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.LData.EncryptedIVColumnName)),
                            new Value(oldIV, true),
                            ComparisonExpression.ComparisonOperatorEnum.Equal);

                        updateRecordStatements.Add(additionalUpdateRecordStatement);
                    }
                }
                
            }

            return updateRecordStatements;

        }

        // marciak - decrypts all row values and saves it to a list of lists
        public IList<IList<string>> DecryptRows(SimpleSelectStatement simpleSelectStatement, IList<IList<string>> allResults, string databaseName, out IList<TableAndColumnName> columnNames)
        {
            var databaseInfoRecord = _encryptor.FindInfoRecord(new estring(databaseName), null);

            var selectCoreStatement = simpleSelectStatement.SelectCoreStatement;

            var decryptedResults = new List<IList<string>>();
            foreach (var row in allResults) decryptedResults.Add(new List<string>());

            columnNames = new List<TableAndColumnName>();

            for (int i = 0; i < selectCoreStatement.ResultColumns.Count; i++)
            {
                var resultColumn = selectCoreStatement.ResultColumns[i];

                var tableInfoRecord = _encryptor.FindInfoRecord(resultColumn.TableName, databaseInfoRecord.IV);

                var decryptedTableName = tableInfoRecord.KeyName != null ? new estring(_encryptor.DecryptName(tableInfoRecord), true) : new estring(tableInfoRecord.Name, false);

                var columnInfoRecord = _encryptor.FindInfoRecord(resultColumn.ColumnName, tableInfoRecord.IV);

                if (columnInfoRecord != null)
                {
                    var decryptedColumnName = columnInfoRecord.KeyName != null ? _encryptor.DecryptName(columnInfoRecord) : columnInfoRecord.Name;

                    columnNames.Add(new TableAndColumnName(decryptedTableName, new estring(decryptedColumnName, true)));
                }

                else columnNames.Add(new TableAndColumnName(decryptedTableName, resultColumn.ColumnName));

                for (int j = 0; j < allResults.Count; j++)
                {
                    var row = allResults[j];

                    if (columnInfoRecord != null)
                    {
                        if(row[i] == null){
                            decryptedResults[j].Add(null);
                            continue;
                        }
                        var dataType = columnInfoRecord.LData.DataType;

                        if (dataType.DataTypeName == DataTypeEnum.ENCRYPTED)
                        {
                            var decryptedValue = "";
                            if (columnInfoRecord.LData.EncryptedIVColumnName != null)
                            {
                                var ivColumn = selectCoreStatement.ResultColumns.Where(r => r.ColumnName.Value == columnInfoRecord.LData.EncryptedIVColumnName).SingleOrDefault();
                                var columnIVIndex = selectCoreStatement.ResultColumns.IndexOf(ivColumn);
                                if(selectCoreStatement.CaseExpressions.Count != 0) 
                                {
                                    decryptedValue = _encryptor.DecryptUniqueValue(row[i], columnInfoRecord);
                                }
                                else {
                                    decryptedValue = _encryptor.DecryptNormalValue(row[i], columnInfoRecord, row[columnIVIndex]);
                                }
                                
                            }
                            else decryptedValue = _encryptor.DecryptUniqueValue(row[i], columnInfoRecord);
                            decryptedResults[j].Add(decryptedValue);
                            continue;
                        }
                    }
                    decryptedResults[j].Add(row[i]);
                }
            }

            return decryptedResults;
        }

        public IList<string> GetDatabasesList()
        {
            var databases = _encryptor.FindChildren("0");
            return databases.Select(d => d.KeyName != null ? _encryptor.DecryptName(d) : d.Name).ToList();
        }

        public IList<string> GetEncryptedDatabasesList()
        {
            var databases = _encryptor.FindChildren("0");
            return databases.Select(d => d.Name).ToList();
        }


        public string DecryptDatabaseName(string encryptedDatabaseName)
        {
            var databaseInfoRecord = _encryptor.FindInfoRecord(new estring(encryptedDatabaseName), null);
            return databaseInfoRecord.KeyName != null ? _encryptor.DecryptName(databaseInfoRecord) : databaseInfoRecord.Name;
        }

        // marciak - this method is used with the original where expression to eliminate any redundancy create by the buckets
        private IList<IList<string>> FilterExpression(AbstractExpression expression, IList<IList<string>> decryptedResults, IList<TableAndColumnName> columnNames)
        {
            switch (expression)
            {
                case ComparisonExpression comparisonExpression:
                    if (comparisonExpression.Value == null || comparisonExpression.Value.ValueToInsert == null) return decryptedResults;
                    return FilterComparisonExpression(comparisonExpression, decryptedResults, columnNames);

                case LogicalExpression logicalExpression:
                    IList<IList<string>> resultsLeftExpression;
                    switch (logicalExpression.LogicalOperator)
                    {
                        case LogicalExpression.LogicalOperatorEnum.AND:
                            resultsLeftExpression = FilterExpression(logicalExpression.LeftExpression, decryptedResults, columnNames);
                            return FilterExpression(logicalExpression.RightExpression, resultsLeftExpression, columnNames);

                        case LogicalExpression.LogicalOperatorEnum.OR:
                            resultsLeftExpression = FilterExpression(logicalExpression.LeftExpression, decryptedResults, columnNames);
                            return resultsLeftExpression.Union(FilterExpression(logicalExpression.RightExpression, decryptedResults, columnNames)).ToList();
                    }
                    break;

            }

            return decryptedResults;

        }


        private IList<IList<string>> FilterSelectColumns(IList<ResultColumn> resultColumns, IList<IList<string>> decryptedResults, IList<TableAndColumnName> columnNames, string databaseName, out IList<string> columnsToMantain)
        {
            var databaseInfoRecord = _encryptor.FindInfoRecord(new estring(databaseName), null);

            columnsToMantain = new List<string>();

            foreach (var resultColumn in resultColumns)
            {
                if (!resultColumn.AllColumnsfFlag) columnsToMantain.Add(resultColumn.TableName.Value + "." + resultColumn.ColumnName.Value);
                else
                {
                    var tableInfoRecord = _encryptor.FindInfoRecord(resultColumn.TableName, databaseInfoRecord.IV);

                    foreach (var columnInfoRecord in _encryptor.FindChildren(tableInfoRecord.IV))
                    {
                        var decryptedColumnName = columnInfoRecord.KeyName != null ? _encryptor.DecryptName(columnInfoRecord) : columnInfoRecord.Name;
                        columnsToMantain.Add(resultColumn.TableName.Value + "." + decryptedColumnName);
                    }
                }
            }

            var columnsToRemove = columnNames.Select(s => s.ToString()).Except(columnsToMantain).ToList();
            var indexesOfColumnsToRemove = columnsToRemove.Select(c => columnNames.Select(s => s.ToString()).ToList().IndexOf(c)).OrderByDescending(x => x).ToList();

            foreach (var row in decryptedResults)
                foreach (var index in indexesOfColumnsToRemove)
                    row.RemoveAt(index);


            return decryptedResults;
        }
        private IList<IList<string>> FilterComparisonExpression(ComparisonExpression expression, IList<IList<string>> decryptedResults, IList<TableAndColumnName> columnNames)
        {
            var columnIndex = columnNames.Select(s => s.ToString()).ToList().IndexOf(expression.LeftTableNameAndColumnName.TableName.Value + "." + expression.LeftTableNameAndColumnName.ColumnName.Value);

            switch (expression.ComparisonOperator)
            {
                case ComparisonExpression.ComparisonOperatorEnum.BiggerOrEqualThan:
                    return decryptedResults.Where(r => double.Parse(r[columnIndex]) >= double.Parse(expression.Value.ValueToInsert)).ToList();

                case ComparisonExpression.ComparisonOperatorEnum.BiggerThan:
                    return decryptedResults.Where(r => double.Parse(r[columnIndex]) > double.Parse(expression.Value.ValueToInsert)).ToList();

                case ComparisonExpression.ComparisonOperatorEnum.Different:
                    return decryptedResults.Where(r => r[columnIndex] != expression.Value.ValueToInsert).ToList();

                case ComparisonExpression.ComparisonOperatorEnum.Equal:
                    return decryptedResults.Where(r => r[columnIndex] == expression.Value.ValueToInsert).ToList();

                case ComparisonExpression.ComparisonOperatorEnum.SmallerOrEqualThan:
                    return decryptedResults.Where(r => double.Parse(r[columnIndex]) <= double.Parse(expression.Value.ValueToInsert)).ToList();

                case ComparisonExpression.ComparisonOperatorEnum.SmallerThan:
                    return decryptedResults.Where(r => double.Parse(r[columnIndex]) < double.Parse(expression.Value.ValueToInsert)).ToList();
            }

            throw new FormatException("Comparison operator not recognized.");
        }

        public IList<DatabasePoco> GetStructure()
        {
            var structure = new List<DatabasePoco>();
            var databasesInfoRecords = _encryptor.FindChildren("0");

            foreach (var databaseInfoRecord in databasesInfoRecords)
            {

                var databaseName = databaseInfoRecord.KeyName != null ? _encryptor.DecryptName(databaseInfoRecord) : "!" + databaseInfoRecord.Name;
                var database = new DatabasePoco(databaseName);

                var tablesInfoRecords = _encryptor.FindChildren(databaseInfoRecord.IV);
                foreach (var tableInfoRecord in tablesInfoRecords)
                {
                    var tableName = tableInfoRecord.KeyName != null ? _encryptor.DecryptName(tableInfoRecord) : "!" + tableInfoRecord.Name;
                    var table = new TablePoco(tableName);

                    var columnsInfoRecords = _encryptor.FindChildren(tableInfoRecord.IV);
                    foreach (var columnInfoRecord in columnsInfoRecords)
                    {
                        var columnName = columnInfoRecord.KeyName != null ? _encryptor.DecryptName(columnInfoRecord) : "!" + columnInfoRecord.Name;
                        var column = new FieldPoco(columnName, columnInfoRecord.LData.DataType.DataTypeName.ToString(), columnInfoRecord.LData.ColumnConstraints?.Select(c => c.ToString()).ToList());
                        table.Fields.Add(column);
                    }
                    database.Tables.Add(table);
                }
                structure.Add(database);
            }
            return structure;
        }
    }
}