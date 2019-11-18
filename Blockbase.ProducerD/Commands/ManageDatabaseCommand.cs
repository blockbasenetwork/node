using Blockbase.ProducerD.Commands.Interfaces;
using BlockBase.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Blockbase.ProducerD.Commands
{
    public class ManageDatabaseCommand : IExecutionCommand
    {
        public string QueryType { get; set; }

        public string Query { get; set; }

        private SystemConfig SystemConfig { get; set; }

        public ManageDatabaseCommand(SystemConfig config)
        {
            SystemConfig = config;
        }

        public async Task ExecuteAsync()
        {

            if(QueryType.ToUpper().Equals("SELECT"))
            {
                //SystemConfig.LocalDatabase.ExecuteSQLSelectCommand<string>(Query);
                //Console.WriteLine("Query executed with success.");
            }
            else
            {
                //SystemConfig.LocalDatabase.ExecuteSQLCommand(Query);
                //Console.WriteLine("Query executed with success.");
            }
            
        }

        public string GetCommandHelp()
        {
            return "mdbc <query>";
        }

        public bool TryParseCommand(string commandStr)
        {
            //TODO better validation
            var commandData = commandStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            if (commandData.Length < 4 || commandData[0] != "mdbc" || commandData[1] == "") return false;
            
            Console.WriteLine("Valid");
            
            QueryType = commandData[1];

            for(int i = 1; i < commandData.Length; i++)
            {
                Query += commandData[i] + " ";
            }

            return true;
        }
    }
}
