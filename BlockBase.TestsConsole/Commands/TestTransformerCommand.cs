using Antlr4.Runtime;
using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy;
using BlockBase.DataProxy.Encryption;
using BlockBase.TestsConsole.Commands.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using BlockBase.DataPersistence.Sidechain.Connectors;


namespace BlockBase.TestsConsole.Commands
{
    internal class TestTransformerCommand : IHelperCommand
    {
        private ExecuteSqlCommand _executer;
        public TestTransformerCommand(ILogger logger)
        {
            var secretStore = new SecretStore();
            secretStore.SetSecret("master_key", KeyAndIVGenerator_v2.CreateRandomKey());
            secretStore.SetSecret("master_iv", KeyAndIVGenerator_v2.CreateMasterIV("qwerty123"));
            var databaseKeyManager = new DatabaseKeyManager(secretStore);
            var middleMan = new MiddleMan(databaseKeyManager, secretStore);
            _executer = new ExecuteSqlCommand(middleMan, logger, new PSqlConnector("localhost", "postgres", 5432, "qwerty123", logger));
        }

        public async Task ExecuteAsync()
        {
            await RunSqlCommand("CREATE DATABASE database2;");
            await RunSqlCommand("CREATE DATABASE database1;");
            await RunSqlCommand("USE database1;");

            await RunSqlCommand("CREATE TABLE table1 ( position ENCRYPTED RANGE (2, 1, 10) PRIMARY KEY, !column2 ENCRYPTED 2 NOT NULL);");
            await RunSqlCommand("CREATE TABLE table2 ( column1 ENCRYPTED RANGE (2, 1, 10) PRIMARY KEY REFERENCES table1 ( position ), column2 ENCRYPTED 40 );");
            //RunSqlCommand("CREATE TABLE table3 ( column1 ENCRYPTED 5 PRIMARY KEY REFERENCES table1 ( position ), column2 ENCRYPTED 40 );");
            await RunSqlCommand("CREATE TABLE !table4 ( !column1 int PRIMARY KEY);");
            await RunSqlCommand("CREATE TABLE !table5 ( !column1 int PRIMARY KEY REFERENCES !table4 ( !column1 ), column2 ENCRYPTED 40 );");
            await RunSqlCommand("CREATE TABLE !table6 ( !column1 int PRIMARY KEY REFERENCES !table4 ( !column1 ), column2 ENCRYPTED 40 );");

            await RunSqlCommand("DROP TABLE table2;");

            await RunSqlCommand("ALTER TABLE table1 RENAME TO newtable11");
            await RunSqlCommand("ALTER TABLE newtable11 RENAME TO bestplayers");
            await RunSqlCommand("ALTER TABLE bestplayers RENAME !column2 TO name");
            await RunSqlCommand("ALTER TABLE bestplayers ADD COLUMN !number int");
            await RunSqlCommand("ALTER TABLE bestplayers ADD COLUMN column4 ENCRYPTED 30 NOT NULL");
            await RunSqlCommand("ALTER TABLE bestplayers DROP COLUMN column4");

            await RunSqlCommand("INSERT INTO !table4 (!column1) VALUES ( 1 )");
            await RunSqlCommand("INSERT INTO !table4 (!column1) VALUES ( 2 )");
            await RunSqlCommand("INSERT INTO !table4 (!column1) VALUES ( 3 )");

            await RunSqlCommand("INSERT INTO !table5 (!column1, column2) VALUES ( 1, 'primeiro' )");
            await RunSqlCommand("INSERT INTO !table5 (!column1, column2) VALUES ( 2, 'segundo' )");
            await RunSqlCommand("INSERT INTO !table5 (!column1, column2) VALUES ( 3, 'terceiro' )");

            await RunSqlCommand("INSERT INTO !table6 (!column1, column2) VALUES ( 1, 'first' )");
            await RunSqlCommand("INSERT INTO !table6 (!column1, column2) VALUES ( 2, 'second' )");
            await RunSqlCommand("INSERT INTO !table6 (!column1, column2) VALUES ( 3, 'third' )");

            await RunSqlCommand("INSERT INTO bestplayers (position, name, !number) VALUES ( 1, 'Cristiano Ronaldo', 7 )");
            await RunSqlCommand("INSERT INTO bestplayers (position, name, !number) VALUES ( 2, 'bulha', 7 )");
            await RunSqlCommand("INSERT INTO bestplayers (position, name, !number) VALUES ( 3, 'bulha', 25 )");
            await RunSqlCommand("INSERT INTO bestplayers (position, name, !number) VALUES ( 4, 'pires', 10 )");
            await RunSqlCommand("INSERT INTO bestplayers (position, name, !number) VALUES ( 5, 'fernando', 25 )");
            await RunSqlCommand("INSERT INTO bestplayers (position, name, !number) VALUES ( 6, 'marcia', 26 )");
            await RunSqlCommand("INSERT INTO bestplayers (position, name, !number) VALUES ( 7, 'marcia', 290)");

            await RunSqlCommand("SELECT bestplayers.name FROM bestplayers WHERE bestplayers.name = 'bulha';");

            await RunSqlCommand("SELECT bestplayers.* FROM bestplayers WHERE bestplayers.name = 'bulha' OR ( bestplayers.!number > 10 AND bestplayers.!number <= 26 );");

            await RunSqlCommand("SELECT bestplayers.* FROM bestplayers");

            await RunSqlCommand("SELECT !table4.*, !table5.* FROM !table4 JOIN !table5 ON !table4.!column1 = !table5.!column1;");

            await RunSqlCommand("SELECT !table4.*, !table5.* FROM !table4 JOIN !table5 ON !table4.!column1 = !table5.!column1 AND !table4.!column1 = 2 ;");

            await RunSqlCommand("SELECT !table4.*, !table5.* FROM !table4 JOIN !table5 ON !table4.!column1 = !table5.!column1 WHERE !table4.!column1 = 2;");

            await RunSqlCommand("SELECT !table4.*, !table5.column2, !table6.column2  FROM !table4 JOIN !table5 ON !table4.!column1 = !table5.!column1 JOIN !table6 ON !table4.!column1 = !table6.!column1 WHERE !table4.!column1 = 2;");

            await RunSqlCommand("SELECT !table5.column2, !table6.column2  FROM !table5 JOIN !table6 ON !table5.column2 = !table6.column2;");

            await RunSqlCommand("SELECT !table5.column2, !table6.column2  FROM !table5, !table6 WHERE !table5.!column1 = !table6.!column1;");

            await RunSqlCommand("UPDATE bestplayers SET name = 'ricardo', number = 1000 where bestplayers.name = 'marcia'");
            await RunSqlCommand("SELECT bestplayers.* FROM bestplayers");


            await RunSqlCommand("UPDATE bestplayers SET name = 'ricardo' where bestplayers.number = 25");
            await RunSqlCommand("SELECT bestplayers.* FROM bestplayers");

            await RunSqlCommand("LIST");
            await RunSqlCommand("CURRENT_DATABASE");

            await RunSqlCommand("DROP DATABASE database1;");
            await RunSqlCommand("DROP DATABASE database2;");
        }

        private async Task RunSqlCommand(string plainSqlCommand)
        {
            await _executer.Execute(plainSqlCommand);
        }

        public string GetCommandHelp()
        {
            return "tt";
        }

        public bool TryParseCommand(string commandStr)
        {
            return "tt" == commandStr;
        }
    }
}