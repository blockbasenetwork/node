
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace BlockBase.Domain.Pocos
{
public class FieldPoco
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public JArray Data { get; private set; }

        public FieldPoco(string name, string type, JArray data)
        {
            Name = name;
            Type = type;
            Data = data;
        }

    }
}