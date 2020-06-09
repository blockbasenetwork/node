using System.Threading.Tasks;
using BlockBase.Utils.Threading;

namespace BlockBase.Runtime.Requester
{
    public interface ISidechainMaintainerManager
    {
        TaskContainer TaskContainerMaintainer { get; }
        TaskContainer TaskContainerProduction { get; }
        TaskContainer TaskContainerConnections { get; }

        bool IsMaintainerRunning();
        bool IsProductionRunning();


        Task Start();

        Task Pause();
        Task End();
    }
}