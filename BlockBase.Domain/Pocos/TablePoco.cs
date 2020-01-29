
using System.Collections.Generic;

namespace BlockBase.Domain.Pocos
{
    public class TablePoco
    {
        public IList<FieldPoco> Fields { get; set; }
        public string Name { get; private set; }

        public TablePoco(string name)
        {
            Name = name;
            Fields = new List<FieldPoco>();
        }
    }
}