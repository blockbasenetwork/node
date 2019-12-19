using BlockBase.Domain.Database.Info;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wiry.Base32;

namespace BlockBase.DataProxy.Encryption
{
    public class InfoRecordManager
    {
        private Dictionary<string, List<InfoRecord>> _infoRecordsLookup = new Dictionary<string, List<InfoRecord>>();

        private const string ROOT_DUMMY_IV = "0";

        public static InfoRecord CreateInfoRecord(string recordName, string encryptedKeyManage, string encryptedKeyName, string recordIV, string parentIV, string localNameHash, InfoRecord.LocalData localData = null, string data = null)
        {
            return new InfoRecord
            {
                Name = recordName,
                IV = recordIV,
                KeyManage = encryptedKeyManage,
                KeyName = encryptedKeyName,
                LocalNameHash = localNameHash,
                ParentIV = parentIV,
                Data = data,
                LData = localData
            };
        }

        public List<InfoRecord> FindChildren(string iv, bool deepFind = false)
        {
            if (!_infoRecordsLookup.ContainsKey(iv))
                return new List<InfoRecord>();

            if (!deepFind)
            {
                return _infoRecordsLookup[iv];
            }
            else
            {
                var resultList = new List<InfoRecord>();
                var queueToSearch = new Queue<InfoRecord>();

                foreach (var record in _infoRecordsLookup[iv])
                {
                    resultList.Add(record);
                    queueToSearch.Enqueue(record);
                }

                while (queueToSearch.Count != 0)
                {
                    if (_infoRecordsLookup.ContainsKey(queueToSearch.Peek().IV))
                    {
                        foreach (var record in _infoRecordsLookup[queueToSearch.Peek().IV])
                        {
                            resultList.Add(record);
                            queueToSearch.Enqueue(record);
                        }
                    }
                    queueToSearch.Dequeue();
                }

                return resultList;
            }
        }

        public InfoRecord FindInfoRecord(estring recordName, string parentIV)
        {
            string pIV = parentIV != null ? parentIV : ROOT_DUMMY_IV;
            if (!_infoRecordsLookup.ContainsKey(pIV)) return null;
            string localNameHash = !recordName.ToEncrypt ? null : Base32Encoding.ZBase32.GetString(Utils.Crypto.Utils.SHA256(Encoding.Unicode.GetBytes(recordName.Value)));
            return _infoRecordsLookup[pIV].SingleOrDefault(r => !recordName.ToEncrypt ? r.Name == recordName.Value : r.LocalNameHash == localNameHash);
        }

        public InfoRecord FindInfoRecord(string recordIV)
        {    
            return _infoRecordsLookup.Values.SelectMany(r => r).SingleOrDefault(r => r.IV == recordIV);
        }

        public IEnumerable<InfoRecord> GetAllInfoRecords()
        {
            throw new NotImplementedException();
        }

        public void AddInfoRecord(InfoRecord infoRecord)
        {
            string parentIV = infoRecord.ParentIV != null ? infoRecord.ParentIV : ROOT_DUMMY_IV;

            if (!_infoRecordsLookup.ContainsKey(parentIV))
                _infoRecordsLookup.Add(parentIV, new List<InfoRecord>());

            _infoRecordsLookup[parentIV].Add(infoRecord);
        }

      
        public void RemoveInfoRecord(InfoRecord infoRecord)
        {
            if (_infoRecordsLookup.ContainsKey(infoRecord.IV)) {
                foreach (var childInfoRecord in _infoRecordsLookup[infoRecord.IV].ToList())
                {
                    RemoveInfoRecord(childInfoRecord);
                }
            }
            _infoRecordsLookup[infoRecord.ParentIV ?? ROOT_DUMMY_IV].Remove(infoRecord);
        }

        public void ClearRecords()
        {
            _infoRecordsLookup = new Dictionary<string, List<InfoRecord>>();
        }
    }
}