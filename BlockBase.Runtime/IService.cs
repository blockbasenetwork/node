using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Runtime
{
    public interface IService : IDisposable
    {
        void Run();
    }
}
