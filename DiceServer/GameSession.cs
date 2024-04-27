using System.Numerics;

public class GameSession
{
    public string SessionId { get; set; }
    public List<Player> Players { get; set; }
    public GameState State { get; set; }
}