using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memory_game.Model.Services
{
    public interface ILastServerService
    {
        string GetLastServerAddress();
        void SaveLastServerAddress(string address);
        void ClearLastServerAddress();
    }
}
