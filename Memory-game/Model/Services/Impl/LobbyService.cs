using Memory_game_shared.Constants;
using Memory_game_shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using Memory_game.Model.Services;

namespace Memory_game.Model.Services.Impl
{
    public class LobbyService : ILobbyService
    {

        HubConnection? connection;
        private readonly IDeckPackageService _deckPackageService;
        private Task _pendingDeckSyncTask = Task.CompletedTask;
        public event Action<GameState> OnGameStarted;
        public event Action<int> OnCardFlipped;
        public event Action<List<int>, string> OnMatchFound;
        public event Action<List<int>> OnMatchFailed;
        public event Action<string, int> OnTurnChanged;
        public event Action<string> OnGameOver;
        public event Action OnPlayerDisconnected;
        public event Action<int, int> OnWaitingForPlayers;
        public string PlayerToken { get; } = Guid.NewGuid().ToString();

        public string MyConnectionId => connection?.ConnectionId ?? "";

        public LobbyService(IDeckPackageService deckPackageService)
        {
            _deckPackageService = deckPackageService;
        }

        public async Task ConnectAsync(string serverAddress)
        {
            Debug.WriteLine("Trying to connect");
            try
            {
                connection = new HubConnectionBuilder()
                .WithUrl($"http://{serverAddress}/gamehub")
                .Build();

                HandleServerEvents();

                await connection.StartAsync();

                Debug.WriteLine($"Connection status: {connection.State}");
            }catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

        }

        private void HandleServerEvents()
        {
            connection.On<GameState>(HubMethods.GameStarted, async (gameState) =>
            {
                await _pendingDeckSyncTask;
                OnGameStarted?.Invoke(gameState);
            });

            connection.On<int>(HubMethods.FlipCard, (cardId) =>
            {
                OnCardFlipped?.Invoke(cardId);
            });

            connection.On<List<int>, string>(HubMethods.MatchFound, (cardIds, currentPlayerId) =>
            {
                OnMatchFound?.Invoke(cardIds, currentPlayerId);
            });

            connection.On<List<int>>(HubMethods.MatchFailed, (cardIds) =>
            {
                OnMatchFailed?.Invoke(cardIds);
            });

            connection.On<string, int>(HubMethods.ChangeTurn, (currentPlayerId, turnTimeSeconds) =>
            {
                OnTurnChanged?.Invoke(currentPlayerId, turnTimeSeconds);
            });

            connection.On<string>(HubMethods.GameOver, (result) =>
            {
                OnGameOver?.Invoke(result);
            });

            connection.On(HubMethods.PlayerDisconnected, () =>
            {
                OnPlayerDisconnected?.Invoke();
            });

            connection.On<int, int>(HubMethods.WaitingForPlayers, (currentCount, maxCount) =>
            {
                OnWaitingForPlayers?.Invoke(currentCount, maxCount);
            });

            connection.Closed += async (error) =>
            {
                OnPlayerDisconnected?.Invoke();
                await Task.CompletedTask;
            };

            connection.On<string, byte[], int>(HubMethods.DeckPackage, (deckName, deckZipData, expectedCardCount) =>
            {
                _pendingDeckSyncTask = Task.Run(() =>
                {
                    bool alreadyExists = _deckPackageService.DeckExistsAndMatches(deckName, expectedCardCount);

                    if (!alreadyExists)
                    {
                        _deckPackageService.SaveDeckZip(deckName, deckZipData, overwriteExisting: true);
                    }
                });
            });
        }

        public async Task JoinGameAsync()
        {
            if (connection == null)
                return;

            await connection.InvokeAsync(HubMethods.JoinGame, PlayerToken);
        }

        public async Task DisconnectAsync()
        {
            if(connection != null)
                await connection.StopAsync();
        }

        public async Task CreateNewGame(GameSettings gameSettings)
        {
            if(connection != null)
            await connection.InvokeAsync(HubMethods.CreateNewGame, gameSettings, PlayerToken);
        }

        public async Task SendFlipCardAsync(int cardId)
        {
            if(connection != null)
                await connection.InvokeAsync(HubMethods.FlipCard, cardId);
        }

        public async Task SendTurnTimeoutAsync()
        {
            if (connection != null)
                await connection.InvokeAsync(HubMethods.TurnTimeout);
        }
    }
}
