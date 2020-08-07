
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace BlockBase.Domain.Pocos
{
public class FieldPoco
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public IList<string> Data { get; private set; }

        public FieldPoco(string name, string type, IList<string> data)
        {
            Name = name;
            Type = type;
            Data = data;
        }

    }
}