using Tetris.Core;

namespace Tetris.Core.Tests;

public class TetrominoTests
{
    [Theory]
    [InlineData(PieceType.I, 4)]
    [InlineData(PieceType.J, 3)]
    [InlineData(PieceType.L, 3)]
    [InlineData(PieceType.O, 2)]
    [InlineData(PieceType.S, 3)]
    [InlineData(PieceType.T, 3)]
    [InlineData(PieceType.Z, 3)]
    public void EveryPiece_HasExpectedSizeAndFourCells(PieceType type, int expectedSize)
    {
        var piece = Tetromino.Create(type);
        Assert.Equal(expectedSize, piece.Size);
        Assert.Equal(4, piece.Cells.Count);
    }

    [Fact]
    public void RotateClockwise_FourTimes_ReturnsToOriginal()
    {
        var piece = Tetromino.Create(PieceType.T);
        var rotated = piece
            .RotateClockwise()
            .RotateClockwise()
            .RotateClockwise()
            .RotateClockwise();

        Assert.Equal(piece.Cells, rotated.Cells);
    }

    [Fact]
    public void TPiece_RotateClockwise_ProducesExpectedCells()
    {
        var t = Tetromino.Create(PieceType.T);
        var expected = new[] { (2, 1), (1, 0), (1, 1), (1, 2) };

        Assert.Equal(expected, t.RotateClockwise().Cells);
    }

    [Fact]
    public void OPiece_IsRotationInvariant()
    {
        var o = Tetromino.Create(PieceType.O);
        var rotated = o.RotateClockwise();

        AssertSameCells(o.Cells, rotated.Cells);
    }

    [Fact]
    public void IPiece_RotateClockwise_BecomesVertical()
    {
        var i = Tetromino.Create(PieceType.I);
        // 水平 I 位于第 1 行（列 0-3），旋转后应变为竖直（列 2，行 0-3）
        var expected = new[] { (2, 0), (2, 1), (2, 2), (2, 3) };

        AssertSameCells(expected, i.RotateClockwise().Cells);
    }

    [Fact]
    public void RotateCounterClockwise_IsInverseOfClockwise()
    {
        var piece = Tetromino.Create(PieceType.L);
        var back = piece.RotateClockwise().RotateCounterClockwise();

        Assert.Equal(piece.Cells, back.Cells);
    }

    private static void AssertSameCells(
        IReadOnlyList<(int X, int Y)> expected,
        IReadOnlyList<(int X, int Y)> actual)
    {
        Assert.Equal(expected.OrderBy(c => c).ToArray(), actual.OrderBy(c => c).ToArray());
    }
}
