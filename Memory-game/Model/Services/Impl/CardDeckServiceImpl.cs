using System.IO;

namespace Memory_game.Model.Services.Impl
{
    public class CardDeckServiceImpl : ICardDeckService
    {
        private readonly string _decksDirectory;
        private readonly string _defaultDeckDirectory;

        public CardDeckServiceImpl()
        {
            _decksDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MemoryGame", "Decks");
            _defaultDeckDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Cards");

            InitializeDecksDirectory();
        }

        private void InitializeDecksDirectory()
        {
            if (!Directory.Exists(_decksDirectory))
            {
                Directory.CreateDirectory(_decksDirectory);
            }

            if (!Directory.Exists(_defaultDeckDirectory))
            {
                return;
            }

            foreach (string sourceDeckPath in Directory.GetDirectories(_defaultDeckDirectory, "DefaultDeck*"))
            {
                string deckName = Path.GetFileName(sourceDeckPath);
                string targetDeckPath = Path.Combine(_decksDirectory, deckName);

                if (!Directory.Exists(targetDeckPath))
                {
                    Directory.CreateDirectory(targetDeckPath);
                }

                foreach (string file in Directory.GetFiles(sourceDeckPath, "*.png"))
                {
                    string targetFilePath = Path.Combine(targetDeckPath, Path.GetFileName(file));

                    if (!File.Exists(targetFilePath))
                    {
                        File.Copy(file, targetFilePath);
                    }
                }
            }
        }

        public IEnumerable<string> GetAllDecks()
        {
            return Directory.GetDirectories(_decksDirectory).Select(Path.GetFileName);
        }

        public string[] GetCardsFromDeck(string deckName)
        {
            string deckPath = Path.Combine(_decksDirectory, deckName);
            if (Directory.Exists(deckPath))
            {
                return Directory.GetFiles(deckPath, "*.png");
            }
            return Array.Empty<string>();
        }

        public void CreateDeck(string deckName, string[] imagePaths)
        {
            string deckPath = Path.Combine(_decksDirectory, deckName);
            if (!Directory.Exists(deckPath))
            {
                Directory.CreateDirectory(deckPath);
                foreach (string imagePath in imagePaths)
                {
                    string destPath = Path.Combine(deckPath, Path.GetFileName(imagePath));
                    File.Copy(imagePath, destPath, overwrite: true);
                }
            }
        }

        public void AddCardsToDeck(string deckName, string[] imagePaths)
        {
            string deckPath = Path.Combine(_decksDirectory, deckName);
            foreach (string file in imagePaths)
            {
                File.Copy(file, Path.Combine(deckPath, Path.GetFileName(file)), overwrite: true);
            }
        }

        public void DeleteDeck(string deckName)
        {
            string deckPath = Path.Combine(_decksDirectory, deckName);
            if (!Directory.Exists(deckPath))
                return;

            Exception? lastException = null;

            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Directory.Delete(deckPath, recursive: true);
                    return;
                }
                catch (IOException ex)
                {
                    lastException = ex;
                    Thread.Sleep(150);
                }
                catch (UnauthorizedAccessException ex)
                {
                    lastException = ex;
                    Thread.Sleep(150);
                }
            }

            throw new IOException("Nie można usunąć talii, ponieważ jej pliki są nadal używane. Zamknij planszę gry lub podgląd folderu i spróbuj ponownie.", lastException);
        }

        public int GetCardCount(string deckName)
        {
            string deckPath = Path.Combine(_decksDirectory, deckName);
            if (Directory.Exists(deckPath))
            {
                return Directory.GetFiles(deckPath, "*.png").Length;
            }
            return 0;
        }
    }
}