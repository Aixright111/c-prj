using Tetris.Core;

namespace Tetris.Core.Tests;

public class BoardTests
{
    [Fact]
    public void EmptyBoard_CanPlacePieceInside()
    {
        var board = new Board();
        Assert.True(board.CanPlace(Tetromino.Create(PieceType.T), 4, 0));
    }

    [Fact]
    public void CanPlace_FalseWhenOutOfLeftWall()
    {
        var board = new Board();
        Assert.False(board.CanPlace(Tetromino.Create(PieceType.I), -1, 0));
    }

    [Fact]
    public void CanPlace_FalseWhenOutOfRightWall()
    {
        var board = new Board();
        // I 方块宽 4，放在 x=9 时占列 9-12，超出 12 列（合法 0-11）
        Assert.False(board.CanPlace(Tetromino.Create(PieceType.I), 9, 0));
    }

    [Fact]
    public void CanPlace_FalseWhenOutOfBottom()
    {
        var board = new Board();
        // 竖直 I（旋转后列 2，行 0-3）放在 y=14 时占行 14-17，超出 16 行
        var verticalI = Tetromino.Create(PieceType.I).RotateClockwise();
        Assert.False(board.CanPlace(verticalI, 4, 14));
    }

    [Fact]
    public void CanPlace_FalseWhenOverlappingOccupiedCell()
    {
        var board = new Board();
        board.SetCell(4, 1, PieceType.I);
        // T 放在 (4,0) 时包含格子 (4,1)
        Assert.False(board.CanPlace(Tetromino.Create(PieceType.T), 4, 0));
    }

    [Fact]
    public void ClearFullLines_NoFullRows_ReturnsZero()
    {
        var board = new Board();
        Assert.Equal(0, board.ClearFullLines());
    }

    [Fact]
    public void ClearFullLines_ClearsSingleFullRow()
    {
        var board = new Board();
        for (int x = 0; x < Board.Width; x++)
            board.SetCell(x, 15, PieceType.I);

        Assert.Equal(1, board.ClearFullLines());
        for (int x = 0; x < Board.Width; x++)
            Assert.Null(board.GetCell(x, 15));
    }

    [Fact]
    public void ClearFullLines_ClearsMultipleRows()
    {
        var board = new Board();
        for (int x = 0; x < Board.Width; x++)
        {
            board.SetCell(x, 14, PieceType.I);
            board.SetCell(x, 15, PieceType.J);
        }

        Assert.Equal(2, board.ClearFullLines());
    }

    [Fact]
    public void ClearFullLines_CompactsRowsAbove()
    {
        var board = new Board();
        for (int x = 0; x < Board.Width; x++)
            board.SetCell(x, 15, PieceType.I);
        board.SetCell(0, 14, PieceType.T);

        Assert.Equal(1, board.ClearFullLines());

        Assert.Equal(PieceType.T, board.GetCell(0, 15));
        Assert.Null(board.GetCell(0, 14));
    }

    [Fact]
    public void Clone_ProducesIndependentBoard()
    {
        var board = new Board();
        board.SetCell(0, 0, PieceType.I);
        var clone = board.Clone();

        clone.SetCell(1, 1, PieceType.O);

        Assert.Equal(PieceType.I, board.GetCell(0, 0));
        Assert.Null(board.GetCell(1, 1));
        Assert.Equal(PieceType.O, clone.GetCell(1, 1));
    }
}
