namespace Tetris.Core;

/// <summary>
/// 可复制的确定性伪随机数生成器（xorshift64*）。
/// 相比 <see cref="System.Random"/>，它的状态是显式的、可被克隆，
/// 因此整局游戏状态（含随机序列）都能被完整复制。
/// </summary>
public sealed class Randomizer
{
    private ulong _state;

    public Randomizer(ulong seed)
    {
        _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
    }

    public ulong NextUInt64()
    {
        ulong x = _state;
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        _state = x;
        return x * 0x2545F4914F6CDD1DUL;
    }

    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        return (int)(NextUInt64() % (uint)maxExclusive);
    }

    /// <summary>Fisher–Yates 洗牌。</summary>
    public void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// 克隆随机数生成器。xorshift 状态一旦非零便恒非零，
    /// 因此可直接用当前状态构造出相同状态的副本。
    /// </summary>
    public Randomizer Clone() => new(_state);
}
