using Memory_game.Model.Services;
using Memory_game.MVVM;
using Memory_game.View;
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
        private int _opponentBestScore;
        private string _myPlayerId;
        private Dictionary<string, int> _allScores = new Dictionary<string, int>();
        private string _currentTurnText;
        private bool _isProcessingMove;
        private double _timeLeft;
        private int _turnTimeSeconds = 5;
        private DispatcherTimer? _turnTimer;

        private readonly ICardDeckService _deckService;
        private readonly ILobbyService _lobbyService;
        private readonly IServerManager _serverManager;

        public ObservableCollection<CardViewModel> Cards { get; set; } = new ObservableCollection<CardViewModel>();

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

        public int OpponentBestScore
        {
            get => _opponentBestScore;
            set
            {
                _opponentBestScore = value;
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
            IServerManager serverManager)
        {
            _rows = gameState.settings.Rows;
            _columns = gameState.settings.Columns;
            _myPlayerId = lobbyService.PlayerToken;
            _myScore = 0;
            _opponentBestScore = 0;
            CanInteract = true;
            _turnTimeSeconds = gameState.settings.TurnTimeSeconds;
            TimeLeft = _turnTimeSeconds;

            _deckService = deckService;
            _lobbyService = lobbyService;
            _serverManager = serverManager;

            _lobbyService.OnCardFlipped += HandleCardFlipped;
            _lobbyService.OnMatchFound += HandleCardsMatchFound;
            _lobbyService.OnMatchFailed += HandleCardsMatchFailed;
            _lobbyService.OnTurnChanged += HandleTurnChange;
            _lobbyService.OnGameOver += HandleGameOver;
            _lobbyService.OnPlayerDisconnected += HandlePlayerDisconnected;
            _lobbyService.OnWaitingForPlayers += HandleWaitingForPlayers;

            InitializeCards(gameState.CardsOnBoard ,deckName);
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

                Cards.Add(new CardViewModel(card.id, card.pairId, imagePath));
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

                UpdateOpponentBestScore();
            });
        }

        private void UpdateOpponentBestScore()
        {
            int bestOpponentScore = 0;
            foreach (var score in _allScores)
            {
                if (score.Key != _myPlayerId && score.Value > bestOpponentScore)
                {
                    bestOpponentScore = score.Value;
                }
            }
            OpponentBestScore = bestOpponentScore;
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
                    CurrentTurnText = "Twoja tura";
                    CanInteract = true;
                    StartTimer();
                }
                else
                {
                    CurrentTurnText = "Tura przeciwnika";
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

        private void HandleGameOver(string result)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
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

                MessageBoxResult mbResult = MessageBox.Show(
                    message + "\n\nKliknij OK, aby zamknąć grę.",
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

        private async Task FlipCard(CardViewModel card)
        {
            if (card != null && !card.IsFaceUp && !card.IsMatched)
            {
                await _lobbyService.SendFlipCardAsync(card.Id);
            }
        }

        private async void HandlePlayerDisconnected()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StopTimer();
                MessageBox.Show("Drugi gracz rozłączył się");
                
                foreach(Window window in Application.Current.Windows)
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
            StopTimer();
            _lobbyService.OnCardFlipped -= HandleCardFlipped;
            _lobbyService.OnMatchFound -= HandleCardsMatchFound;
            _lobbyService.OnMatchFailed -= HandleCardsMatchFailed;
            _lobbyService.OnTurnChanged -= HandleTurnChange;
            _lobbyService.OnGameOver -= HandleGameOver;
            _lobbyService.OnPlayerDisconnected -= HandlePlayerDisconnected;
            _lobbyService.OnWaitingForPlayers -= HandleWaitingForPlayers;

            await _lobbyService.DisconnectAsync();

            if (_serverManager != null)
            {
                await _serverManager.StopServerAsync();
            }
  
        }

    }
}