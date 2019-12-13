using System.Threading.Tasks;

namespace BlockBase.TestsConsole.Commands.Interfaces
{
    public interface ICommand
    {
        Task ExecuteAsync();

        bool TryParseCommand(string commandStr);

        string GetCommandHelp();
    }
}