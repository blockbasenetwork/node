using BlockBase.Domain.Database.Columns;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BlockBase.Domain.Database
{
     public class Column : ICloneable
    {
        public string Name { get; set; }
        public SqlDbType Type { get; set; }
        public bool NotNull { get; set; }
        public int Size { get; set; }
        public ColumnType ColumnType { get; set; }
        
        public Column() { }
        public Column(string name, SqlDbType type, bool notNull) 
        {
            NotNull = notNull;
            Name = name;
            Type = type;
        }
        public Column(string name, SqlDbType type, bool notNull, int size) : this(name, type, notNull)
        {
            Size = size;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
