using Microsoft.Data.Sqlite;
using Tetris.Core;

namespace Tetris.Data;

/// <summary>
/// SQLite 排行榜持久化。与 HighScoreStore 一样，所有 IO / 数据库异常都被吞掉：
/// Add 返回 bool，Top 返回空列表，绝不向上抛异常导致程序崩溃。
/// </summary>
public sealed class LeaderboardStore
{
    private readonly string _connectionString;

    public LeaderboardStore(string dbPath)
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        EnsureSchema();
    }

    public bool Add(HighScore score)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "INSERT INTO HighScores (Name, Score, Lines, Level, AchievedAt) VALUES ($name, $score, $lines, $level, $achievedAt);";
            command.Parameters.AddWithValue("$name", score.Name);
            command.Parameters.AddWithValue("$score", score.Score);
            command.Parameters.AddWithValue("$lines", score.Lines);
            command.Parameters.AddWithValue("$level", score.Level);
            command.Parameters.AddWithValue("$achievedAt", score.AchievedAt.ToString("o"));
            command.ExecuteNonQuery();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IReadOnlyList<HighScore> Top(int count)
    {
        var result = new List<HighScore>();
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT Name, Score, Lines, Level, AchievedAt FROM HighScores ORDER BY Score DESC LIMIT $count;";
            command.Parameters.AddWithValue("$count", count);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new HighScore(
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3),
                    DateTime.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
                    reader.GetString(0)));
            }
        }
        catch
        {
            return Array.Empty<HighScore>();
        }
        return result;
    }

    private void EnsureSchema()
    {
        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(new SqliteConnectionStringBuilder(_connectionString).DataSource));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "CREATE TABLE IF NOT EXISTS HighScores (" +
                "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "Name TEXT NOT NULL DEFAULT '', " +
                "Score INTEGER NOT NULL, " +
                "Lines INTEGER NOT NULL, " +
                "Level INTEGER NOT NULL, " +
                "AchievedAt TEXT NOT NULL);";
            command.ExecuteNonQuery();

            // 迁移：旧版表无 Name 列，尝试补充（已存在则忽略）
            using var migrate = connection.CreateCommand();
            migrate.CommandText = "ALTER TABLE HighScores ADD COLUMN Name TEXT NOT NULL DEFAULT '';";
            try { migrate.ExecuteNonQuery(); } catch { /* 列已存在 */ }
        }
        catch
        {
            // 数据库不可用（如只读目录）时静默降级：Add/Top 都会返回失败/空。
        }
    }
}
