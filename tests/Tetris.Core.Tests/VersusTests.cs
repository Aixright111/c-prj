using Tetris.Core;

namespace Tetris.Core.Tests;

public class VersusTests
{
    [Fact]
    public void AddGarbageLines_InsertsRowWithSingleGap()
    {
        var board = new Board();

        bool overflow = board.AddGarbageLines(1);

        Assert.False(overflow);
        int filled = 0;
        for (int x = 0; x < Board.Width; x++)
            if (board.GetCell(x, Board.Height - 1) == PieceType.Garbage)
                filled++;
        Assert.Equal(Board.Width - 1, filled);
    }

    [Fact]
    public void AddGarbageLines_ShiftsExistingContentUp()
    {
        var board = new Board();
        board.SetCell(5, 10, PieceType.I);

        board.AddGarbageLines(2);

        // 内容上移 2 行：(5,10) → (5,8)
        Assert.Equal(PieceType.I, board.GetCell(5, 8));
        Assert.Null(board.GetCell(5, 10));
    }

    [Fact]
    public void AddGarbageLines_TopRowOverflow_ReturnsTrue()
    {
        var board = new Board();
        board.SetCell(3, 0, PieceType.T);

        bool overflow = board.AddGarbageLines(1);

        Assert.True(overflow);
        // 顶部被挤出的格子应被丢弃
        Assert.Null(board.GetCell(3, 0));
    }

    [Fact]
    public void ApplyGarbage_ShiftsActivePieceUp()
    {
        var board = new Board();
        var game = new GameState(board, Tetromino.Create(PieceType.T), 4, 5, Tetromino.Create(PieceType.L));

        game.ApplyGarbage(1);

        Assert.Equal(4, game.CurrentY);
        Assert.False(game.IsGameOver);
    }

    [Fact]
    public void ApplyGarbage_PiecePushedAboveTop_CausesGameOver()
    {
        var board = new Board();
        var game = new GameState(board, Tetromino.Create(PieceType.T), 4, 0, Tetromino.Create(PieceType.L));

        game.ApplyGarbage(1);

        Assert.True(game.IsGameOver);
    }

    [Fact]
    public void ResolveAfter_TwoLineClear_SendsGarbageToOpponent()
    {
        // 玩家1 棋盘底部两行只留左下角 2×2 空位，用 O 方块硬降正好一次消两行
        var p1Board = new Board();
        for (int y = Board.Height - 2; y < Board.Height; y++)
            for (int x = 2; x < Board.Width; x++)
                p1Board.SetCell(x, y, PieceType.Z);
        var p1 = new GameState(p1Board, Tetromino.Create(PieceType.O), 0, Board.Height - 2, Tetromino.Create(PieceType.I));

        var p2 = new GameState(new Board(), Tetromino.Create(PieceType.T), 4, 5, Tetromino.Create(PieceType.L));
        var versus = new VersusGame(p1, p2);

        p1.HardDrop();
        Assert.Equal(2, p1.LinesCleared);

        versus.ResolveAfter(PlayerSide.Player1);

        // 玩家2 底部应收到 1 行垃圾（Width-1 个填充格 + 1 个空位）
        int filled = 0;
        for (int x = 0; x < Board.Width; x++)
            if (p2.Board.GetCell(x, Board.Height - 1) is not null)
                filled++;
        Assert.Equal(Board.Width - 1, filled);
    }

    [Fact]
    public void ResolveAfter_SingleLineClear_SendsNoGarbage()
    {
        // 玩家1 只消一行：底部一行留 0~3 四个空位，用水平 I 方块硬降补齐
        var p1Board = new Board();
        for (int x = 4; x < Board.Width; x++)
            p1Board.SetCell(x, Board.Height - 1, PieceType.Z);
        var p1 = new GameState(p1Board, Tetromino.Create(PieceType.I), 0, Board.Height - 2, Tetromino.Create(PieceType.O));

        var p2 = new GameState(new Board(), Tetromino.Create(PieceType.T), 4, 5, Tetromino.Create(PieceType.L));
        var versus = new VersusGame(p1, p2);

        p1.HardDrop();
        Assert.Equal(1, p1.LinesCleared);

        versus.ResolveAfter(PlayerSide.Player1);

        // 玩家2 棋盘应保持全空
        for (int x = 0; x < Board.Width; x++)
            for (int y = 0; y < Board.Height; y++)
                Assert.Null(p2.Board.GetCell(x, y));
    }

    [Fact]
    public void ResolveAfter_OpponentTopsOut_DeterminesWinner()
    {
        // 玩家2 方块在顶部，收到垃圾行后被顶出 → 玩家1 获胜
        var p1 = new GameState(new Board(), Tetromino.Create(PieceType.T), 4, 5, Tetromino.Create(PieceType.L));
        var p2 = new GameState(new Board(), Tetromino.Create(PieceType.T), 4, 0, Tetromino.Create(PieceType.L));
        var versus = new VersusGame(p1, p2);

        p2.ApplyGarbage(1);
        versus.ResolveAfter(PlayerSide.Player2);

        Assert.Equal(PlayerSide.Player1, versus.Winner);
        Assert.True(versus.IsOver);
    }
}
