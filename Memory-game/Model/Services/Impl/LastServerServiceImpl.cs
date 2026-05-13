using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memory_game.Model.Services.Impl
{
    public class LastServerServiceImpl : ILastServerService
    {
        private readonly string _filePath;

        public LastServerServiceImpl()
        {
            // Uncomment for testing on the same device need 3 instane of this project in 3 different folders
            //string appDataFolder = AppDomain.CurrentDomain.BaseDirectory;
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MemoryGame");

            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }
            
            _filePath = Path.Combine(appDataFolder, "last_server.txt");
        }
        public string GetLastServerAddress()
        {
            
            if (File.Exists(_filePath)) 
            {
                return File.ReadAllText(_filePath);
            }

            return string.Empty;
        }

        public void SaveLastServerAddress(string address)
        {
            File.WriteAllText(_filePath, address);
        }

        public void ClearLastServerAddress()
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
    }
}
