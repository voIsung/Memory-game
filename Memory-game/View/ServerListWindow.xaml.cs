using Memory_game.Model.Services;
using Memory_game.Model.Services.Impl;
using Memory_game.ViewModel;
using System.ComponentModel;
using System.Windows;

namespace Memory_game.View
{

    public partial class ServerListWindow : Window
    {
        public ServerListWindow()
        {
            InitializeComponent();
            ServerListWindowViewModel viewModel = new ServerListWindowViewModel(new ServerListener(), App.SharedLobbyService, new NavigationServiceImpl(), App.LastServerService);
            DataContext = viewModel;
            Owner = Application.Current.MainWindow;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (DataContext is ServerListWindowViewModel viewModel)
                viewModel.CleanUp();
            base.OnClosing(e);
        }

    }
}
