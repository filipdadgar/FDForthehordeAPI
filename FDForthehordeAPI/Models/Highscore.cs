namespace FDForthehordeAPI.Models;

public class Highscore
{
    public string PlayerName { get; set; }
    public string CreatedAt { get; set; }
    public int TotalKills { get; set; }

    public Highscore()
    {
        CreatedAt = DateTime.Now.ToString("yy/MM/dd_HH:mm");
    }
}