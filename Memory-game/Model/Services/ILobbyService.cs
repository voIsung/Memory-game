using Memory_game_shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memory_game.Model.Services
{
    public interface ILobbyService
    {
        public event Action<GameState> OnGameStarted;
        public event Action<int> OnCardFlipped;
        public event Action<List<int>, string> OnMatchFound;
        public event Action<List<int>> OnMatchFailed;
        public event Action<string, int> OnTurnChanged;
        public event Action<string, Dictionary<string, int>> OnGameOver;
        public event Action<string> OnPlayerDisconnected;
        public event Action<int, int> OnWaitingForPlayers;

        public string PlayerToken { get; }
        public string MyConnectionId { get; }
        public Task SendFlipCardAsync(int cardId);
        public Task CreateNewGame(GameSettings gameSettings);
        public Task ConnectAsync(string serverAddress);
        public Task JoinGameAsync();
        public Task DisconnectAsync();
        public Task SendTurnTimeoutAsync();

    }
}
