using System;
using System.Threading.Tasks;
using BlockBase.Node.Commands.Utils;

namespace BlockBase.Node.Commands
{
    public interface ICommand
    {
        string CommandName { get;}
        string CommandInfo { get; }
        string CommandUsage { get;}

        Task<CommandExecutionResponse> Execute();
        CommandParseResult TryLoadCommand(string commandStr);
    }
}