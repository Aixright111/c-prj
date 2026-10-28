using Tetris.Core;
using Tetris.Data;

namespace Tetris.Core.Tests;

public class HighScoreStoreTests : IDisposable
{
    private readonly string _dir;

    public HighScoreStoreTests()
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
    public void Load_FileMissing_ReturnsNull()
    {
        var store = new HighScoreStore(PathOf("missing.xml"));
        Assert.Null(store.Load());
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var path = PathOf("score.xml");
        var store = new HighScoreStore(path);
        var score = new HighScore(0x1A2B, 12, 3, new DateTime(2026, 10, 28, 9, 30, 0));

        Assert.True(store.Save(score));

        var loaded = store.Load();
        Assert.NotNull(loaded);
        Assert.Equal(score.Score, loaded!.Score);
        Assert.Equal(score.Lines, loaded.Lines);
        Assert.Equal(score.Level, loaded.Level);
        Assert.Equal(score.AchievedAt, loaded.AchievedAt);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsNull()
    {
        var path = PathOf("corrupt.xml");
        File.WriteAllText(path, "这不是合法的 XML <<<>>>");

        var store = new HighScoreStore(path);
        Assert.Null(store.Load());
    }

    [Fact]
    public void Load_LockedFile_ReturnsNull()
    {
        var path = PathOf("locked.xml");
        var store = new HighScoreStore(path);
        Assert.True(store.Save(new HighScore(100, 1, 1, DateTime.Now)));

        // 用独占锁占用文件，模拟「文件被占用」场景
        using (var _ = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.Null(store.Load());
        }
    }

    [Fact]
    public void Save_ReadOnlyDirectory_ReturnsFalse()
    {
        var path = PathOf("ro.xml");
        var store = new HighScoreStore(path);
        Assert.True(store.Save(new HighScore(1, 1, 1, DateTime.Now)));

        // 将文件改为只读，随后保存应失败（FileShare.None 无法写入只读文件）
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try
        {
            Assert.False(store.Save(new HighScore(2, 2, 2, DateTime.Now)));
        }
        finally
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }

    [Fact]
    public void Save_NestedMissingDirectory_CreatesAndSucceeds()
    {
        var path = Path.Combine(_dir, "sub", "deep", "score.xml");
        var store = new HighScoreStore(path);

        Assert.True(store.Save(new HighScore(42, 3, 2, DateTime.Now)));
        Assert.True(File.Exists(path));
    }
}
