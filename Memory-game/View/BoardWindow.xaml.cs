using Memory_game.Model.Services;
using Memory_game.ViewModel;
using Memory_game_shared.Models;
using System.ComponentModel;
using System.Windows;

namespace Memory_game.View
{
    public partial class BoardWindow : Window
    {
        public BoardWindow(GameState gameState,
            string deckName,
            ICardDeckService deckService,
            IServerManager serverManager)
        {
            InitializeComponent();
            BoardWindowViewModel viewModel = new BoardWindowViewModel(gameState, deckName, deckService, App.SharedLobbyService, serverManager, App.LastServerService);
            DataContext = viewModel;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if(DataContext is  BoardWindowViewModel viewModel)
            {
                viewModel.Cleanup();
            }
            base.OnClosing(e);
        }
    }
}