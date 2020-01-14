using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Table;
using BlockBase.Domain.Database.Sql.SqlCommand;
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


        public IList<IList<string>> TranslateSelectResults(ReadQuerySqlCommand readQuerySqlCommand, IList<IList<string>> allResults, string databaseName)
        {
            var decryptedResults = DecryptRows((SimpleSelectStatement)readQuerySqlCommand.TransformedSqlStatement[0], allResults, databaseName, out IList<string> columnNames);

            var filteredResults = FilterExpression(((SimpleSelectStatement)readQuerySqlCommand.OriginalSqlStatement).SelectCoreStatement.WhereExpression, decryptedResults, columnNames);

            var removedExtraColumns = FilterSelectColumns(((SimpleSelectStatement)readQuerySqlCommand.OriginalSqlStatement).SelectCoreStatement.ResultColumns, filteredResults, columnNames, databaseName);

            return removedExtraColumns;
        }

        public IList<UpdateRecordStatement> CreateUpdateRecordStatement(UpdateSqlCommand updateSqlCommand, IList<IList<string>> allResults, string databaseName)
        {
            var transformedUpdateRecordStatements = new List<UpdateRecordStatement>();

            var originalUpdateRecordStatement = (UpdateRecordStatement)updateSqlCommand.OriginalSqlStatement;

            var databaseInfoRecord = _encryptor.FindInfoRecord(new estring(databaseName), null);

            var tableInfoRecord = _encryptor.FindInfoRecord(originalUpdateRecordStatement.TableName, databaseInfoRecord.IV);

            var transformedUpdateRecordStatement = new UpdateRecordStatement();

            transformedUpdateRecordStatement.TableName = new estring(tableInfoRecord.Name);

            foreach (var columnValues in originalUpdateRecordStatement.ColumnNamesAndUpdateValues)
            {
                var columnInfoRecord = _encryptor.FindInfoRecord(columnValues.Key, tableInfoRecord.IV);

                var dataType = _encryptor.GetColumnDataType(columnInfoRecord);

                if (dataType.DataTypeName != DataTypeEnum.ENCRYPTED)
                    transformedUpdateRecordStatement.ColumnNamesAndUpdateValues[new estring(columnInfoRecord.Name)] = columnValues.Value;
                else
                {
                    if (columnInfoRecord.LData.EncryptedIVColumnName == null)
                        transformedUpdateRecordStatement.ColumnNamesAndUpdateValues[new estring(columnInfoRecord.Name)] = new Value(_encryptor.EncryptUniqueValue(columnValues.Value.ValueToInsert, columnInfoRecord), true);

                    //else
                    //{
                    //    transformedUpdateRecordStatement.ColumnNamesAndUpdateValues[new estring(columnInfoRecord.Name)] = new Value(_encryptor.EncryptUniqueValue(columnValues.Value.ValueToInsert, columnInfoRecord), true);

                    //    transformedUpdateRecordStatements.Add(
                    //        new UpdateRecordStatement(
                    //            new estring(tableInfoRecord.Name), 
                    //            new Dictionary<estring, Value>() { { new estring(columnInfoRecord.LData.EncryptedIVColumnName), generatedIV } }, 
                    //            new ComparisonExpression(new estring(tableInfoRecord.Name), new estring(columnInfoRecord.LData.EncryptedIVColumnName), );
                    //    transformedUpdateRecordStatement.ColumnNamesAndUpdateValues[new estring(columnInfoRecord.Name)] = new Value(_encryptor.EncryptUniqueValue(columnValues.Value.ValueToInsert, columnInfoRecord), true);
                    //}
                }
            }
            return transformedUpdateRecordStatements;
        }

        public IList<IList<string>> DecryptRows(SimpleSelectStatement simpleSelectStatement, IList<IList<string>> allResults, string databaseName, out IList<string> columnNames)
        {
            var databaseInfoRecord = _encryptor.FindInfoRecord(new estring(databaseName), null);

            var selectCoreStatement = simpleSelectStatement.SelectCoreStatement;

            var decryptedResults = new List<IList<string>>();
            foreach (var row in allResults) decryptedResults.Add(new List<string>());

            columnNames = new List<string>();

            for (int i = 0; i < selectCoreStatement.ResultColumns.Count; i++)
            {
                var resultColumn = selectCoreStatement.ResultColumns[i];
                var tableInfoRecord = _encryptor.FindInfoRecord(resultColumn.TableName, databaseInfoRecord.IV);

                var decryptedTableName = tableInfoRecord.KeyName != null ? _encryptor.DecryptName(tableInfoRecord) : tableInfoRecord.Name;

                var columnInfoRecord = _encryptor.FindInfoRecord(resultColumn.ColumnName, tableInfoRecord.IV);

                if (columnInfoRecord == null) continue;

                var decryptedColumnName = columnInfoRecord.KeyName != null ? _encryptor.DecryptName(columnInfoRecord) : columnInfoRecord.Name;

                columnNames.Add(decryptedTableName + "." + decryptedColumnName);

                for (int j = 0; j < allResults.Count; j++)
                {
                    var row = allResults[j];
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
                    }

                    else decryptedResults[j].Add(row[i]);
                }
            }

            return decryptedResults;
        }


        private IList<IList<string>> FilterExpression(AbstractExpression expression, IList<IList<string>> decryptedResults, IList<string> columnNames)
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
        private IList<IList<string>> FilterSelectColumns(IList<ResultColumn> resultColumns, IList<IList<string>> decryptedResults, IList<string> columnNames, string databaseName)
        {
            var databaseInfoRecord = _encryptor.FindInfoRecord(new estring(databaseName), null);

            var columnsToMantain = new List<string>();

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

            var columnsToRemove = columnNames.Except(columnsToMantain);
            var indexesOfColumnsToRemove = columnsToRemove.Select(c => columnNames.IndexOf(c));
            foreach (var row in decryptedResults)
            {
                foreach (var index in indexesOfColumnsToRemove)
                {
                    row.Remove(row[index]);
                }
            }
            return decryptedResults;
        }
        private IList<IList<string>> FilterComparisonExpression(ComparisonExpression expression, IList<IList<string>> decryptedResults, IList<string> columnNames)
        {
            var columnIndex = columnNames.IndexOf(expression.LeftTableNameAndColumnName.TableName.Value + "." + expression.LeftTableNameAndColumnName.ColumnName.Value);

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