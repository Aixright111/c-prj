namespace Tetris.Core;

/// <summary>一条成绩记录。用于 XML 高分持久化与 SQLite 排行榜。</summary>
public sealed class HighScore
{
    public string Name { get; set; } = "";
    public int Score { get; set; }
    public int Lines { get; set; }
    public int Level { get; set; }
    public DateTime AchievedAt { get; set; }

    public HighScore() { }

    public HighScore(int score, int lines, int level, DateTime achievedAt, string name = "")
    {
        Score = score;
        Lines = lines;
        Level = level;
        AchievedAt = achievedAt;
        Name = name;
    }
}
