﻿using BlockBase.Domain.Database.Sql.QueryBuilder.Elements.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database.Sql.SqlCommand
{
    public class GenericSqlCommand : ISqlCommand
    {
        public IList<string> TransformedSqlStatementText { get; set; }
        public ISqlStatement OriginalSqlStatement { get; set; }
        public IList<ISqlStatement> TransformedSqlStatement { get; set; }

        public GenericSqlCommand(ISqlStatement sqlStatement)
        {
            OriginalSqlStatement = sqlStatement;
        }

    }
}
