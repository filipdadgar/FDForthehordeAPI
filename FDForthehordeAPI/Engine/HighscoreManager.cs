using System.Text.Json;
using FDForthehordeAPI.Models;

namespace FDForthehordeAPI.Engine;

public class HighscoreManager
{
    private readonly string _highscoreFilePath = "highscores.json";
    private List<Highscore>? _highscores;
    
    public HighscoreManager()
    {
        _highscores = LoadHighscores();
    }
    
    public List<Highscore>? LoadHighscores()
    {
        if (!File.Exists(_highscoreFilePath))
        {
            return new List<Highscore>();
        }
        
        string json = File.ReadAllText(_highscoreFilePath);
        return JsonSerializer.Deserialize<List<Highscore>>(json);
    }
    
    public void SaveHighscores()
    {
        string json = JsonSerializer.Serialize(_highscores);
        File.WriteAllText(_highscoreFilePath, json);
        // Console.WriteLine("Highscores saved to file in this directory: " + _highscoreFilePath);
    }
    
    public List<Highscore> GetHighscores()
    {
        return _highscores;
    }
    
    public void AddHighscore(Highscore highscore)
    {
        if (_highscores == null)
        {
            _highscores = new List<Highscore>();
        }

        if (_highscores.Count < 10 || highscore.TotalKills > _highscores.Min(h => h.TotalKills))
        {
            _highscores.Add(highscore);
            SortHighscores();
            if (_highscores.Count > 10)
            {
                _highscores = _highscores.Take(10).ToList();
            }
            SaveHighscores();
        }
    }
    
    public void SortHighscores()
    {
        if (_highscores != null)
        {
            _highscores = _highscores.OrderByDescending(h => h.TotalKills).ToList();
        }
    }
    
    public bool IsTop10Score(int score)
    {
        if (_highscores == null || _highscores.Count < 10)
        {
            return true;
        }
        return _highscores.Any(h => score > h.TotalKills);
    }
}