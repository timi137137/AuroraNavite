﻿using System;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraNative.WebSockets
{
    /// <summary>
    /// WebSocket 服务器 封装类
    /// <para>反向WebSocket</para>
    /// </summary>
    public class Server : BaseWebSocket
    {
        #region --变量--

        private string Port = "6700";
        /// <summary>
        /// WebSocket监听端口
        /// </summary>
        public string port
        {
            private get { return Port; }
            set { Port = value; }
        }

        private HttpListener Listener;
        private bool IsConnect = false;

        #endregion

        #region --构造函数--

        static Server()
        {
            AttributeTypes = Assembly.GetExecutingAssembly().GetTypes().Where(p => p.IsAbstract == false && p.IsInterface == false && typeof(Attribute).IsAssignableFrom(p)).ToArray();
        }

        /// <summary>
        /// 创建一个 <see cref="Server"/> 实例
        /// </summary>
        /// <param name="Event">重写后的事件类实例</param>
        public Server(Event Event) => EventHook = Event;

        #endregion

        #region --公开函数--

        /// <summary>
        /// 创建WebSocket服务器并监听端口
        /// </summary>
        public void Create()
        {
            try
            {
                Logger.Debug("反向WebSocket已创建，准备监听...", $"{MethodBase.GetCurrentMethod().DeclaringType.Name}.{MethodBase.GetCurrentMethod().Name}");
                Listener = new HttpListener();
                Listener.Prefixes.Add("http://*:" + Port + "/");
                Listener.Start();
                Logger.Info("开始监听来自 go-cqhttp 客户端的连接...");
                Task.Run(Feedback);
                while (!IsConnect) {
                    Thread.Sleep(100);
                }
            }
            catch(HttpListenerException) {
                Logger.Error("无法启动监听服务器，请确保使用管理员权限运行。否则无法监听！", $"{MethodBase.GetCurrentMethod().DeclaringType.Name}.{MethodBase.GetCurrentMethod().Name}");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// 立刻中断并释放连接<para>注意！断开后需要重新Create</para>
        /// </summary>
        public void Dispose()
        {
            Logger.Debug($"准备销毁反向WebSocket...", $"{MethodBase.GetCurrentMethod().DeclaringType.Name}.{MethodBase.GetCurrentMethod().Name}");
            try {
                Listener.Stop();
                WebSocket.Dispose();
                WebSocket.Abort();
                Api.Destroy();
                Logger.Info("已销毁反向WebSocket");
            } catch (Exception e) {
                Logger.Error("销毁反向WebSocket失败！\n" + e.Message, $"{MethodBase.GetCurrentMethod().DeclaringType.Name}.{MethodBase.GetCurrentMethod().Name}");
            }
        }

        #endregion

        #region --私有函数--

        private async void Feedback()
        {
            while (true)
            {
                HttpListenerContext Context = await Listener.GetContextAsync();
                if (Context.Request.IsWebSocketRequest)
                {
                    Logger.Info("收到来自 go-cqhttp 客户端的连接！连接已建立！");
                    HttpListenerWebSocketContext SocketContext = await Context.AcceptWebSocketAsync(null);
                    WebSocket = SocketContext.WebSocket;
                    IsConnect = true;
                    Api.Create(this);
                    while (WebSocket.State == WebSocketState.Open)
                    {
                        await GetEventAsync();
                    }
                }
            }
        }

        #endregion
    }
}
