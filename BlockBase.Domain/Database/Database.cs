using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Domain.Database
{
    public class Database
    {
        public Database(string serverName, string databaseName)
        {
            DatabaseName = databaseName;

            ServerName = serverName;
        }
        public string DatabaseName { get; set; }
        public string ServerName { get; set; }
    }
}
