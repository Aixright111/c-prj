using Tetris.Core;
using Tetris.Data;

namespace Tetris.Core.Tests;

public class VersusLeaderboardStoreTests : IDisposable
{
    private readonly string _dir;

    public VersusLeaderboardStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "tetris-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* 忽略清理失败 */ }
    }

    private string PathOf(string name) => Path.Combine(_dir, name);

    [Fact]
    public void GetStats_EmptyDb_ReturnsZeros()
    {
        var store = new VersusLeaderboardStore(PathOf("empty.db"));
        var stats = store.GetStats();

        Assert.Equal(0, stats.Player1Wins);
        Assert.Equal(0, stats.Player2Wins);
        Assert.Equal(0, stats.TotalMatches);
    }

    [Fact]
    public void RecordMatches_GetStats_CountsWinsCorrectly()
    {
        var store = new VersusLeaderboardStore(PathOf("stats.db"));
        Assert.True(store.RecordMatch(PlayerSide.Player1, 500, 300));
        Assert.True(store.RecordMatch(PlayerSide.Player1, 400, 100));
        Assert.True(store.RecordMatch(PlayerSide.Player2, 700, 200));

        var stats = store.GetStats();

        Assert.Equal(2, stats.Player1Wins);
        Assert.Equal(1, stats.Player2Wins);
        Assert.Equal(3, stats.TotalMatches);
    }

    [Fact]
    public void RecordMatch_UnwritablePath_ReturnsFalse()
    {
        var filePath = PathOf("notadir.db");
        File.WriteAllText(filePath, "占位");
        var store = new VersusLeaderboardStore(Path.Combine(filePath, "sub.db"));

        Assert.False(store.RecordMatch(PlayerSide.Player1, 100, 50));
        Assert.Equal(0, store.GetStats().TotalMatches);
    }
}
