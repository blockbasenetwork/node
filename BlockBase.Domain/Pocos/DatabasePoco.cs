
using System.Collections.Generic;

namespace BlockBase.Domain.Pocos
{
public class DatabasePoco
    {
        public IList<TablePoco> Tables { get; set; }
        public string Name { get; private set; }

        public DatabasePoco(string name)
        {
            Name = name;
            Tables = new List<TablePoco>();
        }
    }
}