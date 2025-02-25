using DBSrv.Conf;
using NLog;
using System;
using System.Collections.Generic;
using System.Net;
using SystemModule;
using SystemModule.SocketComponents.AsyncSocketClient;
using SystemModule.SocketComponents.Event;

namespace DBSrv.Services.Impl
{
    /// <summary>
    /// 登陆会话同步服务(DBSrv-LoginSrv)
    /// </summary>
    public class ClientSession
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly ScoketClient _clientScoket;
        private readonly IList<GlobaSessionInfo> _globaSessionList = null;
        private readonly SettingConf _setting;
        private string _sockMsg = string.Empty;

        public ClientSession(SettingConf conf)
        {
            _setting = conf;
            _clientScoket = new ScoketClient(new IPEndPoint(IPAddress.Parse(_setting.LoginServerAddr), _setting.LoginServerPort));
            _clientScoket.OnReceivedData += LoginSocketRead;
            _clientScoket.OnConnected += LoginSocketConnected;
            _clientScoket.OnDisconnected += LoginSocketDisconnected;
            _clientScoket.OnError += LoginSocketError;
            _globaSessionList = new List<GlobaSessionInfo>();
        }

        private void LoginSocketError(object sender, DSCClientErrorEventArgs e)
        {
            switch (e.ErrorCode)
            {
                case System.Net.Sockets.SocketError.ConnectionRefused:
                    _logger.Warn("账号服务器[" + _setting.LoginServerAddr + ":" + _setting.LoginServerPort + "]拒绝链接...");
                    break;
                case System.Net.Sockets.SocketError.ConnectionReset:
                    _logger.Warn("账号服务器[" + _setting.LoginServerAddr + ":" + _setting.LoginServerPort + "]关闭连接...");
                    break;
                case System.Net.Sockets.SocketError.TimedOut:
                    _logger.Warn("账号服务器[" + _setting.LoginServerAddr + ":" + _setting.LoginServerPort + "]链接超时...");
                    break;
            }
        }

        private void LoginSocketConnected(object sender, DSCClientConnectedEventArgs e)
        {
            _logger.Info($"账号服务器[{e.RemoteEndPoint}]链接成功.");
        }

        private void LoginSocketDisconnected(object sender, DSCClientConnectedEventArgs e)
        {
            _logger.Error($"账号服务器[{e.RemoteEndPoint}]断开链接.");
        }

        public void Start()
        {
            _clientScoket.Connect();
        }

        public void Stop()
        {
            for (var i = 0; i < _globaSessionList.Count; i++)
            {
                _globaSessionList[i] = null;
            }
        }

        public void CheckConnection()
        {
            if (_clientScoket.IsConnected)
            {
                return;
            }
            if (_clientScoket.IsBusy)
            {
                return;
            }
            _logger.Debug($"开始接账号服务器[{_clientScoket.RemoteEndPoint}].");
            _clientScoket.Connect(_setting.LoginServerAddr, _setting.LoginServerPort);
        }

        private void LoginSocketRead(object sender, DSCClientDataInEventArgs e)
        {
            _sockMsg += HUtil32.GetString(e.Buff, 0, e.BuffLen);
            if (_sockMsg.IndexOf(")", StringComparison.OrdinalIgnoreCase) > 0)
            {
                ProcessSocketMsg();
            }
        }

        private void ProcessSocketMsg()
        {
            var sData = string.Empty;
            var sCode = string.Empty;
            var sScoketText = _sockMsg;
            while (sScoketText.IndexOf(")", StringComparison.OrdinalIgnoreCase) > 0)
            {
                sScoketText = HUtil32.ArrestStringEx(sScoketText, "(", ")", ref sData);
                if (string.IsNullOrEmpty(sData))
                {
                    break;
                }
                var sBody = HUtil32.GetValidStr3(sData, ref sCode, HUtil32.Backslash);
                var nIdent = HUtil32.StrToInt(sCode, 0);
                switch (nIdent)
                {
                    case Messages.SS_OPENSESSION:
                        ProcessAddSession(sBody);
                        break;
                    case Messages.SS_CLOSESESSION:
                        ProcessDelSession(sBody);
                        break;
                    case Messages.SS_KEEPALIVE:
                        ProcessGetOnlineCount(sBody);
                        break;
                }
            }
            _sockMsg = sScoketText;
        }

        public void SendSocketMsg(short wIdent, string sMsg)
        {
            const string sFormatMsg = "({0}/{1})";
            var sSendText = string.Format(sFormatMsg, wIdent, sMsg);
            if (_clientScoket.IsConnected)
            {
                _clientScoket.SendText(sSendText);
            }
        }

        public bool CheckSession(string account, string sIPaddr, int sessionId)
        {
            var result = false;
            for (var i = 0; i < _globaSessionList.Count; i++)
            {
                var globaSessionInfo = _globaSessionList[i];
                if (globaSessionInfo != null)
                {
                    if ((globaSessionInfo.sAccount == account) && (globaSessionInfo.nSessionID == sessionId))
                    {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

        public int CheckSessionLoadRcd(string sAccount, string sIPaddr, int nSessionId, ref bool boFoundSession)
        {
            var result = -1;
            boFoundSession = false;
            for (var i = 0; i < _globaSessionList.Count; i++)
            {
                var globaSessionInfo = _globaSessionList[i];
                if (globaSessionInfo != null)
                {
                    if ((globaSessionInfo.sAccount == sAccount) && (globaSessionInfo.nSessionID == nSessionId))
                    {
                        boFoundSession = true;
                        if (!globaSessionInfo.boLoadRcd)
                        {
                            globaSessionInfo.boLoadRcd = true;
                            result = 1;
                        }
                        break;
                    }
                }
            }
            return result;
        }

        public bool SetSessionSaveRcd(string sAccount)
        {
            var result = false;
            for (var i = 0; i < _globaSessionList.Count; i++)
            {
                var globaSessionInfo = _globaSessionList[i];
                if (globaSessionInfo != null)
                {
                    if ((globaSessionInfo.sAccount == sAccount))
                    {
                        globaSessionInfo.boLoadRcd = false;
                        result = true;
                    }
                }
            }
            return result;
        }

        public void SetGlobaSessionNoPlay(int nSessionId)
        {
            for (var i = 0; i < _globaSessionList.Count; i++)
            {
                var globaSessionInfo = _globaSessionList[i];
                if (globaSessionInfo != null)
                {
                    if ((globaSessionInfo.nSessionID == nSessionId))
                    {
                        globaSessionInfo.boStartPlay = false;
                        break;
                    }
                }
            }
        }

        public void SetGlobaSessionPlay(int nSessionId)
        {
            for (var i = 0; i < _globaSessionList.Count; i++)
            {
                var globaSessionInfo = _globaSessionList[i];
                if (globaSessionInfo != null)
                {
                    if ((globaSessionInfo.nSessionID == nSessionId))
                    {
                        globaSessionInfo.boStartPlay = true;
                        break;
                    }
                }
            }
        }

        public bool GetGlobaSessionStatus(int nSessionId)
        {
            var result = false;
            for (var i = 0; i < _globaSessionList.Count; i++)
            {
                var globaSessionInfo = _globaSessionList[i];
                if (globaSessionInfo != null)
                {
                    if ((globaSessionInfo.nSessionID == nSessionId))
                    {
                        result = globaSessionInfo.boStartPlay;
                        break;
                    }
                }
            }
            return result;
        }

        public void CloseSession(string sAccount, int nSessionId)
        {
            for (var i = 0; i < _globaSessionList.Count; i++)
            {
                var globaSessionInfo = _globaSessionList[i];
                if (globaSessionInfo != null)
                {
                    if ((globaSessionInfo.nSessionID == nSessionId))
                    {
                        if (globaSessionInfo.sAccount == sAccount)
                        {
                            globaSessionInfo = null;
                            _globaSessionList.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        private void ProcessAddSession(string sData)
        {
            var sAccount = string.Empty;
            var s10 = string.Empty;
            var s14 = string.Empty;
            var s18 = string.Empty;
            var sIPaddr = string.Empty;
            sData = HUtil32.GetValidStr3(sData, ref sAccount, HUtil32.Backslash);
            sData = HUtil32.GetValidStr3(sData, ref s10, HUtil32.Backslash);
            sData = HUtil32.GetValidStr3(sData, ref s14, HUtil32.Backslash);
            sData = HUtil32.GetValidStr3(sData, ref s18, HUtil32.Backslash);
            sData = HUtil32.GetValidStr3(sData, ref sIPaddr, HUtil32.Backslash);
            var globaSessionInfo = new GlobaSessionInfo();
            globaSessionInfo.sAccount = sAccount;
            globaSessionInfo.sIPaddr = sIPaddr;
            globaSessionInfo.nSessionID = HUtil32.StrToInt(s10, 0);
            //GlobaSessionInfo.n24 = HUtil32.Str_ToInt(s14, 0);
            globaSessionInfo.boStartPlay = false;
            globaSessionInfo.boLoadRcd = false;
            globaSessionInfo.dwAddTick = HUtil32.GetTickCount();
            globaSessionInfo.dAddDate = DateTime.Now;
            _globaSessionList.Add(globaSessionInfo);
            //_logger.Debug($"同步账号服务[{sAccount}]同步会话消息...");
        }

        private void ProcessDelSession(string sData)
        {
            var sAccount = string.Empty;
            sData = HUtil32.GetValidStr3(sData, ref sAccount, HUtil32.Backslash);
            var nSessionId = HUtil32.StrToInt(sData, 0);
            for (var i = 0; i < _globaSessionList.Count; i++)
            {
                var globaSessionInfo = _globaSessionList[i];
                if (globaSessionInfo != null)
                {
                    if ((globaSessionInfo.nSessionID == nSessionId) && (globaSessionInfo.sAccount == sAccount))
                    {
                        globaSessionInfo = null;
                        _globaSessionList.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public bool GetSession(string sAccount, string sIPaddr)
        {
            var result = false;
            for (var i = 0; i < _globaSessionList.Count; i++)
            {
                var globaSessionInfo = _globaSessionList[i];
                if (globaSessionInfo != null)
                {
                    if ((globaSessionInfo.sAccount == sAccount) && (globaSessionInfo.sIPaddr == sIPaddr))
                    {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

        private static void ProcessGetOnlineCount(string sData)
        {

        }

        public void SendKeepAlivePacket(int userCount)
        {
            if (_clientScoket.IsConnected)
            {
                _clientScoket.SendText("(" + Messages.SS_SERVERINFO + "/" + _setting.ServerName + "/" + "99" + "/" + userCount + ")");
            }
        }
    }
}