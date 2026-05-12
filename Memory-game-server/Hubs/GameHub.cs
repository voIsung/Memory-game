
using Memory_game_shared.Constants;
using Memory_game_shared.Models;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;

namespace Memory_game_server.Hubs
{
    public class GameHub : Hub
    {

        private static GameState _gameState = new GameState();
        private static List<PlayerSession> _sessions = new List<PlayerSession>();
        private static string _currentPlayerTurn = "";
        private static List<int> _currentlyFlippedCards = new List<int>();
        private static int _currentPlayerIndex = 0;

        public async Task CreateNewGame(GameSettings gameSettings, string playerToken)
        {
            _sessions.Clear();
            _sessions.Add(new PlayerSession
            {
                Token = playerToken,
                ConnectionId = Context.ConnectionId,
                IsOnline = true
            });

            _gameState.HostId = playerToken;

            _gameState.settings = gameSettings;
            _gameState.Scores.Clear();
            _gameState.Scores[playerToken] = 0;
        }


        public async Task JoinGame(string playerToken)
        {

            var existingSession = _sessions.FirstOrDefault(session => session.Token == playerToken);

            if(existingSession != null)
            {
                existingSession.ConnectionId = Context.ConnectionId;
                existingSession.IsOnline = true;

                await Clients.Caller.SendAsync(HubMethods.GameStarted, _gameState);
                await Clients.Caller.SendAsync(HubMethods.ChangeTurn, _currentPlayerTurn, _gameState.settings.TurnTimeSeconds);

                return;
            }

            if (!_sessions.Any(session => session.Token == playerToken))
            {
                _sessions.Add(new PlayerSession
                {
                    Token = playerToken,
                    ConnectionId = Context.ConnectionId,
                    IsOnline = true
                });

                _gameState.Scores[playerToken] = 0;
            }

            int maxPlayers = _gameState.settings.MaxPlayers;

            await Clients.All.SendAsync(HubMethods.WaitingForPlayers, _sessions.Count, maxPlayers);

            if (_sessions.Count == maxPlayers)
            {
                Random rng = new Random();
                _currentPlayerIndex = rng.Next(_sessions.Count);
                _currentPlayerTurn = _sessions[_currentPlayerIndex].Token;

                if (_gameState.settings.DeckZipData != null && _gameState.settings.DeckZipData.Length > 0)
                {
                    foreach (var session in _sessions)
                    {
                        if (session.Token != _gameState.HostId)
                        {
                            await Clients.Client(session.ConnectionId).SendAsync(
                                HubMethods.DeckPackage,
                                _gameState.settings.DeckName,
                                _gameState.settings.DeckZipData,
                                _gameState.settings.ImagePaths.Length);
                        }
                    }
                }

                GenerateBoard(_gameState);

                await Clients.All.SendAsync(HubMethods.GameStarted, _gameState);

                int turnTimeSeconds = _gameState.settings.TurnTimeSeconds;
                await Clients.All.SendAsync(HubMethods.ChangeTurn, _currentPlayerTurn, turnTimeSeconds);
            }
        }

        public async Task FlipCard(int cardId)
        {

            string callerToken = GetTokenByConnectionId(Context.ConnectionId);
            if (callerToken != _currentPlayerTurn)
                return;

            Card cardToFlip = _gameState.CardsOnBoard.FirstOrDefault(card => card.id == cardId);
            if (cardToFlip == null || cardToFlip.isFaceUp || cardToFlip.isMatched)
                return;

            cardToFlip.isFaceUp = true;
            _currentlyFlippedCards.Add(cardId);
            await Clients.All.SendAsync(HubMethods.FlipCard, cardId);

            if (_currentlyFlippedCards.Count == 2)
            {
                Card firstCard = _gameState.CardsOnBoard.First(card => card.id == _currentlyFlippedCards[0]);
                Card secondCard = _gameState.CardsOnBoard.First(card => card.id == _currentlyFlippedCards[1]);

                if (firstCard.pairId == secondCard.pairId)
                {
                    firstCard.isMatched = true;
                    secondCard.isMatched = true;

                    _gameState.Scores[_currentPlayerTurn] = _gameState.Scores.GetValueOrDefault(_currentPlayerTurn, 0) + 1;

                    await Clients.All.SendAsync(HubMethods.MatchFound, _currentlyFlippedCards, _currentPlayerTurn);
                    await CheckGameOver();
                }
                else
                {
                    await Task.Delay(1000);
                    firstCard.isFaceUp = false;
                    secondCard.isFaceUp = false;

                    await Clients.All.SendAsync(HubMethods.MatchFailed, _currentlyFlippedCards);
                    await PassTurnToNextOnlinePlayer();

                    
                }
                _currentlyFlippedCards.Clear();
            }
        }

        private async Task PassTurnToNextOnlinePlayer()
        {
            int startringIndex = _currentPlayerIndex;

            do
            {
                _currentPlayerIndex = (_currentPlayerIndex + 1) % _sessions.Count;
            } while (!_sessions[_currentPlayerIndex].IsOnline && _currentPlayerIndex !=  startringIndex);

            _currentPlayerTurn = _sessions[_currentPlayerIndex].Token;
            int turnTimeSeconds = _gameState.settings.TurnTimeSeconds;
            await Clients.All.SendAsync(HubMethods.ChangeTurn, _currentPlayerTurn, turnTimeSeconds);
        }



        private string GetTokenByConnectionId(string connectionId)
        {
            return _sessions.FirstOrDefault(s => s.ConnectionId == connectionId)?.Token ?? "";
        }

        private void GenerateBoard(GameState gameState)
        {
            _gameState.CardsOnBoard.Clear();
            int totalCards = gameState.settings.Rows * gameState.settings.Columns;
            int pairOfCards = totalCards / 2;

            List<Card> cardsToShuffle = new List<Card>();

            for(int i = 0; i < pairOfCards; i++)
            {
                string imagePath = gameState.settings.ImagePaths[i];

                cardsToShuffle.Add(new Card { pairId = i, imagePath = imagePath });
                cardsToShuffle.Add(new Card { pairId = i, imagePath = imagePath });
            }

            ShuffleCards(cardsToShuffle);

            for(int i = 0; i < cardsToShuffle.Count; i++)
            {
                Card card = cardsToShuffle[i];
                card.id = i;
                card.isFaceUp = false;
                card.isMatched = false;

                _gameState.CardsOnBoard.Add(card);
            }

        }

        private void ShuffleCards(List<Card> cardsToShuffle)
        {
            Random rng = new Random();
            int n = cardsToShuffle.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = cardsToShuffle[k];
                cardsToShuffle[k] = cardsToShuffle[n];
                cardsToShuffle[n] = value;
            }
        }

        private async Task CheckGameOver()
        {
            bool allMatched = _gameState.CardsOnBoard.All(card => card.isMatched);

            if (allMatched)
            {
                int maxScore = _gameState.Scores.Values.Max();
                var winners = _gameState.Scores.Where(s => s.Value == maxScore).Select(s => s.Key).ToList();

                foreach (var session in _sessions)
                {
                    string result;
                    if (winners.Count == 1 && winners[0] == session.Token)
                        result = "win";
                    else if (winners.Count > 1 && winners.Contains(session.Token))
                        result = "draw";
                    else
                        result = "loss";

                    await Clients.Client(session.ConnectionId).SendAsync(HubMethods.GameOver, result);
                }
            }
        }

        public async Task TurnTimeout()
        {
            string callerToken = GetTokenByConnectionId(Context.ConnectionId);

            if (callerToken!= _currentPlayerTurn)
                return;

            if (_currentlyFlippedCards.Count > 0)
            {
                foreach (int cardId in _currentlyFlippedCards)
                {
                    Card card = _gameState.CardsOnBoard.First(c => c.id == cardId);
                    card.isFaceUp = false;
                }

                await Clients.All.SendAsync(HubMethods.MatchFailed, _currentlyFlippedCards);
                _currentlyFlippedCards.Clear();
            }

            bool allMatched = _gameState.CardsOnBoard.All(card => card.isMatched);
            if (allMatched)
            {
                await CheckGameOver();
                return;
            }

            await PassTurnToNextOnlinePlayer();
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var diconnectedPlayerSession = _sessions.FirstOrDefault(s => s.ConnectionId == Context.ConnectionId);

            if(diconnectedPlayerSession != null)
            {
                diconnectedPlayerSession.IsOnline = false;
                bool wasHisTurn = (_currentPlayerTurn == diconnectedPlayerSession.Token);
                bool isHost = (diconnectedPlayerSession.Token == _gameState.HostId);

                if (isHost)
                {
                    await Clients.All.SendAsync(HubMethods.PlayerDisconnected);
                }
                else
                {
                    if (wasHisTurn)
                    {
                        if (_currentlyFlippedCards.Count > 0)
                        {
                            await Clients.All.SendAsync(HubMethods.MatchFailed, _currentlyFlippedCards);
                            _currentlyFlippedCards.Clear();
                        }

                        if (_currentPlayerIndex >= _sessions.Count)
                        {
                            _currentPlayerIndex = 0;
                        }
                        await PassTurnToNextOnlinePlayer();
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
