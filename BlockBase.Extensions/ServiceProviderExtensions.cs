using System;
using System.Collections.Generic;
using System.Text;

namespace BlockBase.Extensions
{
    public static class ServiceProviderExtensions
    {
        public static T Get<T>(this IServiceProvider serviceProvider)
        {
            return (T)serviceProvider.GetService(typeof(T));
        }
    }
}
