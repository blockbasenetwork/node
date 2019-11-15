using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.Utils.Operation
{
    public class Repeater
    {
        public static async Task<T> TryAgain<T>(Func<Task<OpResult<T>>> func, int maxTry)
        {
            Exception exception = null;

            for (int i = 0; i < maxTry-1; i++)
            {
                var opResult = await func.Invoke();

                if (opResult.Succeeded)
                {
                    return opResult.Result;
                }
                else
                {
                    exception = opResult.Exception;
                }
            }

            throw exception;
        }
    }
}
