using Memory_game.MVVM;
using Memory_game.Model.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using Memory_game.View;
using System.Windows;
using System.IO;
using System.Diagnostics;

namespace Memory_game.ViewModel
{
    public class CardDeckViewModel : ViewModelBase
    {
        private readonly ICardDeckService _deckService;
        private string _newDeckName = string.Empty;
        private string _selectedDeck = string.Empty;
        private string _errorMessage = string.Empty;
        private ObservableCollection<string> _availableDecks;
        private readonly INavigationService _navigationService;

        public RelayCommand CreateDeckCommand => new RelayCommand(execute => CreateDeck(), canExecute => true);
        public RelayCommand DeleteDeckCommand => new RelayCommand(execute => DeleteDeck(), canExecute => !string.IsNullOrEmpty(SelectedDeck));
        public RelayCommand AddCardsCommand => new RelayCommand(execute => AddCards(), canExecute => !string.IsNullOrEmpty(SelectedDeck));
        public RelayCommand CancelCommand => new RelayCommand(execute => Cancel(), canExecute => true);
        public RelayCommand SeeCardDeckPreviewCommand => new RelayCommand(execute => SeeCardDeckPreview(), canExecute => !string.IsNullOrEmpty(SelectedDeck));

        public CardDeckViewModel(INavigationService navigationService, ICardDeckService deckService)
        {
            _navigationService = navigationService;
            _deckService = deckService;
            _availableDecks = new ObservableCollection<string>(_deckService.GetAllDecks());

            if (_availableDecks.Contains(_navigationService.SelectedDeck))
                _selectedDeck = _navigationService.SelectedDeck;
            else
                _selectedDeck = _availableDecks.FirstOrDefault() ?? string.Empty;

            _navigationService.SelectedDeck = _selectedDeck;
        }

        public ObservableCollection<string> AvailableDecks
        {
            get => _availableDecks;
            set
            {
                _availableDecks = value;
                OnPropertyChanged();
            }
        }

        public string SelectedDeck
        {
            get => _selectedDeck;
            set
            {
                _selectedDeck = value ?? string.Empty;
                _navigationService.SelectedDeck = _selectedDeck;
                OnPropertyChanged();
            }
        }

        public string NewDeckName
        {
            get => _newDeckName;
            set
            {
                _newDeckName = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        private void CreateDeck()
        {
            string deckName = NewDeckName.Trim();

            if (string.IsNullOrWhiteSpace(deckName))
            {
                ErrorMessage = "Podaj nazwę talii.";
                return;
            }

            if (AvailableDecks.Any(deck => string.Equals(deck, deckName, StringComparison.OrdinalIgnoreCase)))
            {
                ErrorMessage = "Talia o tej nazwie już istnieje.";
                return;
            }

            try
            {
                _deckService.CreateDeck(deckName, Array.Empty<string>());
                AvailableDecks.Add(deckName);
                SelectedDeck = deckName;
                NewDeckName = string.Empty;
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private void DeleteDeck()
        {
            if (string.IsNullOrEmpty(SelectedDeck))
            {
                ErrorMessage = "Wybierz talię do usunięcia.";
                return;
            }

            if (SelectedDeck.StartsWith("DefaultDeck", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Nie można usunąć domyślnej talii.";
                return;
            }

            string deckToDelete = SelectedDeck;

            try
            {
                _deckService.DeleteDeck(deckToDelete);
                AvailableDecks.Remove(deckToDelete);

                SelectedDeck = AvailableDecks.FirstOrDefault(deck => deck.StartsWith("DefaultDeck", StringComparison.OrdinalIgnoreCase))
                               ?? AvailableDecks.FirstOrDefault()
                               ?? string.Empty;

                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private void AddCards()
        {
            if (string.IsNullOrEmpty(SelectedDeck))
                return;

            var dialog = new OpenFileDialog
            {
                Filter = "Obrazy (*.png)|*.png",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _deckService.AddCardsToDeck(SelectedDeck, dialog.FileNames);
                    ErrorMessage = string.Empty;
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                }
            }
        }

        private void Cancel()
        {
            Application.Current.Windows.OfType<CardDeckWindow>().FirstOrDefault()?.Close();
        }

        private void SeeCardDeckPreview()
        {
            if (string.IsNullOrWhiteSpace(SelectedDeck))
                return;

            string deckPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MemoryGame", "Decks", SelectedDeck);

            if (Directory.Exists(deckPath))
            {
                Process.Start("explorer.exe", deckPath);
            }
            else
            {
                ErrorMessage = "Wybrana talia nie istnieje na dysku.";
            }
        }
    }
}
