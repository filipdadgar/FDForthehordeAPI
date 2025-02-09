using System.Timers;
using FDForthehordeAPI.Models;
using Timer = System.Timers.Timer;
using Microsoft.Extensions.Logging; // Make sure to include logging

namespace FDForthehordeAPI.Engine;

public class Game
{
    private static GameState _gameState;
    private static Timer _gameTimer;
    private static Random _random = new Random();
    private static bool _gameLoopRunning = false;
    private static ILogger _logger; // Logger instance
    private static readonly object _gameStateLock = new object(); // Lock object for gameState

    // Constructor to inject ILoggerFactory
    public Game(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Game>();
    }

    internal GameState GetGameState()
    {
        lock (_gameStateLock)
        {
            _logger.LogInformation("Getting game state");
            if (_gameState == null)
            {
                InitializeGameData(); // Call InitializeGameData here when _gameState is null
            }
            return _gameState;
        }
    }

    // Separate InitializeGameData method
    private void InitializeGameData()
    {
        lock(_gameStateLock)
        {
            _logger.LogInformation("Initializing game data");
            _gameState = new GameState()
            {
                Soldier = new Soldier() { X = 290, Y = 500 },
                Chest = new Chest() { X = _random.Next(0, 300), Y = 50 },
                Hordes = new List<Horde>(), // Initialize Hordes list - CRITICAL FIX
                Bosses = new List<Boss>(),  // Initialize Bosses list - CRITICAL FIX
                IsGameOver = false,         // Ensure game is NOT over at start
                GameTime = TimeSpan.Zero,     // Reset game time
                HordeKills = 0,             // Reset scores
                BossKills = 0,
                Message = "",                // Clear messages
            };
        }
    }


    internal void StartGameLoop()
    {
        lock (_gameStateLock)
        {
            _logger.LogInformation("Starting game loop");
        

        if (_gameTimer == null && !_gameLoopRunning)
        {
            _gameTimer = new Timer(16); // ~60 frames per second
            _gameTimer.Elapsed += GameLoopTick;
            _gameTimer.AutoReset = true;
            _gameTimer.Enabled = true;
            _gameLoopRunning = true;
        }
        else if (_gameTimer != null && !_gameTimer.Enabled) // Restart if timer exists but is stopped
        {
            _gameTimer.Start();
            _gameLoopRunning = true; // Ensure flag is set when restarting
        }
        }
    }

    private void StopGameLoop() // Added StopGameLoop method
    {
        _logger.LogInformation("Stopping game loop");
        if (_gameTimer != null && _gameTimer.Enabled)
        {
            _gameTimer.Stop();
            _gameLoopRunning = false;
        }
    }


    private void GameLoopTick(object sender, ElapsedEventArgs e)
    {
        UpdateGameState();
    }

    private void UpdateGameState()
    {
        lock (_gameStateLock)
        {
            _logger.LogInformation($"Updating game state - Start - Hordes count: {_gameState.Hordes.Count}, Bosses count: {_gameState.Bosses.Count}");

            if (_gameState.IsGameOver) // <-- EARLY GAME OVER CHECK - Add this at the very beginning
            {
                _logger.LogInformation("UpdateGameState: Game Over already set, exiting update.");
                return; // Exit immediately if game is already over
            }

            MoveGameObjectsDown();
            GenerateEnemies();
            HandleSoldierAttacks();
            HandleChestInteraction();
            CheckGameOver();

            _logger.LogInformation($"Updating game state - End - Hordes count: {_gameState.Hordes.Count}, Bosses count: {_gameState.Bosses.Count}");

            _gameState.GameTime += TimeSpan.FromMilliseconds(_gameTimer.Interval);
        }
    }

    private void MoveGameObjectsDown()
    {
        lock (_gameStateLock) // <--- Add lock here
        {
            _logger.LogInformation("Moving game objects down");
            foreach (var horde in _gameState.Hordes)
            {
                _logger.LogInformation($"Moving horde down - Before: {horde.X}, {horde.Y}");
                horde.Y += horde.SpeedY;
                _logger.LogInformation($"Moving horde down - After: {horde.X}, {horde.Y}");
            }
            foreach (var boss in _gameState.Bosses)
            {
                _logger.LogInformation($"Moving boss down - Before: {boss.X}, {boss.Y}");
                boss.Y += boss.SpeedY;
                _logger.LogInformation($"Moving boss down - After: {boss.X}, {boss.Y}");
            }
            _gameState.Chest.Y += _gameState.Chest.SpeedY;
        } // <--- Lock ends here
    }

    private void GenerateEnemies()
    {
        lock (_gameStateLock) // <--- Add lock here
        {
            _logger.LogInformation("Generating enemies");
            if (_random.Next(100) < 5)
            {
                var newHorde = new Horde() { X = _random.Next(0, _gameState.ScreenWidth), Y = 0, SpeedY = 1 };
                _gameState.Hordes.Add(newHorde);
                _logger.LogInformation($"Horde generated at X: {newHorde.X}, Y: {newHorde.Y}");
            }
            if (_random.Next(1000) < 1)
            {
                var newBoss = new Boss() { X = _random.Next(0, _gameState.ScreenWidth), Y = 0, SpeedY = 0.5 };
                _gameState.Bosses.Add(newBoss);
                _logger.LogInformation($"Boss generated at X: {newBoss.X}, Y: {newBoss.Y}");
            }
        }
    }

    private void HandleSoldierAttacks()
    {
        lock (_gameStateLock)
        { 
            _logger.LogInformation("Handling soldier attacks");
            List<Horde> nextHordes = new List<Horde>(); // Create a *new* list for hordes to keep
            foreach (var horde in _gameState.Hordes)
            {
                // --- Attack condition (range check for X) ---
                if (Math.Abs(horde.X - _gameState.Soldier.X) <= 25 && horde.Y < _gameState.Soldier.Y && horde.Y > 0)
                {
                    _logger.LogInformation($"Horde IN ATTACK ZONE! Horde X:{horde.X}, Y:{horde.Y}, SoldierX:{_gameState.Soldier.X}, SoldierY:{_gameState.Soldier.Y}");
                    horde.HitPoints--;
                    _logger.LogInformation($"Horde hit, HP: {horde.HitPoints}, HordeID: {horde.Id}");
                    if (horde.HitPoints > 0) // Keep horde only if hitpoints > 0
                    {
                        nextHordes.Add(horde); // Add horde to the *new* list if it survives
                    }
                    else
                    {
                        _gameState.HordeKills++;
                        _logger.LogInformation($"Horde killed, Kills: {_gameState.HordeKills}, HordeID: {horde.Id}");
                    }
                }
                else
                {
                    nextHordes.Add(horde); // Add horde to the *new* list if it's not hit (or not in attack zone)
                    _logger.LogInformation($"Horde NOT in attack zone: X:{horde.X}, Y:{horde.Y}, SoldierX:{_gameState.Soldier.X}, SoldierY:{_gameState.Soldier.Y}");
                }
        
            }
            _gameState.Hordes = nextHordes; // Replace the *old* Hordes list with the *new* one

            // Boss attack logic (similar approach - create new Bosses list)
            List<Boss> nextBosses = new List<Boss>();
            foreach (var boss in _gameState.Bosses)
            {
                if (Math.Abs(boss.X - _gameState.Soldier.X) <= 25 && boss.Y < _gameState.Soldier.Y && boss.Y > 0)
                {
                    boss.HitPoints--;
                    if (boss.HitPoints > 0)
                    {
                        nextBosses.Add(boss);
                    }
                    else
                    {
                        _gameState.BossKills++;
                    }
                }
                else
                {
                    nextBosses.Add(boss);
                }
            }
            _gameState.Bosses = nextBosses; // Replace Bosses list too
        }
    }

    private void HandleChestInteraction()
    {
        lock(_gameStateLock)
        {
            _logger.LogInformation("Handling chest interaction");
            if (_gameState.Chest.Y > _gameState.Soldier.Y && _gameState.Chest.X == _gameState.Soldier.X && !_gameState.Chest.IsDestroyed)
            {
                _gameState.Chest.HitPoints--;
                _logger.LogInformation($"Chest hit, HP: {_gameState.Chest.HitPoints}");
                if (_gameState.Chest.HitPoints <= 0)
                {
                    _gameState.Chest.IsDestroyed = true;
                    AwardBonus();
                    _logger.LogInformation("Chest destroyed, bonus awarded");
                }
            }
            if (_gameState.Chest.Y > _gameState.ScreenHeight && !_gameState.Chest.IsDestroyed)
            {
                _gameState.Chest = new Chest() { X = _random.Next(0, _gameState.ScreenWidth), Y = 50, IsDestroyed = false, HitPoints = 5, Bonus = BonusType.None };
                _logger.LogInformation("Chest respawned");
            }
        }
    }

    private void AwardBonus()
    {
        BonusType bonus = (BonusType)_random.Next(1, Enum.GetValues(typeof(BonusType)).Length);
        _gameState.Chest.Bonus = bonus;
        switch (bonus)
        {
            case BonusType.MoreSoldiers:
                _gameState.Message = "Bonus: More Soldiers! (Feature not implemented in this example)";
                break;
            case BonusType.PowerfulWeapon:
                _gameState.Message = "Bonus: Powerful Weapon! (Feature not implemented in this example)";
                break;
            default:
                _gameState.Message = "Bonus received!";
                break;
        }
        Task.Delay(3000).ContinueWith(_ => _gameState.Message = "");
    }
    

    private void CheckGameOver()
    {
        lock(_gameStateLock)
        {
            _logger.LogInformation("Checking game over");
            foreach (var horde in _gameState.Hordes)
            {
                _logger.LogInformation($"Checking Horde Game Over Y:{horde.Y}, Soldier Y:{_gameState.Soldier.Y}");
                // --- REVISED GAME OVER CONDITION - USE >= ---
                if (horde.Y >= _gameState.Soldier.Y) // Changed > to >=
                {
                    _gameState.IsGameOver = true;
                    _gameState.Message = "Game Over! Hordes reached the soldier.";
                    _logger.LogWarning("Game Over triggered by Horde!");
                    StopGameLoop();
                    return; // Exit after game over
                }
            }
            foreach (var boss in _gameState.Bosses)
            {
                _logger.LogInformation($"Checking Boss Game Over Y:{boss.Y}, Soldier Y:{_gameState.Soldier.Y}");
                if (boss.Y >= _gameState.Soldier.Y) // Changed > to >= for bosses too
                {
                    _gameState.IsGameOver = true;
                    _gameState.Message = "Game Over! Boss reached the soldier.";
                    _logger.LogWarning("Game Over triggered by Boss!");
                    StopGameLoop();
                    return; // Exit after game over
                }
            }
        }
    }
}