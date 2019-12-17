using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Record;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.DataProxy.Encryption.Pocos
{
    class CreateBktColumnResultPoco
    {
        public ColumnDefinition ColumnDefinition { get; set; }
        public InsertRecordStatement InsertRecordStatement { get; set; }
    }
}
