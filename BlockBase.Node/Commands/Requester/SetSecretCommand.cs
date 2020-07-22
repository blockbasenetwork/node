using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using System.Net;
using BlockBase.Domain.Configurations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using BlockBase.DataProxy.Encryption;
using System.IO;
using System.Text;
using BlockBase.Node.Commands.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BlockBase.Node.Commands.Requester
{
    public class SetSecretCommand : AbstractCommand
    {

        private ILogger _logger;
        private RequesterConfigurations _requesterConfigurations;
        private DatabaseKeyManager _databaseKeyManager;
        private bool _isEncrypted;
        private string _encryptionMasterKey;
        private string _encryptionPassword;
        private string _filePassword;
        private string _encryptedData;



        public override string CommandName => "Set secret";

        public override string CommandInfo => "Sets required encryption secrets";

        public override string CommandUsage => "set secret [--isEncrypted true --encryptedData <encryptedData> || --isEncrypted false --masterKey <encryptedMasterKey> --epassword <encryptionpassword> --fpassword <filepassword> ]";

        public SetSecretCommand(ILogger logger, RequesterConfigurations requesterConfigurations, DatabaseKeyManager databaseKeyManager, DatabaseSecurityConfigurations databaseSecurityConfigurations = null)
        {
            _requesterConfigurations = requesterConfigurations;
            _databaseKeyManager = databaseKeyManager;
            _logger = logger;
            _isEncrypted = databaseSecurityConfigurations?.IsEncrypted ?? false;
            _encryptionMasterKey = databaseSecurityConfigurations?.EncryptionMasterKey;
            _encryptionPassword = databaseSecurityConfigurations?.EncryptionPassword;
            _filePassword = databaseSecurityConfigurations?.FilePassword;
            _encryptedData = databaseSecurityConfigurations?.EncryptedData;

        }

        public decimal Stake { get; set; }

        public override async Task<CommandExecutionResponse> Execute()
        {
            try
            {
                if (_requesterConfigurations.DatabaseSecurityConfigurations.Use)
                {
                    _databaseKeyManager.SetInitialSecrets(_requesterConfigurations.DatabaseSecurityConfigurations);
                }

                else
                {
                    var newConfig = new DatabaseSecurityConfigurations()
                    {
                        IsEncrypted = _isEncrypted,
                        EncryptionMasterKey = _encryptionMasterKey,
                        EncryptionPassword = _encryptionPassword,
                        EncryptedData = _encryptedData
                    };
                    _databaseKeyManager.SetInitialSecrets(newConfig);
                }

                return new CommandExecutionResponse(HttpStatusCode.OK, new OperationResponse(true, "Secret set with success"));
            }
            catch (Exception e)
            {
                return new CommandExecutionResponse(HttpStatusCode.InternalServerError, new OperationResponse(e));
            }
        }

        protected override bool IsCommandAppropratelyStructured(string[] commandData)
        {
            return commandData.Length == 2 || commandData.Length == 6 || commandData.Length == 10;
        }

        protected override bool IsCommandRecognizable(string commandStr)
        {
            return commandStr.ToLower().StartsWith("set secret");
        }

        protected override CommandParseResult ParseCommand(string[] commandData)
        {
            if (commandData.Length == 2) return new CommandParseResult(true, true);
            if (commandData[2] != "--isEncrypted") return new CommandParseResult(true, CommandUsage);
            if (!Boolean.TryParse(commandData[3], out var isEncrypted)) return new CommandParseResult(true, "Unable to parse isEncrypted");
            _isEncrypted = isEncrypted;

            if (commandData.Length == 6)
            {
                if (commandData[4] != "--encryptedData") return new CommandParseResult(true, CommandUsage);
                _encryptedData = commandData[5];
                return new CommandParseResult(true, true);
            }
            if (commandData.Length == 10)
            {
                if (commandData[4] != "--masterKey") return new CommandParseResult(true, CommandUsage);
                _encryptionMasterKey = commandData[5];
                if (commandData[6] != "--epassword") return new CommandParseResult(true, CommandUsage);
                _encryptionMasterKey = commandData[7];
                if (commandData[8] != "--fpassword") return new CommandParseResult(true, CommandUsage);
                _encryptionMasterKey = commandData[9];
                return new CommandParseResult(true, true);
            }

            return new CommandParseResult(true, CommandUsage);
        }

    }
}