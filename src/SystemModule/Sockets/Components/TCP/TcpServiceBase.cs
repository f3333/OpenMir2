using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SystemModule.ByteManager;
using SystemModule.Core.Common;
using SystemModule.Core.Config;
using SystemModule.Dependency;
using SystemModule.Sockets.Common;
using SystemModule.Sockets.Enum;
using SystemModule.Sockets.Exceptions;
using SystemModule.Sockets.Interface;
using SystemModule.Sockets.SocketEventArgs;

namespace SystemModule.Sockets.Components.TCP
{
    /// <summary>
    /// Tcp服务器基类
    /// </summary>
    public abstract class TcpServiceBase : BaseSocket, ITcpService
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public abstract TouchSocketConfig Config { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public abstract IContainer Container { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public int Count => SocketClients.Count;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public abstract Common.NetworkMonitor[] Monitors { get; }
        
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public abstract string ServerName { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public abstract ServerState ServerState { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public abstract SocketClientCollection SocketClients { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public abstract Func<string> GetDefaultNewID { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public abstract int MaxCount { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public string[] GetIDs()
        {
            return SocketClients.GetIDs().ToArray();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="oldID"></param>
        /// <param name="newID"></param>
        public abstract void ResetID(string oldID, string newID);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="serverConfig"></param>
        /// <returns></returns>
        public abstract IService Setup(TouchSocketConfig serverConfig);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public abstract IService Setup(int port);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public abstract IService Start();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public abstract IService Stop();

        internal void OnInternalConnected(ISocketClient socketClient, TouchSocketEventArgs e)
        {
            OnClientConnected(socketClient, e);
        }

        internal void OnInternalConnecting(ISocketClient socketClient, OperationEventArgs e)
        {
            OnClientConnecting(socketClient, e);
        }

        internal void OnInternalDisconnected(ISocketClient socketClient, DisconnectEventArgs e)
        {
            OnClientDisconnected(socketClient, e);
        }

        internal void OnInternalDisconnecting(ISocketClient socketClient, DisconnectEventArgs e)
        {
            OnClientDisconnecting(socketClient, e);
        }

        internal void OnInternalReceivedData(ISocketClient socketClient, ByteBlock byteBlock, IRequestInfo requestInfo)
        {
            OnClientReceivedData(socketClient, byteBlock, requestInfo);
        }

        /// <summary>
        /// 客户端连接完成
        /// </summary>
        /// <param name="socketClient"></param>
        /// <param name="e"></param>
        protected abstract void OnClientConnected(ISocketClient socketClient, TouchSocketEventArgs e);

        /// <summary>
        /// 客户端请求连接
        /// </summary>
        /// <param name="socketClient"></param>
        /// <param name="e"></param>
        protected abstract void OnClientConnecting(ISocketClient socketClient, OperationEventArgs e);

        /// <summary>
        /// 客户端断开连接
        /// </summary>
        /// <param name="socketClient"></param>
        /// <param name="e"></param>
        protected abstract void OnClientDisconnected(ISocketClient socketClient, DisconnectEventArgs e);

        /// <summary>
        /// 即将断开连接(仅主动断开时有效)。
        /// <para>
        /// 当主动调用Close断开时，可通过<see cref="TouchSocketEventArgs.IsPermitOperation"/>终止断开行为。
        /// </para>
        /// </summary>
        /// <param name="socketClient"></param>
        /// <param name="e"></param>
        protected abstract void OnClientDisconnecting(ISocketClient socketClient, DisconnectEventArgs e);

        /// <summary>
        /// 收到数据时
        /// </summary>
        /// <param name="socketClient"></param>
        /// <param name="byteBlock"></param>
        /// <param name="requestInfo"></param>
        protected abstract void OnClientReceivedData(ISocketClient socketClient, ByteBlock byteBlock, IRequestInfo requestInfo);

        #region ID发送
        /// <summary>
        /// 发送字节流
        /// </summary>
        /// <param name="id">用于检索TcpSocketClient</param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="NotConnectedException"></exception>
        /// <exception cref="OverlengthException"></exception>
        /// <exception cref="Exception"></exception>
        public void Send(string id, ReadOnlyMemory<byte> buffer)
        {
            if (SocketClients.TryGetSocketClient(id, out ISocketClient client))
            {
                client.Send(buffer, 0, buffer.Length);
            }
            else
            {
                throw new ClientNotFindException(TouchSocketStatus.ClientNotFind.GetDescription(id));
            }
        }

        /// <summary>
        /// 发送字节流
        /// </summary>
        /// <param name="id">用于检索TcpSocketClient</param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="NotConnectedException"></exception>
        /// <exception cref="OverlengthException"></exception>
        /// <exception cref="Exception"></exception>
        public void Send(string id, ReadOnlyMemory<byte> buffer, int offset, int length)
        {
            Send(id, buffer);
        }

        /// <summary>
        /// 发送字节流
        /// </summary>
        /// <param name="id">用于检索TcpSocketClient</param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="NotConnectedException"></exception>
        /// <exception cref="OverlengthException"></exception>
        /// <exception cref="Exception"></exception>
        public void Send(string id, byte[] buffer, int offset, int length)
        {
            if (SocketClients.TryGetSocketClient(id, out ISocketClient client))
            {
                client.Send(buffer, offset, length);
            }
            else
            {
                throw new ClientNotFindException(TouchSocketStatus.ClientNotFind.GetDescription(id));
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="requestInfo"></param>
        public void Send(string id, IRequestInfo requestInfo)
        {
            if (SocketClients.TryGetSocketClient(id, out ISocketClient client))
            {
                client.Send(requestInfo);
            }
            else
            {
                throw new ClientNotFindException(TouchSocketStatus.ClientNotFind.GetDescription(id));
            }
        }

        /// <summary>
        /// 发送字节流
        /// </summary>
        /// <param name="id">用于检索TcpSocketClient</param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="NotConnectedException"></exception>
        /// <exception cref="OverlengthException"></exception>
        /// <exception cref="Exception"></exception>
        public Task SendAsync(string id, byte[] buffer, int offset, int length)
        {
            if (SocketClients.TryGetSocketClient(id, out ISocketClient client))
            {
                return client.SendAsync(buffer, offset, length);
            }
            else
            {
                throw new ClientNotFindException(TouchSocketStatus.ClientNotFind.GetDescription(id));
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="requestInfo"></param>
        public Task SendAsync(string id, IRequestInfo requestInfo)
        {
            if (SocketClients.TryGetSocketClient(id, out ISocketClient client))
            {
                return client.SendAsync(requestInfo);
            }
            else
            {
                throw new ClientNotFindException(TouchSocketStatus.ClientNotFind.GetDescription(id));
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public abstract bool SocketClientExist(string id);

        #endregion ID发送
    }
}