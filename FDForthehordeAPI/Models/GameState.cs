// Models/GameState.cs
namespace FDForthehordeAPI.Models;

public class GameState
{
    public Soldier? Soldier { get; set; }
    public List<Horde> Hordes { get; set; } = new List<Horde>();
    public List<Boss> Bosses { get; set; } = new List<Boss>();
    public Chest Chest { get; set; }
    public int HordeKills { get; set; }
    public int BossKills { get; set; }
    public TimeSpan GameTime { get; set; }
    public bool IsGameOver { get; set; }
    public string Message { get; set; }
    public int ScreenWidth { get; set; } = 300; 
    public int ScreenHeight { get; set; } = 600;
    public List<Shot> Shots { get; set; } = new List<Shot>();
    
    public BonusType ActiveBonus { get; set; }
    
    public DateTime BonusEndTime { get; set; } // Track bonus expiration time

}

public class Soldier
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class Horde
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int SpeedY { get; set; } = 5; 
    public int HitPoints { get; set; } = 1;
}

public class Boss
{
    public int Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double SpeedY { get; set; } = 3; 
    public int HitPoints { get; set; } = 150; 
}

public class Chest
{
    public int X { get; set; }
    public double Y { get; set; }
    public double SpeedY { get; set; } = 2; 
  // public int HitPoints { get; set; } = 3; 
    public bool IsDestroyed { get; set; }
    public BonusType Bonus { get; set; }
}
    
public class MoveSoldierRequest
{
    public string Direction { get; set; } // Property to hold the direction string
}

public enum BonusType
{
    None = 0,
    PowerSoldier = 1,
    PowerfulWeapon = 2
    
}
    
public class Shot
{
    public int X { get; set; }
    public int Y { get; set; }
    public int SpeedY { get; set; } = -30;
}