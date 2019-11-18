using Blockbase.ProducerD.Commands.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Blockbase.ProducerD;
using BlockBase.Utils;
using System.Threading.Tasks;

namespace Blockbase.ProducerD.Commands
{
    class ConnectToDatabaseCommand : IExecutionCommand
    {
        private string ServerName { get; set; }
        private string DatabaseName { get; set; }
        private string UserId { get; set; }
        private string Password { get; set; }
        private SystemConfig SystemConfig { get; set; }

        public string Name => "Connect to Database Command (ctdb)";

        public ConnectToDatabaseCommand(SystemConfig config)
        {
            SystemConfig = config;
        }

        public async Task ExecuteAsync()
        {
            //SystemConfig.LocalDatabase = new ManageLocalDatabase(ServerName, DatabaseName, UserId, Password);
            Console.WriteLine("Connection string inserted");
            
        }

        public string GetCommandHelp()
        {
            return "ctdb <serverName> <databaseName> <userID> <password>";
        }

        public bool TryParseCommand(string commandStr)
        {
            //TODO Validations
            var commandData = commandStr.Split(new string[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            if (commandData.Length != 5 || commandData[0] != "ctdb" || commandData[1] == "" ||
                commandData[2] == "" || commandData[3] == "" || commandData[4] == "") return false;
            
            Console.WriteLine("Valid");

            ServerName = commandData[1];
            DatabaseName = commandData[2];
            UserId = commandData[3];
            Password = commandData[4];

            return true;
        }
    }
}
     



