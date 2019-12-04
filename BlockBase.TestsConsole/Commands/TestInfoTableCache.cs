using BlockBase.DataPersistence.Sidechain.Connectors;
using BlockBase.DataProxy;
using BlockBase.DataProxy.Encryption;
using BlockBase.TestsConsole.Commands.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.TestsConsole.Commands
{
    class TestInfoTableCache : IHelperCommand
    {
        private ILogger _logger;
        private InfoTableCache _cache;


        public TestInfoTableCache(ILogger logger)
        {
            _logger = logger;
            _cache = new InfoTableCache(new PSqlConnector("localhost", "postgres", 5432, "qwerty123", _logger), new Encryptor());
        }

        public async Task ExecuteAsync()
        {
            await _cache.Build();
        }

        public string GetCommandHelp()
        {
           return "itc";
        }

        public bool TryParseCommand(string commandStr)
        {
            return commandStr == "itc";
        }
    }
}
