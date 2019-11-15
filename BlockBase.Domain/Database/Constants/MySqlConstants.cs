using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Constants
{
    public class MySqlConstants : DatabaseConstants
    {
        public override string TABLE_NAME_PREFIX => "Table_";
        public override string METAINFO_TABLE_NAME => "metainfo";
        public override string COLUMN_TYPE_TABLE_NAME => "columntype";
        public override string COLUMN_INFO_TABLE => "columninfo";
        public override string FOREIGN_COLUMNS_TABLE_NAME => "foreigncolumns";
        public override string NORMAL_COLUMNS_TABLE_NAME => "normalcolumns";
        public override string PRIMARY_COLUMNS_TABLE_NAME => "primarycolumns";
        public override string RANGE_COLUMNS_TABLE_NAME => "rangecolumns";
        public override string UNIQUE_COLUMNS_TABLE_NAME => "uniquecolumns";
    }
}
