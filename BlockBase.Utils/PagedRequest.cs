using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BlockBase.Utils
{
    public class PagedRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public ColumnRequest.SearchRequest Search { get; set; }
        public List<ColumnRequest> Columns { get; set; }
        public List<OrderRequest> Order { get; set; }

        public class OrderRequest
        {
            public int Column { get; set; }
            public string Dir { get; set; }
        }

        public class ColumnRequest
        {
            public string Data { get; set; }
            public string Name { get; set; }
            public bool Searchable { get; set; }
            public bool Orderable { get; set; }
            public SearchRequest Search { get; set; }

            public class SearchRequest
            {
                public string Value { get; set; }
                public string Regex { get; set; }
            }
        }
    }
}

