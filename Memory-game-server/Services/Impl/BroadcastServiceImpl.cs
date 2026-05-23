
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Memory_game_server.Services.Impl
{
    public class BroadcastServiceImpl : IBroadcastService
    {
        private UdpClient? udpClient;
        private bool running;
        private string _lobbyName = "Memory Game Lobby";

        public async Task StartBroadcastingAsync(int port = 5000)
        {
            udpClient = new UdpClient(port);
            udpClient.EnableBroadcast = true;
            running = true;

            while (running)
            {
                var message = Encoding.UTF8.GetBytes($"MEMORY_GAME_SERVER:{port}:{_lobbyName}");
                await udpClient.SendAsync(message, message.Length, new IPEndPoint(IPAddress.Broadcast, 7788));

                await Task.Delay(2000);
            }
        }

        public void StopBroadcasting()
        {
            running = false;
            udpClient?.Close();
            Debug.WriteLine("Stopping broadcasting");
        }

        public void SetLobbyName(string lobbyName)
        {
            _lobbyName = lobbyName;
        }
    }
}
