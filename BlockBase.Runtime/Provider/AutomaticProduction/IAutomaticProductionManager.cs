using System.Threading.Tasks;
using BlockBase.Utils.Threading;

namespace BlockBase.Runtime.Provider.AutomaticProduction
{
    public interface IAutomaticProductionManager
    {
        TaskContainer AutomaticProductionTaskContainer { get; set; }

        TaskContainer Start();
    }
}