using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Memory_game.Model.Services.Impl
{
    public class ServerListener : IServerListener
    {
        public event Action<string, string>? ServerFound;
        private UdpClient? udpClient;
        private bool running;
        private static readonly int port = 7788;

        public async Task StartListeningAsync()
        {
            /*
             For testing on one pc delete port from constructor and uncommnet two commented lines
             */
            try
            {
                udpClient?.Close();
                udpClient?.Dispose();

                udpClient = new UdpClient(port);
                //udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                //udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                running = true;

                while (running)
                {
                    var result = await udpClient.ReceiveAsync();
                    var message = Encoding.UTF8.GetString(result.Buffer);

                    if (message.StartsWith("MEMORY_GAME_SERVER"))
                    {
                        var parts = message.Split(':');
                        var port = parts[1];
                        var serverIP = result.RemoteEndPoint.Address.ToString();
                        string lobbyName = parts.Length > 2 ? parts[2] : $"{serverIP}:{port}";
                        string address = $"{serverIP}:{port}";

                        ServerFound?.Invoke(lobbyName, address);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public void StopListening()
        {
            running = false;
            udpClient?.Close();
        }
    }
}
