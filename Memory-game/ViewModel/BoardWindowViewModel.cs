using Memory_game.Model.Services;
using Memory_game.MVVM;
using Memory_game.View;
using Memory_game_server.Services;
using Memory_game_shared.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace Memory_game.ViewModel
{
    public class BoardWindowViewModel : ViewModelBase
    {
        private int _rows;
        private int _columns;
        private int _myScore;
        private string _myPlayerId;
        private Dictionary<string, int> _allScores = new Dictionary<string, int>();
        private Dictionary<string, string> _playerNames = new Dictionary<string, string>();
        private string _currentTurnText = string.Empty;
        private bool _isProcessingMove;
        private bool _hasHandledFatalDisconnect;
        private double _timeLeft;
        private int _turnTimeSeconds = 5;
        private DispatcherTimer? _turnTimer;
        private bool _isCleaningUp;

        private readonly ICardDeckService _deckService;
        private readonly ILobbyService _lobbyService;
        private readonly IServerManager _serverManager;
        private readonly ILastServerService _lastServerService;
        private readonly IBroadcastService _broadcastService;
        public ObservableCollection<CardViewModel> Cards { get; set; } = new ObservableCollection<CardViewModel>();
        public ObservableCollection<PlayerScoreViewModel> ScoreBoard { get; set; } = new ObservableCollection<PlayerScoreViewModel>();

        public RelayCommand FlipCardCommand => new RelayCommand(async execute => await FlipCard((CardViewModel)execute), canExecute => true);

        public int Rows
        {
            get => _rows;
            set
            {
                _rows = value;
                OnPropertyChanged();
            }
        }

        public int Columns
        {
            get => _columns;
            set
            {
                _columns = value;
                OnPropertyChanged();
            }
        }

        public int MyScore
        {
            get => _myScore;
            set
            {
                _myScore = value;
                OnPropertyChanged();
            }
        }

        public string CurrentTurnText
        {
            get => _currentTurnText;
            set
            {
                _currentTurnText = value;
                OnPropertyChanged();
            }
        }

        public double TimeLeft
        {
            get => _timeLeft;
            set
            {
                _timeLeft = value;
                OnPropertyChanged();
            }
        }

        public int TurnTimeSeconds
        {
            get => _turnTimeSeconds;
            set
            {
                _turnTimeSeconds = value;
                OnPropertyChanged();
            }
        }

        public bool CanInteract
        {
            get => !_isProcessingMove;
            set
            {
                _isProcessingMove = !value;
                OnPropertyChanged();
            }
        }

        public BoardWindowViewModel(GameState gameState, string deckName,
            ICardDeckService deckService,
            ILobbyService lobbyService,
            IServerManager serverManager,
            ILastServerService lastServerService)
        {
            _rows = gameState.settings.Rows;
            _columns = gameState.settings.Columns;
            _myPlayerId = lobbyService.PlayerToken;
            _allScores = new Dictionary<string, int>(gameState.Scores);
            _playerNames = new Dictionary<string, string>(gameState.PlayerNames);
            if (!_allScores.ContainsKey(_myPlayerId))
                _allScores[_myPlayerId] = 0;
            if (!_playerNames.ContainsKey(_myPlayerId))
                _playerNames[_myPlayerId] = GetShortPlayerId(_myPlayerId);
            _myScore = _allScores[_myPlayerId];
            CanInteract = true;
            _turnTimeSeconds = gameState.settings.TurnTimeSeconds;
            TimeLeft = _turnTimeSeconds;

            _deckService = deckService;
            _lobbyService = lobbyService;
            _serverManager = serverManager;
            _lastServerService = lastServerService;
            _broadcastService = App.BroadcastService;

            _lobbyService.OnCardFlipped += HandleCardFlipped;
            _lobbyService.OnMatchFound += HandleCardsMatchFound;
            _lobbyService.OnMatchFailed += HandleCardsMatchFailed;
            _lobbyService.OnTurnChanged += HandleTurnChange;
            _lobbyService.OnGameOver += HandleGameOver;
            _lobbyService.OnPlayerDisconnected += HandlePlayerDisconnected;
            _lobbyService.OnWaitingForPlayers += HandleWaitingForPlayers;

            InitializeCards(gameState.CardsOnBoard, deckName);
            InitializeScoreBoard();
        }

        private void InitializeCards(List<Card> cardsFromServer, string deckName)
        {
            string[] imageFiles = _deckService.GetCardsFromDeck(deckName);

            var imagePathsByPairId = new Dictionary<int, string>();
            for (int i = 0; i < imageFiles.Length; i++)
            {
                if (i < imageFiles.Length)
                {
                    imagePathsByPairId[i] = imageFiles[i];
                }
            }

            foreach (Card card in cardsFromServer)
            {
                string imagePath = imagePathsByPairId.ContainsKey(card.pairId) ? imagePathsByPairId[card.pairId] : string.Empty;

                var newCard = new CardViewModel(card.id, card.pairId, imagePath);
                newCard.IsFaceUp = card.isFaceUp;
                newCard.IsMatched = card.isMatched;

                Cards.Add(newCard);
            }
        }

        private void HandleCardFlipped(int cardId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var cardToFlip = Cards.FirstOrDefault(card => card.Id == cardId);
                if (cardToFlip != null)
                {
                    cardToFlip.IsFaceUp = true;
                }
            });
        }

        private void HandleCardsMatchFound(List<int> cardIds, string currentPlayerId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (int cardId in cardIds)
                {
                    CardViewModel? card = Cards.FirstOrDefault(card => card.Id == cardId);
                    if (card != null)
                        card.IsMatched = true;
                }

                if (!_allScores.ContainsKey(currentPlayerId))
                    _allScores[currentPlayerId] = 0;
                _allScores[currentPlayerId]++;

                if (currentPlayerId == _myPlayerId)
                {
                    MyScore = _allScores[currentPlayerId];
                    ResetTimer();
                }

                UpdateScoreBoard();
            });
        }

        private void InitializeScoreBoard()
        {
            ScoreBoard.Clear();

            foreach (var score in GetOrderedScores())
                ScoreBoard.Add(new PlayerScoreViewModel(GetPlayerName(score.Key), score.Key == _myPlayerId, score.Value));
        }

        private void UpdateScoreBoard()
        {
            ScoreBoard.Clear();

            foreach (var score in GetOrderedScores())
                ScoreBoard.Add(new PlayerScoreViewModel(GetPlayerName(score.Key), score.Key == _myPlayerId, score.Value));

            MyScore = _allScores.GetValueOrDefault(_myPlayerId, 0);
        }

        private IEnumerable<KeyValuePair<string, int>> GetOrderedScores()
        {
            return _allScores
                .OrderByDescending(score => score.Value)
                .ThenByDescending(score => score.Key == _myPlayerId)
                .ThenBy(score => score.Key);
        }

        private void HandleCardsMatchFailed(List<int> cardIds)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (int cardId in cardIds)
                {
                    CardViewModel? card = Cards.FirstOrDefault(card => card.Id == cardId);
                    if (card != null)
                        card.IsFaceUp = false;
                }
            });
        }

        private void HandleTurnChange(string currentPlayerId, int turnTimeSeconds)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StopTimer();

                TurnTimeSeconds = turnTimeSeconds;
                TimeLeft = TurnTimeSeconds;

                if (currentPlayerId == _lobbyService.PlayerToken)
                {
                    CurrentTurnText = $"Twoja tura: {GetPlayerName(currentPlayerId)}";
                    CanInteract = true;
                    StartTimer();
                }
                else
                {
                    CurrentTurnText = $"Tura gracza: {GetPlayerName(currentPlayerId)}";
                    CanInteract = false;
                }
            });
        }

        private void StartTimer()
        {
            TimeLeft = TurnTimeSeconds;
            _turnTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.1)
            };
            _turnTimer.Tick += OnTimerTick;
            _turnTimer.Start();
        }

        private void StopTimer()
        {
            _turnTimer?.Stop();
            _turnTimer = null;
        }

        private void ResetTimer()
        {
            StopTimer();
            TimeLeft = TurnTimeSeconds;
            StartTimer();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (Application.Current == null)
            {
                StopTimer();
                return;
            }

            TimeLeft -= 0.1;
            if (TimeLeft <= 0)
            {
                StopTimer();
                OnTurnTimeout();
            }
        }

        private async void OnTurnTimeout()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CanInteract = false;
                CurrentTurnText = "Czas minął!";
                foreach (var card in Cards.Where(c => c.IsFaceUp && !c.IsMatched))
                {
                    card.IsFaceUp = false;
                }
            });
            await _lobbyService.SendTurnTimeoutAsync();
        }

        private void HandleGameOver(string result, Dictionary<string, int> finalScores)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _lastServerService.ClearLastServerAddress();
                _allScores = new Dictionary<string, int>(finalScores);
                UpdateScoreBoard();

                string message;
                switch (result)
                {
                    case "win":
                        message = "Gratulacje! Wygrałeś!";
                        break;
                    case "loss":
                        message = "Niestety, przegrałeś. Spróbuj ponownie!";
                        break;
                    case "draw":
                        message = "Remis!";
                        break;
                    default:
                        message = "Koniec gry!";
                        break;
                }

                string scoreSummary = string.Join(
                    "\n",
                    GetOrderedScores().Select(score =>
                    {
                        string playerName = score.Key == _myPlayerId ? $"{GetPlayerName(score.Key)} (Ty)" : GetPlayerName(score.Key);
                        return $"{playerName}: {score.Value}";
                    }));

                MessageBoxResult mbResult = MessageBox.Show(
                    message + "\n\nWyniki:\n" + scoreSummary + "\n\nKliknij OK, aby zamknąć grę.",
                    "Koniec gry",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                if (mbResult == MessageBoxResult.OK)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window is BoardWindow)
                            {
                                window.Close();
                                break;
                            }
                        }
                    });
                }
            });
        }

        private string GetPlayerName(string playerId)
        {
            if (_playerNames.ContainsKey(playerId) && !string.IsNullOrWhiteSpace(_playerNames[playerId]))
                return _playerNames[playerId];

            return GetShortPlayerId(playerId);
        }

        private string GetShortPlayerId(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return string.Empty;

            return playerId.Length <= 8 ? playerId : playerId.Substring(0, 8);
        }

        private async Task FlipCard(CardViewModel card)
        {
            if (card != null && !card.IsFaceUp && !card.IsMatched)
            {
                await _lobbyService.SendFlipCardAsync(card.Id);
            }
        }

        private void HandlePlayerDisconnected(string reason)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (reason == Memory_game_shared.Constants.DisconnectReasons.PlayerDisconnected)
                {
                    MessageBox.Show("Jeden z graczy rozłączył się. Gra trwa dalej.");
                    return;
                }

                if (_hasHandledFatalDisconnect)
                    return;

                _hasHandledFatalDisconnect = true;
                StopTimer();
                CanInteract = false;
                _lastServerService.ClearLastServerAddress();

                string message = reason switch
                {
                    Memory_game_shared.Constants.DisconnectReasons.HostDisconnected => "Host rozłączył się. Gra została zakończona.",
                    Memory_game_shared.Constants.DisconnectReasons.NotEnoughPlayers => "Zostałeś sam w grze. Gra została zakończona.",
                    _ => "Utracono połączenie z serwerem. Gra została zakończona."
                };

                MessageBox.Show(message);

                foreach (Window window in Application.Current.Windows)
                {
                    if (window is BoardWindow boardWindow)
                    {
                        boardWindow.Close();
                        break;
                    }
                }

            });

        }
        private void HandleWaitingForPlayers(int currentCount, int maxCount)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentTurnText = $"Oczekiwanie na graczy... ({currentCount}/{maxCount})";
            });
        }

        public async Task Cleanup()
        {
            if (_isCleaningUp)
                return;

            _isCleaningUp = true;

            StopTimer();

            _lobbyService.OnCardFlipped -= HandleCardFlipped;
            _lobbyService.OnMatchFound -= HandleCardsMatchFound;
            _lobbyService.OnMatchFailed -= HandleCardsMatchFailed;
            _lobbyService.OnTurnChanged -= HandleTurnChange;
            _lobbyService.OnGameOver -= HandleGameOver;
            _lobbyService.OnPlayerDisconnected -= HandlePlayerDisconnected;
            _lobbyService.OnWaitingForPlayers -= HandleWaitingForPlayers;

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Cards.Clear();
                ScoreBoard.Clear();
            });

            try
            {
                await _lobbyService.DisconnectAsync();
            }
            catch
            {
                
            }

            if (_serverManager != null)
            {
                _lastServerService.ClearLastServerAddress();

                try
                {
                    _broadcastService.StopBroadcasting();
                    await _serverManager.StopServerAsync();
                }
                catch
                {

                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

    }
}
