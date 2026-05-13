using Memory_game.MVVM;

namespace Memory_game.ViewModel
{
    public class PlayerScoreViewModel : ViewModelBase
    {
        private int _score;

        public PlayerScoreViewModel(string playerId, bool isCurrentPlayer, int score)
        {
            PlayerId = playerId;
            IsCurrentPlayer = isCurrentPlayer;
            _score = score;
        }

        public string PlayerId { get; }

        public bool IsCurrentPlayer { get; }

        public string DisplayName => IsCurrentPlayer ? "Ty" : $"Gracz {ShortPlayerId}";

        public string ShortPlayerId => PlayerId.Length <= 6 ? PlayerId : PlayerId.Substring(0, 6);

        public int Score
        {
            get => _score;
            set
            {
                _score = value;
                OnPropertyChanged();
            }
        }
    }
}
