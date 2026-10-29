using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using Tetris.Core;

namespace Tetris.Console;

/// <summary>把游戏状态渲染成 Spectre.Console 的可渲染对象。与逻辑完全解耦。</summary>
public static class GameRenderer
{
    private const string Block = "██";

    public static IRenderable Render(GameState game, HighScore? highScore = null)
    {
        var board = new Markup(RenderBoard(game));
        var info = new Panel(new Markup(RenderInfo(game, highScore)))
        {
            Header = new PanelHeader("信息"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 1),
        };

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow(board, info);

        return new Panel(grid)
        {
            Header = new PanelHeader("俄罗斯方块 TETRIS", Justify.Center),
            Border = BoxBorder.Rounded,
        };
    }

    public static IRenderable RenderVersus(VersusGame game)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow(
            RenderPlayerPanel(game.Player1, "玩家1"),
            RenderPlayerPanel(game.Player2, "玩家2"));

        return new Panel(grid)
        {
            Header = new PanelHeader("双人对战 TETRIS", Justify.Center),
            Border = BoxBorder.Rounded,
        };
    }

    private static Panel RenderPlayerPanel(GameState game, string label)
    {
        var sb = new StringBuilder();
        sb.Append(RenderBoard(game));
        sb.AppendLine();
        sb.AppendLine($"分数(十六进制)：0x{game.Score:X}");
        sb.AppendLine($"等级：{game.Level}");
        sb.AppendLine($"消行数：{game.LinesCleared}");
        sb.AppendLine();
        sb.AppendLine("下一个：");
        sb.Append(RenderPiece(game.Next));
        if (game.IsPaused)
            sb.AppendLine("[yellow]【已暂停】[/]");

        return new Panel(new Markup(sb.ToString()))
        {
            Header = new PanelHeader(label, Justify.Center),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 1),
        };
    }

    private static string RenderBoard(GameState game)
    {
        var board = game.Board;
        var overlay = new HashSet<(int X, int Y)>();
        if (!game.IsGameOver)
            foreach (var (cx, cy) in game.Current.Cells)
                overlay.Add((game.CurrentX + cx, game.CurrentY + cy));

        var sb = new StringBuilder();
        sb.Append('┌').Append(new string('─', Board.Width * 2)).AppendLine("┐");
        for (int y = 0; y < Board.Height; y++)
        {
            sb.Append('│');
            for (int x = 0; x < Board.Width; x++)
            {
                var cell = board.GetCell(x, y);
                if (cell is not null)
                    sb.Append(CellMarkup(cell.Value));
                else if (overlay.Contains((x, y)))
                    sb.Append(CellMarkup(game.Current.Type));
                else
                    sb.Append("  ");
            }
            sb.AppendLine("│");
        }
        sb.Append('└').Append(new string('─', Board.Width * 2)).AppendLine("┘");
        return sb.ToString();
    }

    private static string RenderInfo(GameState game, HighScore? highScore)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"分数(十六进制)：0x{game.Score:X}");
        sb.AppendLine($"最高分：{(highScore is null ? "暂无" : $"0x{highScore.Score:X}")}");
        sb.AppendLine($"等级：{game.Level}");
        sb.AppendLine($"消行数：{game.LinesCleared}");
        sb.AppendLine();
        sb.AppendLine("下一个：");
        sb.Append(RenderPiece(game.Next));
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("[grey]← → 移动    ↑/X 顺时针[/]");
        sb.AppendLine("[grey]↓ 软降     Z 逆时针[/]");
        sb.AppendLine("[grey]空格 硬降   P 暂停[/]");
        sb.AppendLine("[grey]Q/Esc 退出[/]");
        if (game.IsPaused)
            sb.AppendLine("[yellow]【已暂停】[/]");
        return sb.ToString();
    }

    private static string RenderPiece(Tetromino piece)
    {
        var cells = piece.Cells;
        int minX = cells.Min(c => c.X), maxX = cells.Max(c => c.X);
        int minY = cells.Min(c => c.Y), maxY = cells.Max(c => c.Y);
        var set = cells.ToHashSet();

        var sb = new StringBuilder();
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
                sb.Append(set.Contains((x, y)) ? CellMarkup(piece.Type) : "  ");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string CellMarkup(PieceType type) => $"[{ColorFor(type)}]{Block}[/]";

    private static string ColorFor(PieceType type) => type switch
    {
        PieceType.I => "#00d7d7",
        PieceType.J => "#005fff",
        PieceType.L => "#ff8700",
        PieceType.O => "#ffff00",
        PieceType.S => "#00d75f",
        PieceType.T => "#af00ff",
        PieceType.Z => "#ff0000",
        PieceType.Garbage => "#808080",
        _ => "#ffffff"
    };
}
