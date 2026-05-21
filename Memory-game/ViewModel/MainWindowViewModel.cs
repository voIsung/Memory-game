using System;
using System.IO;
using Memory_game.Model.Services;
using Memory_game.Model.Services.Impl;
using Memory_game.MVVM;

namespace Memory_game.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly INavigationService navigationService;
        private readonly ILobbyService lobbyService;
        private string _nickName = string.Empty;
        private bool _isLoadingNickName;

        public RelayCommand OpenBoardCommand => new RelayCommand(execute =>
        {
            SaveNickName();
            navigationService.OpenBoardSetup();
        }, canExecute => true);

        public RelayCommand OpenCardDeckWindowCommand => new RelayCommand(execute => navigationService.OpenCardDeckWindow(), canExecute => true);

        public RelayCommand OpenServerList => new RelayCommand(execute =>
        {
            SaveNickName();
            navigationService.OpenServerListWindow();
        }, canExecute => true);

        public string NickName
        {
            get => _nickName;
            set
            {
                var newNickName = value ?? string.Empty;

                if (newNickName.Length > 8)
                    newNickName = newNickName.Substring(0, 8);

                if (_nickName == newNickName)
                    return;

                _nickName = newNickName;
                OnPropertyChanged();

                if (!_isLoadingNickName)
                {
                    SaveNickNameToFile();
                    SaveNickName();
                }
            }
        }

        public MainWindowViewModel(INavigationService navigation, ILobbyService lobby)
        {
            navigationService = navigation;
            lobbyService = lobby;

            LoadNickNameFromFile();
            SaveNickName();
        }

        private void SaveNickName()
        {
            lobbyService.PlayerNickName = string.IsNullOrWhiteSpace(NickName) ? string.Empty : NickName.Trim();
        }

        private string GetNickNameFilePath()
        {
            var folderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MemoryGame");

            return Path.Combine(folderPath, "nick.txt");
        }

        private void LoadNickNameFromFile()
        {
            try
            {
                _isLoadingNickName = true;

                var filePath = GetNickNameFilePath();

                if (!File.Exists(filePath))
                    return;

                NickName = File.ReadAllText(filePath).Trim();
            }
            catch
            {
                NickName = string.Empty;
            }
            finally
            {
                _isLoadingNickName = false;
            }
        }

        private void SaveNickNameToFile()
        {
            try
            {
                var filePath = GetNickNameFilePath();
                var folderPath = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var nickNameToSave = string.IsNullOrWhiteSpace(NickName) ? string.Empty : NickName.Trim();
                File.WriteAllText(filePath, nickNameToSave);
            }
            catch
            {

            }
        }
    }
}
