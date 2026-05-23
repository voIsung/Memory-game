using Memory_game.ViewModel;
using Memory_game.Model.Services;
using System.Windows;
using Memory_game_server.Services.Impl;
using Memory_game.Model.Services.Impl;
using System.ComponentModel;

namespace Memory_game.View
{
    public partial class BoardSetupWindow : Window
    {
        public BoardSetupWindow(INavigationService navigationService, ICardDeckService deckService)
        {
            InitializeComponent();
            BoardSetupViewModel viewModel = new BoardSetupViewModel(
                navigationService,
                deckService, 
                App.SharedLobbyService,
                App.BroadcastService,
                new ServerManagerImpl(),
                new DeckPackageService()
                );

            DataContext = viewModel;
            Owner = Application.Current.MainWindow;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if(DataContext is BoardSetupViewModel viewModel)
            {
                viewModel.CleanUp();
            }

            base.OnClosing(e);
        }

    }
}