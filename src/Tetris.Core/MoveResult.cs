namespace Tetris.Core;

/// <summary>一次操作的结果。</summary>
public enum MoveResult
{
    /// <summary>操作被拒绝（撞墙 / 撞底 / 撞方块 / 游戏已结束 / 已暂停）。</summary>
    Blocked,

    /// <summary>方块发生了移动或旋转。</summary>
    Moved,

    /// <summary>方块已锁定到底部并触发了消行 / 生成下一块。</summary>
    Locked,

    /// <summary>本次操作后游戏结束（新方块无法生成）。</summary>
    GameOver
}
