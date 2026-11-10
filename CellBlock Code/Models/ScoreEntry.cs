namespace CellBlock_Code.Models;

public class ScoreEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "";
    public int Score { get; set; }
    public Difficulty Difficulty { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
}