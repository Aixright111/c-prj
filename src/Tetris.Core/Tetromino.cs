namespace Tetris.Core;

/// <summary>
/// 一个方块（不可变）。保存形状类型、包围盒边长与当前旋转状态，
/// 通过坐标旋转公式按需计算出当前朝向的各个格子。
/// 旋转采用 (x, y) → (size-1-y, x) 的顺时针 90° 公式。
/// </summary>
public sealed class Tetromino
{
    public PieceType Type { get; }
    public int Size { get; }
    public int Rotation { get; }

    private readonly (int X, int Y)[] _baseCells;

    private Tetromino(PieceType type, int size, (int X, int Y)[] baseCells, int rotation)
    {
        Type = type;
        Size = size;
        _baseCells = baseCells;
        Rotation = rotation;
    }

    public static Tetromino Create(PieceType type, int rotation = 0)
    {
        int size = type == PieceType.I ? 4 : type == PieceType.O ? 2 : 3;
        return new Tetromino(type, size, BaseCells(type), ((rotation % 4) + 4) % 4);
    }

    /// <summary>当前朝向的各个格子（相对包围盒左上角的偏移）。</summary>
    public IReadOnlyList<(int X, int Y)> Cells
    {
        get
        {
            var result = new (int X, int Y)[_baseCells.Length];
            for (int i = 0; i < _baseCells.Length; i++)
                result[i] = RotateCell(_baseCells[i], Rotation);
            return result;
        }
    }

    public Tetromino RotateClockwise() => new(Type, Size, _baseCells, (Rotation + 1) % 4);

    public Tetromino RotateCounterClockwise() => new(Type, Size, _baseCells, (Rotation + 3) % 4);

    private (int X, int Y) RotateCell((int X, int Y) cell, int times)
    {
        var (x, y) = cell;
        for (int i = 0; i < times; i++)
            (x, y) = (Size - 1 - y, x);
        return (x, y);
    }

    private static (int X, int Y)[] BaseCells(PieceType type) => type switch
    {
        PieceType.I => new[] { (0, 1), (1, 1), (2, 1), (3, 1) },
        PieceType.O => new[] { (0, 0), (1, 0), (0, 1), (1, 1) },
        PieceType.T => new[] { (1, 0), (0, 1), (1, 1), (2, 1) },
        PieceType.S => new[] { (1, 0), (2, 0), (0, 1), (1, 1) },
        PieceType.Z => new[] { (0, 0), (1, 0), (1, 1), (2, 1) },
        PieceType.J => new[] { (0, 0), (0, 1), (1, 1), (2, 1) },
        PieceType.L => new[] { (2, 0), (0, 1), (1, 1), (2, 1) },
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
