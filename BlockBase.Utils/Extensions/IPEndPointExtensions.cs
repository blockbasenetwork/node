using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BlockBase.Utils.Extensions
{
    public static class IPEndPointExtensions
    {
        public static bool IsEqualTo(this IPEndPoint ipEndPoint, IPEndPoint ipEndPointToCompare)
        {
            return ipEndPoint.Address.Equals(ipEndPointToCompare.Address) && ipEndPoint.Port == ipEndPointToCompare.Port;
        }
    }
}
