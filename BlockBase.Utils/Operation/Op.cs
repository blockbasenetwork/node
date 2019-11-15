using System;
using System.Threading.Tasks;

namespace BlockBase.Utils.Operation
{
    public static class Op
    {
        public static OpResult Run(Action func)
        {
            try
            {
                func.Invoke();
                return new OpResult(true);
            }
            catch (Exception ex)
            {
                return new OpResult(ex);
            }
        }

        public static OpResult<Tout> Run<Tout>(Func<Tout> func)
        {
            try
            {
                var result = func.Invoke();
                return new OpResult<Tout>(result);
            }
            catch (Exception ex)
            {
                return new OpResult<Tout>(ex);
            }
        }

        public static async Task<OpResult<Tout>> RunAsync<Tout>(Func<Task<Tout>> func)
        {
            try
            {
                var result = await func.Invoke();
                return new OpResult<Tout>(result);
            }
            catch (Exception ex)
            {
                return new OpResult<Tout>(ex);
            }
        }
    }
}