using System.Xml.Serialization;
using Tetris.Core;

namespace Tetris.Data;

/// <summary>
/// 高分 XML 持久化。所有 IO 异常都被优雅处理：
/// 文件缺失 / 损坏 / 被占用 / 目录只读时，Load 返回 null、Save 返回 false，
/// 绝不向上抛异常导致程序崩溃。
/// </summary>
public sealed class HighScoreStore
{
    private readonly string _filePath;

    public HighScoreStore(string filePath)
    {
        _filePath = filePath;
    }

    public HighScore? Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return null;

            using var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var serializer = new XmlSerializer(typeof(HighScore));
            return serializer.Deserialize(stream) as HighScore;
        }
        catch
        {
            return null;
        }
    }

    public bool Save(HighScore score)
    {
        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(_filePath));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            using var stream = File.Open(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            var serializer = new XmlSerializer(typeof(HighScore));
            serializer.Serialize(stream, score);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
