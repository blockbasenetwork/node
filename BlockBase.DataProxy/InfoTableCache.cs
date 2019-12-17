using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.DataProxy
{
    public class InfoTableCache
    {
        private Dictionary<estring, InfoRecord> _infoRecordsPerDatabase;
        private IConnector _connector;
        private Encryptor _encryptor;

        public InfoTableCache(IConnector connector, Encryptor encryptor)
        {
            _connector = connector;
            _infoRecordsPerDatabase = new Dictionary<estring, InfoRecord>();
            _encryptor = encryptor;
        }

        public async Task Build()
        {
            var databases = await _connector.GetDatabasesList();

            foreach (var database in databases)
            {
                var encryptedInfoRecords = await _connector.GetInfoRecords(database);

                if (encryptedInfoRecords.Count != 0)
                {
                    var infoRecord = DecryptInfoRecords(encryptedInfoRecords);
                    var databaseName = new estring(infoRecord.Name, infoRecord.KeyRead != null);
                    _infoRecordsPerDatabase.Add(databaseName, infoRecord);
                }
            }
        }

        private InfoRecord DecryptInfoRecords(IList<InfoRecord> encryptedInfoRecords)
        {
            var databaseInfoRecord = encryptedInfoRecords.Where(ir => ir.ParentIV == null).SingleOrDefault();
            if (databaseInfoRecord == null) throw new Exception("Database Info not inserted.");
            return DecryptInfoRecord(databaseInfoRecord);
        }

        private InfoRecord DecryptInfoRecord(InfoRecord infoRecord, byte[] parentIV = null, byte[] parentManageKey = null)
        {
            var decryptedInfoRecord = new InfoRecord();
            var isNameEncrypted = infoRecord.KeyRead != null;

            //decryptedInfoRecord.KeyManage = decryptKeyManage(encryptedKey, parentManageKey, parentIV);
            //decryptedInfoRecord.KeyRead = decryptKeyManage(encryptedKey, decryptedInfoRecord.KeyManage, infoRecord.IV);
            //decryptedInfoRecord.Name = isNameEncrypted ? decryptName(encryptedName, readKey) : decryptedInfoRecord.Name;


            //decrypt keys

            return decryptedInfoRecord;

        }
    }
}
