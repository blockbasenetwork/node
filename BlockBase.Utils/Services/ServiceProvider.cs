using System;
using BlockBase.Utils.Extensions;

namespace BlockBase.Utils.Services
{
    public class ServiceProvider
    {
        private static object _locker = new object();

        private static IServiceProvider _serviceProvider;

        public static void Set(IServiceProvider serviceProvider)
        {
            lock(_locker)
            {
                if(_serviceProvider != null) throw new Exception("Service provider already set");
                _serviceProvider = serviceProvider;
            }
        }

        public static IServiceProvider Get() { return _serviceProvider; }

        public static T GetService<T>() where T : class
        {
            return _serviceProvider?.Get<T>();
        }
    }
}