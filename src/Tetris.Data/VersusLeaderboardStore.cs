using Microsoft.Data.Sqlite;
using Tetris.Core;

namespace Tetris.Data;

/// <summary>
/// 双人对战胜场榜持久化（SQLite）。与单人排行榜完全独立，使用单独的表 VersusMatches。
/// 所有数据库异常都被吞掉：RecordMatch 返回 bool，GetStats 返回全 0 统计。
/// </summary>
public sealed class VersusLeaderboardStore
{
    private readonly string _connectionString;

    public VersusLeaderboardStore(string dbPath)
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        EnsureSchema();
    }

    public bool RecordMatch(PlayerSide winner, int winnerScore, int loserScore)
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "INSERT INTO VersusMatches (Winner, WinnerScore, LoserScore, PlayedAt) " +
                "VALUES ($winner, $winnerScore, $loserScore, $playedAt);";
            command.Parameters.AddWithValue("$winner", winner == PlayerSide.Player1 ? 0 : 1);
            command.Parameters.AddWithValue("$winnerScore", winnerScore);
            command.Parameters.AddWithValue("$loserScore", loserScore);
            command.Parameters.AddWithValue("$playedAt", DateTime.UtcNow.ToString("o"));
            command.ExecuteNonQuery();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public VersusStats GetStats()
    {
        try
        {
            int p1Wins = 0, p2Wins = 0, total = 0;
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT Winner, COUNT(*) FROM VersusMatches GROUP BY Winner;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                int count = reader.GetInt32(1);
                total += count;
                if (reader.GetInt32(0) == 0)
                    p1Wins += count;
                else
                    p2Wins += count;
            }
            return new VersusStats(p1Wins, p2Wins, total);
        }
        catch
        {
            return new VersusStats(0, 0, 0);
        }
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
                "CREATE TABLE IF NOT EXISTS VersusMatches (" +
                "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "Winner INTEGER NOT NULL, " +
                "WinnerScore INTEGER NOT NULL, " +
                "LoserScore INTEGER NOT NULL, " +
                "PlayedAt TEXT NOT NULL);";
            command.ExecuteNonQuery();
        }
        catch
        {
            // 数据库不可用（如只读目录）时静默降级。
        }
    }
}
