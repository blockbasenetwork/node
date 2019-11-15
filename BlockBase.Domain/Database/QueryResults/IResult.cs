using BlockBase.Domain.Database.Columns;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database
{
    public interface IResult
    {
        string TableName { get; set; }
        string ColumnName { get; set; }
        ColumnType Type { get; set; }
        void AddSqlValue(object value);
        void AddMySqlValue(object value);
    }
}
