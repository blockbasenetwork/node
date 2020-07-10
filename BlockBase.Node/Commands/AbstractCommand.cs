using System;
using System.Threading.Tasks;
using BlockBase.Node.Commands.Utils;

namespace BlockBase.Node.Commands
{
    public abstract class AbstractCommand : ICommand
    {
        public abstract string CommandName { get; }

        public abstract string CommandInfo { get; }

        public abstract string CommandUsage { get; }

        public abstract Task<CommandExecutionResult> Execute();

        public CommandParseResult TryLoadCommand(string commandStr)
        {
            if (!IsCommandRecognizable(commandStr)) return new CommandParseResult(false, false);
            var commandData = commandStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if(!IsCommandAppropratelyStructured(commandData)) return new CommandParseResult(true, CommandUsage);
            return ParseCommand(commandData);
        }

        protected abstract bool IsCommandRecognizable(string commandStr);
        protected abstract bool IsCommandAppropratelyStructured(string[] commandData);

        protected abstract CommandParseResult ParseCommand(string[] commandData);

    }
}