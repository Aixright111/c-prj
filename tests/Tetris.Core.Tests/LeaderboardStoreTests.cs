using Tetris.Core;
using Tetris.Data;

namespace Tetris.Core.Tests;

public class LeaderboardStoreTests : IDisposable
{
    private readonly string _dir;

    public LeaderboardStoreTests()
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
    public void Top_EmptyDatabase_ReturnsEmpty()
    {
        var store = new LeaderboardStore(PathOf("empty.db"));
        Assert.Empty(store.Top(10));
    }

    [Fact]
    public void AddThenTop_ReturnsSortedByScoreDescending()
    {
        var store = new LeaderboardStore(PathOf("scores.db"));
        Assert.True(store.Add(new HighScore(100, 1, 1, DateTime.Now)));
        Assert.True(store.Add(new HighScore(500, 5, 2, DateTime.Now)));
        Assert.True(store.Add(new HighScore(300, 3, 2, DateTime.Now)));

        var top = store.Top(10);

        Assert.Equal(3, top.Count);
        Assert.Equal(500, top[0].Score);
        Assert.Equal(300, top[1].Score);
        Assert.Equal(100, top[2].Score);
    }

    [Fact]
    public void Top_RespectsCountLimit()
    {
        var store = new LeaderboardStore(PathOf("limit.db"));
        for (int i = 1; i <= 5; i++)
            store.Add(new HighScore(i * 100, i, 1, DateTime.Now));

        var top = store.Top(3);
        Assert.Equal(3, top.Count);
        Assert.Equal(500, top[0].Score);
    }

    [Fact]
    public void Top_RoundTripsAllFields()
    {
        var store = new LeaderboardStore(PathOf("fields.db"));
        var achieved = new DateTime(2026, 10, 28, 14, 5, 30, DateTimeKind.Utc);
        store.Add(new HighScore(0x2B4C, 12, 4, achieved, "小明"));

        var top = store.Top(1);
        var record = Assert.Single(top);

        Assert.Equal(0x2B4C, record.Score);
        Assert.Equal(12, record.Lines);
        Assert.Equal(4, record.Level);
        Assert.Equal(achieved, record.AchievedAt);
        Assert.Equal("小明", record.Name);
    }

    [Fact]
    public void Add_UnwritablePath_ReturnsFalse()
    {
        // 指向一个「文件」作为目录：SQLite 无法在此创建数据库文件，Add 应返回 false 而非抛异常
        var filePath = PathOf("notadir.db");
        File.WriteAllText(filePath, "占位");
        var store = new LeaderboardStore(Path.Combine(filePath, "sub.db"));

        Assert.False(store.Add(new HighScore(1, 1, 1, DateTime.Now)));
        Assert.Empty(store.Top(10));
    }
}
