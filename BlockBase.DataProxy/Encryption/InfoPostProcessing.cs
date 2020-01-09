using BlockBase.Domain.Database.Sql.QueryBuilder.Elements;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common.Expressions;
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
            IList<string> columnNames;

            var decryptedResults = DecryptRows((SimpleSelectStatement)readQuerySqlCommand.TransformedSqlStatement[0], allResults, databaseName, out columnNames);

            return RemoveDecoyRows((SimpleSelectStatement)readQuerySqlCommand.OriginalSqlStatement, decryptedResults, columnNames);
        }

        private IList<IList<string>> DecryptRows(SimpleSelectStatement simpleSelectStatement, IList<IList<string>> allResults, string databaseName, out IList<string> columnNames)
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
                var columnInfoRecord = _encryptor.FindInfoRecord(resultColumn.ColumnName, tableInfoRecord.IV);

                if (columnInfoRecord == null) continue;

                if (columnInfoRecord.KeyName != null) columnNames.Add(_encryptor.DecryptName(columnInfoRecord));
                else columnNames.Add(columnInfoRecord.Name);

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

        private IList<IList<string>> RemoveDecoyRows(SimpleSelectStatement simpleSelectStatement, IList<IList<string>> decryptedResults, IList<string> columnNames)
        {
            var filteredResults = FilterExpression(simpleSelectStatement.SelectCoreStatement.WhereExpression, decryptedResults, columnNames);

            return FilterSelectColumns(simpleSelectStatement.SelectCoreStatement.ResultColumns, filteredResults, columnNames);
        }

        private IList<IList<string>> FilterExpression(AbstractExpression expression, IList<IList<string>> decryptedResults, IList<string> columnNames)
        { 
            switch (expression)
            {
                case ComparisonExpression comparisonExpression:
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

            throw new FormatException("Expression not recognized.");

        }
        private IList<IList<string>> FilterSelectColumns(IList<ResultColumn> resultColumns, IList<IList<string>> decryptedResults, IList<string> columnNames)
        {
            var columnsToRemove = columnNames.Except(resultColumns.Select(r => r.ColumnName.Value).ToList());
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
            var columnIndex = columnNames.IndexOf(expression.ColumnName.Value);

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