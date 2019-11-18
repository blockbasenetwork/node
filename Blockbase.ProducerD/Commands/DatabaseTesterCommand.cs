using BlockBase.Domain.Database;
using BlockBase.Domain.Database.Operations;
using BlockBase.DataPersistence;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy;
using Blockbase.ProducerD.Commands.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Text;
using System.Threading.Tasks;

namespace Blockbase.ProducerD.Commands
{
    class DatabaseTesterCommand : IHelperCommand
    {
        public int Index { get; set; }
        private ILogger _logger;
        private ProxyTest _proxyTest;

        public DatabaseTesterCommand(ILogger logger)
        {
          _logger = logger;  
           _proxyTest = new ProxyTest(_logger);
        }

        public async Task ExecuteAsync()
        {
           if(Index == 0)
           {
                _proxyTest.TestQuery();
           }
           if(Index == 1)
            {
                await _proxyTest.CreateTestDatabase();
                // IConnector connector = new MySQLConnector("localhost", "root", 3306, "qwerty123");
                // var dbServerManager = new DbServerManager("localhost", connector);
                // await dbServerManager.PopulateDatabase("Example");
            }
           if(Index == 2)
            {
                var test = new TestSidechainManager();
                test.Execute();
            }
            if (Index == 3)
            {
                // var proxy = new ProxyTest();
                // proxy.PopulateMongoDB();
            }
        }

        public string GetCommandHelp()
        {
            throw new NotImplementedException();
        }

        public bool TryParseCommand(string commandStr)
        {
            var commandData = commandStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (commandData.Length != 2) return false;
            if (commandData[1].Equals("-t"))
                Index = 0;
            if (commandData[1]=="-s")
                Index = 1;
            if (commandData[1] == "-setup")
                Index = 2;
            if (commandData[1] == "-p")
                Index = 3;
            return commandData[0] == "dbt";
        }
    }
}
