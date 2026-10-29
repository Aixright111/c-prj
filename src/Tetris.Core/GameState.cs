namespace Tetris.Core;

/// <summary>
/// 完整的游戏状态，与界面渲染完全无关，可独立创建与复制。
/// 所有操作（移动、旋转、下落、消行）都通过本类的方法驱动。
/// </summary>
public sealed class GameState
{
    private Board _board;
    private Randomizer _rng;
    private Queue<PieceType> _bag = new();

    private Tetromino _current;
    private int _currentX;
    private int _currentY;
    private Tetromino _next;

    private int _score;
    private int _lines;
    private bool _gameOver;
    private bool _paused;

    public GameState(ulong? seed = null)
    {
        _rng = new Randomizer(seed ?? (ulong)DateTime.UtcNow.Ticks);
        _board = new Board();
        _current = CreatePiece(DrawPiece());
        _currentX = SpawnX(_current);
        _currentY = 0;
        _next = CreatePiece(DrawPiece());
    }

    /// <summary>仅供测试：用指定的棋盘与方块构造确定性的状态。</summary>
    internal GameState(Board board, Tetromino current, int currentX, int currentY, Tetromino next)
    {
        _rng = new Randomizer(1);
        _board = board;
        _current = current;
        _currentX = currentX;
        _currentY = currentY;
        _next = next;
        _bag = new Queue<PieceType>();
    }

    public Board Board => _board;
    public Tetromino Current => _current;
    public int CurrentX => _currentX;
    public int CurrentY => _currentY;
    public Tetromino Next => _next;

    public int Score => _score;
    public int Level => _lines / 10 + 1;
    public int LinesCleared => _lines;
    public bool IsGameOver => _gameOver;
    public bool IsPaused => _paused;

    /// <summary>当前等级对应的重力下落间隔（毫秒）。等级越高下落越快。</summary>
    public int DropIntervalMs => Math.Max(80, 800 - (Level - 1) * 70);

    /// <summary>一次消除 k 行得 100 × k 分。</summary>
    public static int ScoreForLines(int lines) => 100 * lines;

    public MoveResult MoveLeft() => TryMove(-1, 0);
    public MoveResult MoveRight() => TryMove(1, 0);
    public MoveResult SoftDrop() => TryMove(0, 1);

    public MoveResult RotateClockwise() => Rotate(clockwise: true);
    public MoveResult RotateCounterClockwise() => Rotate(clockwise: false);

    public MoveResult HardDrop()
    {
        if (_gameOver || _paused) return MoveResult.Blocked;
        while (_board.CanPlace(_current, _currentX, _currentY + 1))
            _currentY++;
        return LockPiece();
    }

    /// <summary>重力走一步：能下则下，否则锁定。</summary>
    public MoveResult Tick()
    {
        if (_gameOver || _paused) return MoveResult.Blocked;
        if (_board.CanPlace(_current, _currentX, _currentY + 1))
        {
            _currentY++;
            return MoveResult.Moved;
        }
        return LockPiece();
    }

    public void TogglePause()
    {
        if (!_gameOver)
            _paused = !_paused;
    }

    /// <summary>
    /// 双人对战中接收对方送来的垃圾行：棋盘底部插入 count 行，
    /// 活动方块随棋盘上移；若已有内容被挤出或活动方块被顶出顶部，则游戏结束。
    /// </summary>
    public void ApplyGarbage(int count)
    {
        if (_gameOver || count <= 0)
            return;

        bool overflow = _board.AddGarbageLines(count);
        _currentY -= count;

        if (overflow || !_board.CanPlace(_current, _currentX, _currentY))
            _gameOver = true;
    }

    /// <summary>深拷贝当前状态。克隆后与原状态相互独立。</summary>
    public GameState Clone()
    {
        var clone = (GameState)MemberwiseClone();
        clone._board = _board.Clone();
        clone._rng = _rng.Clone();
        clone._bag = new Queue<PieceType>(_bag);
        return clone;
    }

    private MoveResult TryMove(int dx, int dy)
    {
        if (_gameOver || _paused) return MoveResult.Blocked;
        if (_board.CanPlace(_current, _currentX + dx, _currentY + dy))
        {
            _currentX += dx;
            _currentY += dy;
            return MoveResult.Moved;
        }
        return MoveResult.Blocked;
    }

    private MoveResult Rotate(bool clockwise)
    {
        if (_gameOver || _paused) return MoveResult.Blocked;
        var rotated = clockwise ? _current.RotateClockwise() : _current.RotateCounterClockwise();

        // 简单的墙踢：依次尝试 0、左移1、右移1、左移2、右移2。
        foreach (int dx in new[] { 0, -1, 1, -2, 2 })
        {
            if (_board.CanPlace(rotated, _currentX + dx, _currentY))
            {
                _current = rotated;
                _currentX += dx;
                return MoveResult.Moved;
            }
        }
        return MoveResult.Blocked;
    }

    private MoveResult LockPiece()
    {
        _board.Place(_current, _currentX, _currentY);
        int cleared = _board.ClearFullLines();
        if (cleared > 0)
        {
            _score += ScoreForLines(cleared);
            _lines += cleared;
        }

        var next = _next;
        int nextX = SpawnX(next);
        if (_board.CanPlace(next, nextX, 0))
        {
            _current = next;
            _currentX = nextX;
            _currentY = 0;
            _next = CreatePiece(DrawPiece());
            return MoveResult.Locked;
        }

        _gameOver = true;
        return MoveResult.GameOver;
    }

    private PieceType DrawPiece()
    {
        if (_bag.Count == 0)
            RefillBag();
        return _bag.Dequeue();
    }

    private void RefillBag()
    {
        var bag = new List<PieceType>
        {
            PieceType.I, PieceType.J, PieceType.L,
            PieceType.O, PieceType.S, PieceType.T, PieceType.Z
        };
        _rng.Shuffle(bag);
        foreach (var p in bag)
            _bag.Enqueue(p);
    }

    private static Tetromino CreatePiece(PieceType type) => Tetromino.Create(type);

    private static int SpawnX(Tetromino piece) => (Board.Width - piece.Size) / 2;
}
