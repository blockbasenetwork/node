using System.Threading.Tasks;

namespace Blockbase.ProducerD.Commands.Interfaces
{
    public interface ICommand
    {
        Task ExecuteAsync();

        bool TryParseCommand(string commandStr);

        string GetCommandHelp();
    }
}