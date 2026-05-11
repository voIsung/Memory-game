namespace Memory_game_shared.Models
{
    public class PlayerSession
    {
        public string Token { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public bool IsOnline { get; set; }

    }
}
