using System;
using System.Collections.Generic;
using SystemModule.ByteManager;
using SystemModule.Core.Common;
using SystemModule.Sockets.Interface;

namespace SystemModule.Sockets.DataAdapter
{
    /// <summary>
    /// 数据处理适配器
    /// </summary>
    public abstract class DataHandlingAdapter : DisposableObject
    {
        private ITcpClientBase m_client;

        /// <summary>
        /// 最后缓存的时间
        /// </summary>
        protected DateTime LastCacheTime { get; set; }

        /// <summary>
        /// 是否在收到数据时，即刷新缓存时间。默认true。
        /// <list type="number">
        /// <item>当设为true时，将弱化<see cref="CacheTimeout"/>的作用，只要一直有数据，则缓存不会过期。</item>
        /// <item>当设为false时，则在<see cref="CacheTimeout"/>的时效内。必须完成单个缓存的数据。</item>
        /// </list>
        /// </summary>
        public bool UpdateCacheTimeWhenRev { get; set; } = true;

        /// <summary>
        /// 缓存超时时间。默认1秒。
        /// </summary>
        public TimeSpan CacheTimeout { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// 是否启用缓存超时。默认true。
        /// </summary>
        public bool CacheTimeoutEnable { get; set; } = true;

        /// <summary>
        /// 当插件在被第一次加载时调用。
        /// </summary>
        /// <param name="client"></param>
        /// <exception cref="Exception">此适配器已被其他终端使用，请重新创建对象。</exception>
        public virtual void OnLoaded(ITcpClientBase client)
        {
            if (m_client != null)
            {
                throw new Exception("此适配器已被其他终端使用，请重新创建对象。");
            }
            m_client = client;
        }

        /// <summary>
        /// 拼接发送
        /// </summary>
        public abstract bool CanSplicingSend { get; }

        /// <summary>
        /// 是否允许发送<see cref="IRequestInfo"/>对象。
        /// </summary>
        public abstract bool CanSendRequestInfo { get; }

        /// <summary>
        /// 获取或设置适配器能接收的最大数据包长度。
        /// </summary>
        public int MaxPackageSize { get; set; } = 1024 * 1024 * 10;

        /// <summary>
        /// 适配器拥有者。
        /// </summary>
        public ITcpClientBase Client => m_client;

        /// <summary>
        /// 当接收数据处理完成后，回调该函数执行接收
        /// </summary>
        public Action<ByteBlock, IRequestInfo> ReceivedCallBack { get; set; }

        /// <summary>
        /// 当接收数据处理完成后，回调该函数执行发送
        /// </summary>
        public Action<ReadOnlyMemory<byte>, int, int> SendCallBack { get; set; }

        /// <summary>
        /// 收到数据的切入点，该方法由框架自动调用。
        /// </summary>
        /// <param name="byteBlock"></param>
        public void ReceivedInput(ByteBlock byteBlock)
        {
            try
            {
                PreviewReceived(byteBlock);
            }
            catch (Exception ex)
            {
                OnError(ex.Message);
            }
        }

        /// <summary>
        /// 发送数据的切入点，该方法由框架自动调用。
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public void SendInput(byte[] buffer, int offset, int length)
        {
            PreviewSend(buffer, offset, length);
        }

        /// <summary>
        /// 发送数据的切入点，该方法由框架自动调用。
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public void SendInput(ReadOnlyMemory<byte> buffer, int offset, int length)
        {
            PreviewSend(buffer, offset, length);
        }

        /// <summary>
        /// 发送数据的切入点，该方法由框架自动调用。
        /// </summary>
        /// <param name="transferBytes"></param>
        public void SendInput(IList<ArraySegment<byte>> transferBytes)
        {
            PreviewSend(transferBytes);
        }

        /// <summary>
        /// 发送数据的切入点，该方法由框架自动调用。
        /// </summary>
        /// <param name="requestInfo"></param>
        public void SendInput(IRequestInfo requestInfo)
        {
            PreviewSend(requestInfo);
        }

        /// <summary>
        /// 处理已经经过预先处理后的数据
        /// </summary>
        /// <param name="byteBlock">以二进制形式传递</param>
        /// <param name="requestInfo">以解析实例传递</param>
        protected void GoReceived(ByteBlock byteBlock, IRequestInfo requestInfo)
        {
            ReceivedCallBack.Invoke(byteBlock, requestInfo);
        }

        /// <summary>
        /// 发送已经经过预先处理后的数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        protected void GoSend(byte[] buffer, int offset, int length)
        {
            SendCallBack.Invoke(buffer, offset, length);
        }

        /// <summary>
        /// 发送已经经过预先处理后的数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        protected void GoSend(ReadOnlyMemory<byte> buffer, int offset, int length)
        {
            SendCallBack.Invoke(buffer, offset, length);
        }

        /// <summary>
        /// 在解析时发生错误。
        /// </summary>
        /// <param name="error">错误异常</param>
        /// <param name="reset">是否调用<see cref="Reset"/></param>
        /// <param name="log">是否记录日志</param>
        protected virtual void OnError(string error, bool reset = true, bool log = true)
        {
            if (reset)
            {
                Reset();
            }
        }

        /// <summary>
        /// 当接收到数据后预先处理数据,然后调用<see cref="GoReceived(ByteBlock, IRequestInfo)"/>处理数据
        /// </summary>
        /// <param name="byteBlock"></param>
        protected abstract void PreviewReceived(ByteBlock byteBlock);

        /// <summary>
        /// 当发送数据前预先处理数据
        /// </summary>
        /// <param name="buffer">数据</param>
        /// <param name="offset">偏移</param>
        /// <param name="length">长度</param>
        protected abstract void PreviewSend(byte[] buffer, int offset, int length);

        /// <summary>
        /// 当发送数据前预先处理数据
        /// </summary>
        /// <param name="buffer">数据</param>
        /// <param name="offset">偏移</param>
        /// <param name="length">长度</param>
        protected abstract void PreviewSend(ReadOnlyMemory<byte> buffer, int offset, int length);

        /// <summary>
        /// 当发送数据前预先处理数据
        /// </summary>
        /// <param name="requestInfo"></param>
        protected abstract void PreviewSend(IRequestInfo requestInfo);

        /// <summary>
        /// 组合发送预处理数据，
        /// 当属性SplicingSend实现为True时，系统才会调用该方法。
        /// </summary>
        /// <param name="transferBytes">代发送数据组合</param>
        protected abstract void PreviewSend(IList<ArraySegment<byte>> transferBytes);

        /// <summary>
        /// 重置解析器到初始状态，一般在<see cref="OnError(string, bool, bool)"/>被触发时，由返回值指示是否调用。
        /// </summary>
        protected abstract void Reset();

        /// <summary>
        /// 该方法被触发时，一般说明<see cref="Client"/>已经断开连接。
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}