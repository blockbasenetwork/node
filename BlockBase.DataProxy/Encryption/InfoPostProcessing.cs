using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
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

        public IList<IList<string>> RemoveDecoyRows(ReadQuerySqlCommand readQuerySqlCommand, IList<IList<string>> allResults, string databaseName)
        {
            var decryptedResults = DecryptRows((SimpleSelectStatement) readQuerySqlCommand.TransformedSqlStatement, allResults, databaseName);
            
            //TODO: Continue...

            throw new  NotImplementedException();
        }

        public IList<IList<string>> DecryptRows(SimpleSelectStatement simpleSelectStatement, IList<IList<string>> allResults, string databaseName)
        {
            var databaseInfoRecord = _encryptor.FindInfoRecord(new estring(databaseName), null);

            var selectCoreStatement = simpleSelectStatement.SelectCoreStatement;

            var decryptedResults = new List<IList<string>>();
            decryptedResults.Add(new List<string>());
            foreach (var row in allResults) decryptedResults.Add(new List<string>());

            for (int i = 0; i < selectCoreStatement.ResultColumns.Count; i++)
            {
                var resultColumn = selectCoreStatement.ResultColumns[i];
                var tableInfoRecord = _encryptor.FindInfoRecord(resultColumn.TableName, databaseInfoRecord.IV);
                var columnInfoRecord = _encryptor.FindInfoRecord(resultColumn.ColumnName, tableInfoRecord.IV);

                if (columnInfoRecord == null) continue;

                if (columnInfoRecord.KeyName != null) decryptedResults[0].Add(_encryptor.DecryptName(columnInfoRecord));
                else decryptedResults[0].Add(columnInfoRecord.Name);

                

                for(int j = 0; j < allResults.Count; j++)
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
                        decryptedResults[j+1].Add(decryptedValue);
                    }

                    else decryptedResults[j+1].Add(row[i]);
                }
            }

            return decryptedResults;
        }
        
    }
}
