﻿using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using BlockBase.Domain.Database.Sql.SqlCommand;
using BlockBase.Domain.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlockBase.DataProxy.Encryption
{
    public class InfoPostProcessing
    {
        private IEncryptor _encryptor;
        public InfoPostProcessing(MiddleMan middleMan)
        {
            _encryptor = middleMan;
        }


        public QueryResult TranslateSelectResults(ReadQuerySqlCommand readQuerySqlCommand, IList<IList<string>> allResults, string databaseName)
        {
            var decryptedResults = DecryptRows((SimpleSelectStatement)readQuerySqlCommand.TransformedSqlStatement[0], allResults, databaseName, out IList<TableAndColumnName> columnNames);
            var filteredResults = FilterExpression(((SimpleSelectStatement)readQuerySqlCommand.OriginalSqlStatement).SelectCoreStatement.WhereExpression, decryptedResults, columnNames);
            var removedExtraColumns = FilterSelectColumns(((SimpleSelectStatement)readQuerySqlCommand.OriginalSqlStatement).SelectCoreStatement.ResultColumns, filteredResults, columnNames, databaseName, out IList<string> columnsToMantain);

            return new QueryResult(removedExtraColumns, columnsToMantain);
        }

        public IList<UpdateRecordStatement> UpdateUpdateRecordStatement(UpdateSqlCommand updateSqlCommand, IList<IList<string>> allResults, string databaseName)
        {
            var originalUpdateRecordStatement = (UpdateRecordStatement)updateSqlCommand.OriginalSqlStatement;
            var transformedSimpleSelectStatement = (SimpleSelectStatement)updateSqlCommand.TransformedSqlStatement[0];

            var updateRecordStatements = new List<UpdateRecordStatement>();

            var decryptedResults = DecryptRows(transformedSimpleSelectStatement, allResults, databaseName, out IList<TableAndColumnName> columnNames);
            var filteredResults = FilterExpression(originalUpdateRecordStatement.WhereExpression, decryptedResults, columnNames);

            var databaseInfoRecord = _encryptor.FindInfoRecord(new estring(databaseName), null);
            var tableInfoRecord = _encryptor.FindInfoRecord(originalUpdateRecordStatement.TableName, databaseInfoRecord.IV);

            var additionalUpdateRecordStatements = GetAdditionalUpdateRecordStatements(originalUpdateRecordStatement, columnNames, tableInfoRecord, filteredResults);
            updateRecordStatements.AddRange(additionalUpdateRecordStatements);

            var wrongResults = decryptedResults.Except(filteredResults).ToList(); //these are needed to remove extra results on the first

            if (updateSqlCommand.TransformedSqlStatement.Count == 2)
            {
                var transformedUpdateRecordStatement = (UpdateRecordStatement)updateSqlCommand.TransformedSqlStatement[1];
                updateRecordStatements.Add(transformedUpdateRecordStatement);
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

                                if (transformedUpdateRecordStatement.WhereExpression != null)
                                {
                                    transformedUpdateRecordStatement.WhereExpression.HasParenthesis = true;
                                    transformedUpdateRecordStatement.WhereExpression = new LogicalExpression(
                                        transformedUpdateRecordStatement.WhereExpression,
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

            return updateRecordStatements;


        }

        private IList<UpdateRecordStatement> GetAdditionalUpdateRecordStatements(UpdateRecordStatement originalUpdateRecordStatement, IList<TableAndColumnName> columnNames, InfoRecord tableInfoRecord, IList<IList<string>> filteredResults)
        {
            var updateRecordStatements = new List<UpdateRecordStatement>();

            foreach (var columnValue in originalUpdateRecordStatement.ColumnNamesAndUpdateValues)
            {
                var columnInfoRecord = _encryptor.FindInfoRecord(columnValue.Key, tableInfoRecord.IV);
                if (columnInfoRecord.LData.EncryptedIVColumnName == null) continue;

                var decryptedTableName = tableInfoRecord.KeyName != null ? _encryptor.DecryptName(tableInfoRecord) : tableInfoRecord.Name;
                var ivIndexColumn = columnNames.Select(c => c.ToString()).ToList().IndexOf(decryptedTableName  + "." + columnInfoRecord.LData.EncryptedIVColumnName);

                foreach (var row in filteredResults)
                {
                    var additionalUpdateRecordStatement = new UpdateRecordStatement();
                    additionalUpdateRecordStatement.TableName = new estring(tableInfoRecord.Name);

                    var encryptedValue = new Value(_encryptor.EncryptNormalValue(columnValue.Value.ValueToInsert, columnInfoRecord, out string generatedIV), true);
                    additionalUpdateRecordStatement.ColumnNamesAndUpdateValues.Add(new estring(columnInfoRecord.Name), encryptedValue);
                    additionalUpdateRecordStatement.ColumnNamesAndUpdateValues.Add(new estring(columnInfoRecord.LData.EncryptedIVColumnName), new Value(generatedIV, true));

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
                        var dataType = _encryptor.GetColumnDataType(columnInfoRecord);

                        if (dataType.DataTypeName == DataTypeEnum.ENCRYPTED)
                        {
                            var decryptedValue = "";
                            if (columnInfoRecord.LData.EncryptedIVColumnName != null)
                            {
                                var ivColumn = selectCoreStatement.ResultColumns.Where(r => r.ColumnName.Value == columnInfoRecord.LData.EncryptedIVColumnName).SingleOrDefault();
                                var columnIVIndex = selectCoreStatement.ResultColumns.IndexOf(ivColumn);
                                decryptedValue = _encryptor.DecryptNormalValue(row[i], columnInfoRecord, row[columnIVIndex]);
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


        public string DecryptDatabaseName(string encryptedDatabaseName)
        {
            var databaseInfoRecord = _encryptor.FindInfoRecord(new estring(encryptedDatabaseName), null);
            return databaseInfoRecord.KeyName != null ? _encryptor.DecryptName(databaseInfoRecord) : databaseInfoRecord.Name;
        }

        private IList<IList<string>> FilterExpression(AbstractExpression expression, IList<IList<string>> decryptedResults, IList<TableAndColumnName> columnNames)
        {
            switch (expression)
            {
                case ComparisonExpression comparisonExpression:
                    if (comparisonExpression.Value == null) return decryptedResults;
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


    }
}