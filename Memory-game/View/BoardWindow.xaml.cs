using Memory_game.Model.Services;
using Memory_game.ViewModel;
using Memory_game_shared.Models;
using System;
using System.Diagnostics;
using System.Windows;

namespace Memory_game.View
{
    public partial class BoardWindow : Window
    {
        public BoardWindow(GameState gameState, string deckName, ICardDeckService deckService, IServerManager? serverManager = null)
        {
            InitializeComponent();

            DataContext = new BoardWindowViewModel(
                gameState,
                deckName,
                deckService,
                App.SharedLobbyService,
                serverManager,
                App.LastServerService);
        }

        protected override async void OnClosed(EventArgs e)
        {
            BoardWindowViewModel? viewModel = DataContext as BoardWindowViewModel;

            DataContext = null;

            base.OnClosed(e);

            if (viewModel == null)
                return;

            try
            {
                await viewModel.Cleanup();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas sprzątania po grze: {ex.Message}");
            }
        }
    }
}
