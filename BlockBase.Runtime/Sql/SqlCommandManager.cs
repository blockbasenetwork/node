﻿using System;
using BlockBase.DataProxy.Encryption;
using BlockBase.Domain.Database.Sql.Generators;
using BlockBase.DataPersistence.Sidechain.Connectors;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using BlockBase.DataProxy;
using System.Collections.Generic;
using BlockBase.Domain.Results;
using BlockBase.Domain.Pocos;
using BlockBase.Runtime.Network;
using BlockBase.Domain.Configurations;
using BlockBase.DataPersistence.Data;

namespace BlockBase.Runtime.Sql
{
    public class SqlCommandManager
    {
        private Transformer _transformer;
        private IGenerator _generator;
        private InfoPostProcessing _infoPostProcessing;
        private IConnector _connector;
        private ILogger _logger;
        private ConcurrentVariables _concurrentVariables;
        private TransactionsManager _transactionSender;
        private NodeConfigurations _nodeConfigurations;
        private IMongoDbRequesterService _mongoDbRequesterService;
        private MiddleMan _middleMan;


        public SqlCommandManager(MiddleMan middleMan, ILogger logger, IConnector connector, ConcurrentVariables concurrentVariables, TransactionsManager transactionSender, NodeConfigurations nodeConfigurations, IMongoDbRequesterService mongoDbRequesterService)
        {
            _infoPostProcessing = new InfoPostProcessing(middleMan);
            _generator = new PSqlGenerator();
            _logger = logger;
            _connector = connector;
            _middleMan = middleMan;
            _transformer = new Transformer(_middleMan);
            _concurrentVariables = concurrentVariables;
            _logger = logger;
            _transactionSender = transactionSender;
            _nodeConfigurations = nodeConfigurations;
            _mongoDbRequesterService = mongoDbRequesterService;
        }

        public async Task<IList<QueryResult>> Execute(string sqlString)
        {
            IList<QueryResult> results = new List<QueryResult>();

            try
            {
                var executioner = new StatementExecutionManager(_transformer, _generator, _logger, _connector, _infoPostProcessing, _concurrentVariables, _transactionSender, _nodeConfigurations, _mongoDbRequesterService);
                results = await executioner.ExecuteSqlText(sqlString, CreateQueryResult);
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Error executing query: {e}");
                results.Add(CreateQueryResult(false, "script", e.Message));
            }
            return results;
        }


        private QueryResult CreateQueryResult(bool success, string statementType, string exceptionMessage = null)
        {
            var executed = success ? "True" : "False";
            var message = $"The {statementType} statement " + (success ? "executed correctly." : "didn't execute. Exception: " + exceptionMessage);
            return new QueryResult(
                new List<IList<string>>()
                {
                    new List<string>() {executed, message}
                },
                new List<string>() { "Executed", "Message" }
            );
        }

        public IList<DatabasePoco> GetStructure()
        {
            return _infoPostProcessing.GetStructure();
        }

        public async Task RemoveSidechainDatabasesAndKeys()
        {
            var databases = _infoPostProcessing.GetEncryptedDatabasesList();

            foreach(var database in databases) 
                await _connector.DropDatabase(database);

            await _connector.DropDefaultDatabase();
            _middleMan.DatabaseKeyManager.ClearInfoRecords();
            SecretStore.ClearSecrets();
        }

    }
}
