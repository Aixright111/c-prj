namespace Tetris.Core;

/// <summary>12 列 × 16 行的游戏棋盘。负责碰撞检测、方块锁定与消行。</summary>
public sealed class Board
{
    public const int Width = 12;
    public const int Height = 16;

    private readonly PieceType?[,] _cells = new PieceType?[Width, Height];

    public PieceType? GetCell(int x, int y) => _cells[x, y];

    internal void SetCell(int x, int y, PieceType? value) => _cells[x, y] = value;

    public bool IsInside(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public bool IsOccupied(int x, int y) => IsInside(x, y) && _cells[x, y] is not null;

    /// <summary>方块在 (originX, originY) 处能否放下（所有格子均在界内且为空）。</summary>
    public bool CanPlace(Tetromino piece, int originX, int originY)
    {
        foreach (var (cx, cy) in piece.Cells)
        {
            int x = originX + cx;
            int y = originY + cy;
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return false;
            if (_cells[x, y] is not null)
                return false;
        }
        return true;
    }

    public void Place(Tetromino piece, int originX, int originY)
    {
        foreach (var (cx, cy) in piece.Cells)
            _cells[originX + cx, originY + cy] = piece.Type;
    }

    /// <summary>消除所有满行并压实，返回消除的行数。</summary>
    public int ClearFullLines()
    {
        int write = Height - 1;
        int cleared = 0;
        for (int read = Height - 1; read >= 0; read--)
        {
            if (IsRowFull(read))
            {
                cleared++;
                continue;
            }
            if (write != read)
                CopyRow(read, write);
            write--;
        }
        for (int y = write; y >= 0; y--)
            ClearRow(y);
        return cleared;
    }

    /// <summary>
    /// 在棋盘底部插入 count 行垃圾行，并把已有内容整体上移 count 行。
    /// 每行垃圾行除一个随机空位外全部填满。返回顶部是否有内容被挤出棋盘。
    /// </summary>
    public bool AddGarbageLines(int count)
    {
        bool overflow = false;
        for (int y = 0; y < count; y++)
            for (int x = 0; x < Width; x++)
                if (_cells[x, y] is not null)
                    overflow = true;

        for (int y = count; y < Height; y++)
            CopyRow(y, y - count);

        for (int g = 0; g < count; g++)
        {
            int gap = System.Random.Shared.Next(Width);
            int row = Height - count + g;
            for (int x = 0; x < Width; x++)
                _cells[x, row] = x == gap ? null : PieceType.Garbage;
        }
        return overflow;
    }

    public Board Clone()
    {
        var clone = new Board();
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                clone._cells[x, y] = _cells[x, y];
        return clone;
    }

    private bool IsRowFull(int y)
    {
        for (int x = 0; x < Width; x++)
            if (_cells[x, y] is null)
                return false;
        return true;
    }

    private void CopyRow(int from, int to)
    {
        for (int x = 0; x < Width; x++)
            _cells[x, to] = _cells[x, from];
    }

    private void ClearRow(int y)
    {
        for (int x = 0; x < Width; x++)
            _cells[x, y] = null;
    }
}
