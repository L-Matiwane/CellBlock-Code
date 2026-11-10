namespace CellBlock_Code.Models;

public record GuessResult(string Guess, int Hits, int Matches)
{
    public string Summary => $"{Hits} hit(s), {Matches} match(es)";
}