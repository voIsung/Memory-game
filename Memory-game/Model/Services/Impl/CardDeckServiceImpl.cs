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
            Directory.CreateDirectory(_decksDirectory);

            if (!Directory.Exists(_defaultDeckDirectory))
                return;

            foreach (string sourceDeckPath in Directory.GetDirectories(_defaultDeckDirectory, "DefaultDeck*"))
            {
                string deckName = Path.GetFileName(sourceDeckPath);
                string targetDeckPath = Path.Combine(_decksDirectory, deckName);

                Directory.CreateDirectory(targetDeckPath);

                foreach (string file in Directory.GetFiles(sourceDeckPath, "*.png"))
                {
                    string targetFilePath = Path.Combine(targetDeckPath, Path.GetFileName(file));

                    if (!File.Exists(targetFilePath))
                        File.Copy(file, targetFilePath);
                }
            }
        }

        public IEnumerable<string> GetAllDecks()
        {
            Directory.CreateDirectory(_decksDirectory);

            return Directory.GetDirectories(_decksDirectory)
                .Select(Path.GetFileName)
                .Where(deckName => !string.IsNullOrWhiteSpace(deckName))!;
        }

        public string[] GetCardsFromDeck(string deckName)
        {
            if (string.IsNullOrWhiteSpace(deckName))
                return Array.Empty<string>();

            string deckPath = Path.Combine(_decksDirectory, deckName);

            if (!Directory.Exists(deckPath))
                return Array.Empty<string>();

            return Directory.GetFiles(deckPath, "*.png")
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public void CreateDeck(string deckName, string[] imagePaths)
        {
            if (string.IsNullOrWhiteSpace(deckName))
                throw new ArgumentException("Nazwa talii nie może być pusta.", nameof(deckName));

            string deckPath = Path.Combine(_decksDirectory, deckName.Trim());
            Directory.CreateDirectory(deckPath);

            foreach (string imagePath in imagePaths ?? Array.Empty<string>())
            {
                if (!File.Exists(imagePath))
                    continue;

                string destPath = Path.Combine(deckPath, Path.GetFileName(imagePath));
                File.Copy(imagePath, destPath, overwrite: true);
            }
        }

        public void AddCardsToDeck(string deckName, string[] imagePaths)
        {
            if (string.IsNullOrWhiteSpace(deckName))
                throw new ArgumentException("Nazwa talii nie może być pusta.", nameof(deckName));

            string deckPath = Path.Combine(_decksDirectory, deckName);
            Directory.CreateDirectory(deckPath);

            foreach (string file in imagePaths ?? Array.Empty<string>())
            {
                if (!File.Exists(file))
                    continue;

                File.Copy(file, Path.Combine(deckPath, Path.GetFileName(file)), overwrite: true);
            }
        }

        public void DeleteDeck(string deckName)
        {
            if (string.IsNullOrWhiteSpace(deckName))
                return;

            if (deckName.StartsWith("DefaultDeck", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Nie można usunąć domyślnej talii.");

            string deckPath = Path.Combine(_decksDirectory, deckName);

            if (!Directory.Exists(deckPath))
                return;

            try
            {
                foreach (string file in Directory.GetFiles(deckPath, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);

                Directory.Delete(deckPath, recursive: true);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException(
                    "Nie można usunąć tej talii, ponieważ jej pliki są teraz używane. Zamknij podgląd talii lub aktualną planszę i spróbuj ponownie.",
                    ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException(
                    "Nie można usunąć tej talii, ponieważ aplikacja nie ma dostępu do jednego z plików.",
                    ex);
            }
        }

        public int GetCardCount(string deckName)
        {
            return GetCardsFromDeck(deckName).Length;
        }
    }
}
