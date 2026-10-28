using Tetris.Core;

namespace Tetris.Core.Tests;

public class GameStateTests
{
    [Fact]
    public void NewGame_HasInitialState()
    {
        var game = new GameState(42);
        Assert.False(game.IsGameOver);
        Assert.False(game.IsPaused);
        Assert.Equal(0, game.Score);
        Assert.Equal(0, game.LinesCleared);
        Assert.Equal(1, game.Level);
        Assert.Equal(800, game.DropIntervalMs);
    }

    [Fact]
    public void MoveLeft_ChangesPosition()
    {
        var game = new GameState(42);
        int x = game.CurrentX;
        Assert.Equal(MoveResult.Moved, game.MoveLeft());
        Assert.Equal(x - 1, game.CurrentX);
    }

    [Fact]
    public void MoveLeft_BlockedAtLeftWall()
    {
        var game = new GameState(42);
        while (game.MoveLeft() == MoveResult.Moved) { }
        Assert.Equal(MoveResult.Blocked, game.MoveLeft());
    }

    [Fact]
    public void MoveRight_BlockedAtRightWall()
    {
        var game = new GameState(42);
        while (game.MoveRight() == MoveResult.Moved) { }
        Assert.Equal(MoveResult.Blocked, game.MoveRight());
    }

    [Fact]
    public void RotateClockwise_SucceedsOnEmptyBoard()
    {
        var game = new GameState(42);
        int rotation = game.Current.Rotation;
        Assert.Equal(MoveResult.Moved, game.RotateClockwise());
        Assert.Equal((rotation + 1) % 4, game.Current.Rotation);
    }

    [Fact]
    public void SoftDrop_MovesDownOne()
    {
        var game = new GameState(42);
        int y = game.CurrentY;
        Assert.Equal(MoveResult.Moved, game.SoftDrop());
        Assert.Equal(y + 1, game.CurrentY);
    }

    [Fact]
    public void HardDrop_LocksPiece()
    {
        var game = new GameState(42);
        var result = game.HardDrop();

        Assert.Equal(MoveResult.Locked, result);
        Assert.False(IsEmpty(game.Board));
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 200)]
    [InlineData(3, 300)]
    [InlineData(4, 400)]
    public void ScoreForLines_Is100TimesLines(int lines, int expected)
    {
        Assert.Equal(expected, GameState.ScoreForLines(lines));
    }

    [Fact]
    public void ClearingOneLine_Adds100Points()
    {
        var board = new Board();
        for (int x = 0; x < Board.Width; x++)
            if (x < 4 || x > 7)
                board.SetCell(x, 15, PieceType.J);

        var game = new GameState(board, Tetromino.Create(PieceType.I), 4, 14, Tetromino.Create(PieceType.O));

        var result = game.HardDrop();

        Assert.Equal(MoveResult.Locked, result);
        Assert.Equal(100, game.Score);
        Assert.Equal(1, game.LinesCleared);
        Assert.False(game.IsGameOver);
    }

    [Fact]
    public void GameOver_WhenNewPieceCannotSpawn()
    {
        var board = new Board();
        // 填满顶部三行，但每行留出列 0，避免整行被消掉
        for (int y = 0; y <= 2; y++)
            for (int x = 0; x < Board.Width; x++)
                if (x != 0)
                    board.SetCell(x, y, PieceType.I);

        var game = new GameState(board, Tetromino.Create(PieceType.I), 4, 14, Tetromino.Create(PieceType.O));

        var result = game.HardDrop();

        Assert.Equal(MoveResult.GameOver, result);
        Assert.True(game.IsGameOver);
    }

    [Fact]
    public void Clone_ProducesIndependentState()
    {
        var game = new GameState(42);
        var clone = game.Clone();

        Assert.Equal(game.CurrentX, clone.CurrentX);
        Assert.Equal(game.CurrentY, clone.CurrentY);
        Assert.Equal(game.Current.Type, clone.Current.Type);

        clone.HardDrop();

        Assert.True(IsEmpty(game.Board));    // 原棋盘不受影响
        Assert.False(IsEmpty(clone.Board));  // 克隆棋盘已锁定方块
    }

    [Fact]
    public void Pause_BlocksActions()
    {
        var game = new GameState(42);
        game.TogglePause();
        Assert.True(game.IsPaused);

        Assert.Equal(MoveResult.Blocked, game.MoveLeft());
        Assert.Equal(MoveResult.Blocked, game.MoveRight());
        Assert.Equal(MoveResult.Blocked, game.RotateClockwise());
        Assert.Equal(MoveResult.Blocked, game.Tick());

        game.TogglePause();
        Assert.False(game.IsPaused);
    }

    private static bool IsEmpty(Board board)
    {
        for (int y = 0; y < Board.Height; y++)
            for (int x = 0; x < Board.Width; x++)
                if (board.GetCell(x, y) is not null)
                    return false;
        return true;
    }
}
