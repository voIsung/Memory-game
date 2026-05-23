using Memory_game.Model.Services;
using Memory_game.Model.Services.Impl;
using Memory_game_server.Services;
using Memory_game_server.Services.Impl;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Memory_game
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IDeckPackageService DeckPackageService { get; } = new DeckPackageService();
        public static IPlayerTokenService PlayerTokenService { get; } = new PlayerTokenServiceImpl();
        public static ILobbyService SharedLobbyService { get; } = new LobbyService(DeckPackageService, PlayerTokenService);
        public static ILastServerService LastServerService { get; } = new LastServerServiceImpl();
        public static IBroadcastService BroadcastService { get; } = new BroadcastServiceImpl();

    }

}
