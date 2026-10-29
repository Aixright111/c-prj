namespace Tetris.Core;

/// <summary>
/// 双人对战编排器。持有两个独立的 <see cref="GameState"/>，
/// 负责把一方的消行转换为对另一方的垃圾行，并判定胜负。
/// </summary>
public sealed class VersusGame
{
    private int _lastLines1;
    private int _lastLines2;

    public GameState Player1 { get; }
    public GameState Player2 { get; }
    public PlayerSide? Winner { get; private set; }
    public bool IsOver => Winner is not null;

    public VersusGame(ulong? seed = null)
    {
        var baseSeed = seed ?? (ulong)DateTime.UtcNow.Ticks;
        Player1 = new GameState(baseSeed);
        Player2 = new GameState(baseSeed ^ 0x9E3779B97F4A7C15UL);
    }

    /// <summary>仅供测试：注入确定性的双方状态。</summary>
    internal VersusGame(GameState player1, GameState player2)
    {
        Player1 = player1;
        Player2 = player2;
        _lastLines1 = player1.LinesCleared;
        _lastLines2 = player2.LinesCleared;
    }

    /// <summary>
    /// 在某一方完成一次操作或重力下落后调用：
    /// 若该方本轮消行数增量 ≥ 2，则向另一方送 1 行垃圾；随后判定胜负。
    /// </summary>
    public void ResolveAfter(PlayerSide side)
    {
        if (side == PlayerSide.Player1)
        {
            int delta = Player1.LinesCleared - _lastLines1;
            _lastLines1 = Player1.LinesCleared;
            if (delta >= 2)
                Player2.ApplyGarbage(1);
        }
        else
        {
            int delta = Player2.LinesCleared - _lastLines2;
            _lastLines2 = Player2.LinesCleared;
            if (delta >= 2)
                Player1.ApplyGarbage(1);
        }

        if (Player1.IsGameOver)
            Winner = PlayerSide.Player2;
        else if (Player2.IsGameOver)
            Winner = PlayerSide.Player1;
    }
}
