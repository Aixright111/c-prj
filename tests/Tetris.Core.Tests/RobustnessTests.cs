using Tetris.Core;

namespace Tetris.Core.Tests;

/// <summary>
/// 健壮性测试：模拟「快速 / 同时按键」「无效输入」等场景，
/// 证明无论操作序列如何，游戏状态都不会崩溃且始终合法。
/// </summary>
public class RobustnessTests
{
    [Fact]
    public void RandomOperations_NeverCrash_And_MaintainInvariants()
    {
        // 固定种子保证可复现：5000 次随机操作，任何一次抛异常都会让测试失败
        var game = new GameState(12345);
        var rng = new System.Random(6789);

        Action[] operations =
        {
            () => game.MoveLeft(),
            () => game.MoveRight(),
            () => game.SoftDrop(),
            () => game.RotateClockwise(),
            () => game.RotateCounterClockwise(),
            () => game.HardDrop(),
            () => game.Tick(),
            () => game.TogglePause(),
        };

        for (int i = 0; i < 5000; i++)
        {
            operations[rng.Next(operations.Length)]();
            AssertInvariants(game);
            if (game.IsGameOver)
                break;
        }
    }

    [Fact]
    public void SimultaneousMixedInput_DrainsWithoutCrash()
    {
        // 模拟一帧内同时涌入多个冲突按键（左+右、顺+逆、软降等），状态必须始终合法
        var game = new GameState(42);
        for (int i = 0; i < 1000; i++)
        {
            game.MoveLeft();
            game.MoveRight();
            game.SoftDrop();
            game.RotateClockwise();
            game.RotateCounterClockwise();
            AssertInvariants(game);
        }
    }

    [Fact]
    public void OperationsAfterGameOver_AreSafeNoOps()
    {
        // 游戏结束后继续输入，所有操作都应安全地返回 Blocked，而不是抛异常
        var board = new Board();
        for (int y = 0; y <= 2; y++)
            for (int x = 0; x < Board.Width; x++)
                if (x != 0)
                    board.SetCell(x, y, PieceType.I);

        var game = new GameState(board, Tetromino.Create(PieceType.I), 4, 14, Tetromino.Create(PieceType.O));
        game.HardDrop();
        Assert.True(game.IsGameOver);

        Assert.Equal(MoveResult.Blocked, game.MoveLeft());
        Assert.Equal(MoveResult.Blocked, game.MoveRight());
        Assert.Equal(MoveResult.Blocked, game.SoftDrop());
        Assert.Equal(MoveResult.Blocked, game.RotateClockwise());
        Assert.Equal(MoveResult.Blocked, game.RotateCounterClockwise());
        Assert.Equal(MoveResult.Blocked, game.HardDrop());
        Assert.Equal(MoveResult.Blocked, game.Tick());

        // 游戏结束后切换暂停应为空操作（不改变暂停状态）
        game.TogglePause();
        Assert.False(game.IsPaused);
    }

    private static void AssertInvariants(GameState game)
    {
        // 方块原点坐标必须在棋盘范围内（不会因移动/旋转/墙踢而越界）
        Assert.InRange(game.CurrentX, 0, Board.Width - 1);
        Assert.InRange(game.CurrentY, 0, Board.Height - 1);

        // 逐格读取棋盘，确保内部存储一致、无越界访问
        for (int x = 0; x < Board.Width; x++)
            for (int y = 0; y < Board.Height; y++)
                _ = game.Board.GetCell(x, y);
    }
}
