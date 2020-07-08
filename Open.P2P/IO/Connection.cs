//
// - Connection.cs
//
// Author:
//     Lucas Ontivero <lucasontivero@gmail.com>
//
// Copyright 2013 Lucas E. Ontivero
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

// <summary></summary>

using Open.P2P.Utils;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Open.P2P.IO
{
    internal class Connection : IConnection
    {
        private static readonly BlockingPool<SocketAwaitable> SocketAwaitablePool =
            new BlockingPool<SocketAwaitable>(() =>
            new SocketAwaitable(new SocketAsyncEventArgs()));

        private readonly Socket _socket;
        private readonly IPEndPoint _endpoint;
        private bool _socketDisposed;

        private readonly EndPoint _localEndPoint;
        public EndPoint LocalEndPoint => _localEndPoint;

        public IPEndPoint Endpoint
        {
            get { return _endpoint; }
        }

        public bool IsConnected
        {
            get { return _socket.Connected; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "The framework manages the socket lifetime")]
        internal Connection(IPEndPoint remoteEndPoint, EndPoint localEndPoint = null)
            : this(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), remoteEndPoint, localEndPoint)
        { }


        internal Connection(Socket socket)
            : this(socket, (IPEndPoint)socket.RemoteEndPoint, socket.LocalEndPoint)
        { }

        internal Connection(Socket socket, IPEndPoint remoteEndPoint, EndPoint localEndPoint = null)
        {
            _socket = socket;
            _localEndPoint = localEndPoint;
            _endpoint = remoteEndPoint;
            _socketDisposed = false;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            }
        }


        public async Task<int> ReceiveAsync(ArraySegment<byte> buffer)
        {
            var awaitable = SocketAwaitablePool.Take();
            try
            {
                awaitable.EventArgs.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
                await _socket.ReceiveAsync(awaitable);
                return awaitable.EventArgs.BytesTransferred;
            }
            finally
            {
                SocketAwaitablePool.Add(awaitable);
            }
        }

        public async Task<int> SendAsync(ArraySegment<byte> buffer)
        {
            var awaitable = SocketAwaitablePool.Take();
            try
            {
                awaitable.EventArgs.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
                await _socket.SendAsync(awaitable);
                return awaitable.EventArgs.BytesTransferred;
            }
            finally
            {
                SocketAwaitablePool.Add(awaitable);
            }
        }

        public async Task ConnectAsync()
        {
            var awaitable = SocketAwaitablePool.Take();

            //TODO - rpinto IPV6 is not supported...
            awaitable.EventArgs.RemoteEndPoint = Endpoint;
            if (_localEndPoint != null)
            {
                var localEndPoint = (IPEndPoint)_localEndPoint;
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    //Console.WriteLine($"Binding socket locally");
                    _socket.Bind(new IPEndPoint(IPAddress.Parse("0.0.0.0"), localEndPoint.Port));
                }

                else
                {
                    //Console.WriteLine($"Binding socket locally");
                    _socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), localEndPoint.Port));
                }
            }

            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            await _socket.ConnectAsync(awaitable);

            SocketAwaitablePool.Add(awaitable);
        }

        public void Close()
        {
            try
            {
                _socket.Close();
            }
            finally
            {
                _socketDisposed = true;
            }
        }

        private bool CheckDisconnectedOrDisposed()
        {
            var disconnected = !IsConnected;
            if (disconnected || _socketDisposed)
            {
            }
            return disconnected;
        }
    }
}