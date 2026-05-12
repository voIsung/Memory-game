using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memory_game.Model.Services.Impl
{
    public class PlayerTokenServiceImpl : IPlayerTokenService
    {
        public string PlayerToken { get; }

        public PlayerTokenServiceImpl()
        {
            // Uncomment for testing on the same device need 3 instane of this project in 3 different folders
            //string appDataFolder = AppDomain.CurrentDomain.BaseDirectory;

            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MemoryGame");

            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }

            string tokenFilePath = Path.Combine(appDataFolder, "player_token.txt");

            if (File.Exists(tokenFilePath)){
                PlayerToken = File.ReadAllText(tokenFilePath);
                Debug.WriteLine($"Wczytano token: {PlayerToken}");
            }
            else
            {
                PlayerToken = Guid.NewGuid().ToString();
                File.WriteAllText(tokenFilePath, PlayerToken);
                Debug.WriteLine($"Wygenerowano nowy token{PlayerToken}");
            }
        }
    }
}
