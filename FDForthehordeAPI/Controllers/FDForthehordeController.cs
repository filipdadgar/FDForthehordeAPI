using Microsoft.AspNetCore.Mvc;
using FDForthehordeAPI.Engine;
using FDForthehordeAPI.Models; // Namespace for your Game class

namespace FDForthehordeAPI.Controllers;

[ApiController]
[Route("[controller]")] // or [Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly Game _gameEngine; // Inject Game engine
    private readonly ILogger<GameController> _logger; // Logger for controller
    private readonly HighscoreManager _highscoreManager = new HighscoreManager(); // Inject HighscoreManager

    // Constructor to inject Game engine and ILogger
    public GameController(Game gameEngine, ILogger<GameController> logger)
    {
        _gameEngine = gameEngine;
        _logger = logger;
    }


    [HttpPost("start")]
    public ActionResult<GameState> StartNewGame()
    {
        // _logger.LogInformation("StartNewGame endpoint called");
        _gameEngine.StartGameLoop(); // Start game loop via engine
        return Ok(_gameEngine.GetGameState()); // Return initial game state
    }

    [HttpGet("state")]
    public ActionResult<GameState> GetGameState()
    {
        // _logger.LogInformation("GetGameState endpoint called");
        // return Ok(_gameEngine.GetGameState());
        var gameState = _gameEngine.GetGameState();
        bool isTop10 = _gameEngine.SaveHighScore();
        return Ok(new { gameState, isTop10 });
    }

    [HttpPost("soldier/move")]
    public ActionResult<GameState> MoveSoldier([FromBody] MoveSoldierRequest model)
    {
        if (model == null || string.IsNullOrEmpty(model.Direction))
        {
           // _logger.LogWarning("MoveSoldier: Invalid request model or direction is missing");
            return BadRequest("Invalid request. Direction is required.");
        }

        string direction = model.Direction.ToLower();

        // _logger.LogInformation("MoveSoldier endpoint called, direction: {Direction}", direction);
        GameState currentState = _gameEngine.GetGameState();

        if (currentState.IsGameOver) return BadRequest("Game Over");

        // _logger.LogInformation($"MoveSoldier - Soldier X before move: {currentState.Soldier.X}"); // <--- ADD THIS LOGGING

        int moveStep = 10;
        if (direction == "left")
        {
            currentState.Soldier.X -= moveStep;
        }
        else if (direction == "right")
        {
            currentState.Soldier.X += moveStep;
        }
        currentState.Soldier.X = Math.Max(0, Math.Min(currentState.ScreenWidth - 50, currentState.Soldier.X));

        // _logger.LogInformation("MoveSoldier - Soldier X after move: {SoldierX}", currentState.Soldier.X); // <--- ADD THIS LOGGING

        return Ok(currentState);
    }
    
    // In GameController.cs
    [HttpPost("stop")]
    public IActionResult StopGame()
    {
        _gameEngine.StopGameLoop();
        return Ok(new { message = "Game stopped" });
    }
    
    [HttpPost("highscore")]
    public IActionResult SaveHighscore([FromBody] Highscore highscore)
    {
        if (highscore == null || string.IsNullOrEmpty(highscore.PlayerName))
        {
            return BadRequest("Invalid highscore data.");
        }

        _highscoreManager.AddHighscore(highscore);
        _highscoreManager.SortHighscores();
        _highscoreManager.SaveHighscores();
        _logger.LogInformation("Highscore saved successfully.");

        return Ok(new { message = "Highscore saved successfully." });
    }
    
    [HttpGet("highscores")]
    public ActionResult<List<Highscore>> GetHighscores()
    {
        return Ok(_highscoreManager.GetHighscores());
    }
    
    
}