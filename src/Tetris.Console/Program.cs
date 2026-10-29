using System.Diagnostics;
using System.Text;
using Spectre.Console;
using Tetris.Core;
using Tetris.Data;

namespace Tetris.Console;

public static class Program
{
    public static void Main()
    {
        try
        {
            while (true)
            {
                int mode = SelectMode();
                if (mode == 1)
                    RunGame();
                else if (mode == 2)
                    RunVersus();
                else if (mode == 3)
                    ShowLeaderboard();
                else
                    return; // Esc 退出
            }
        }
        catch (Exception ex)
        {
            // 兜底：任何未预期异常都不让控制台闪退，给出可见的错误信息
            try { AnsiConsole.Clear(); } catch { /* 非交互环境下清屏可能失败，忽略 */ }
            try { AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything); }
            catch { System.Console.WriteLine(ex); }
            System.Console.WriteLine();
            System.Console.WriteLine("按任意键退出…");
            System.Console.ReadKey(true);
        }
    }

    private static int SelectMode()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel(new Markup(
            "[bold]俄罗斯方块 TETRIS[/]\n\n" +
            "[yellow]1[/]  单人模式\n" +
            "[yellow]2[/]  双人对战\n" +
            "[yellow]3[/]  排行榜\n\n" +
            "[grey]按 1 / 2 / 3 选择，Esc 退出[/]"))
        {
            Header = new PanelHeader("主菜单", Justify.Center),
            Border = BoxBorder.Rounded,
        });

        while (true)
        {
            var key = System.Console.ReadKey(true).Key;
            switch (key)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1: return 1;
                case ConsoleKey.D2:
                case ConsoleKey.NumPad2: return 2;
                case ConsoleKey.D3:
                case ConsoleKey.NumPad3: return 3;
                case ConsoleKey.Escape: return -1;
            }
        }
    }

    private static string SelectIdentity()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Panel(new Markup(
            "[bold]选择身份[/]\n\n" +
            "[yellow]1[/]  游客模式\n" +
            "[yellow]2[/]  登录模式（输入昵称）\n\n" +
            "[grey]按 1 / 2 选择，Esc 返回[/]"))
        {
            Header = new PanelHeader("身份选择", Justify.Center),
            Border = BoxBorder.Rounded,
        });

        while (true)
        {
            var key = System.Console.ReadKey(true).Key;
            switch (key)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1: return "游客";
                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                {
                    var nickname = AnsiConsole.Ask<string>("请输入昵称：");
                    return string.IsNullOrWhiteSpace(nickname) ? "游客" : nickname.Trim();
                }
                case ConsoleKey.Escape: return "游客";
            }
        }
    }

    private static void ShowLeaderboard()
    {
        var store = new LeaderboardStore(Path.Combine(AppContext.BaseDirectory, "scores.db"));

        AnsiConsole.Clear();
        var sb = new StringBuilder();
        sb.Append("[bold]排行榜 (Top 10)：[/]\n");
        var top = store.Top(10);
        if (top.Count == 0)
            sb.Append("  [grey]暂无记录[/]\n");
        else
            for (int i = 0; i < top.Count; i++)
                sb.Append($"  {i + 1}. {EscapeName(top[i].Name)}  0x{top[i].Score:X}  (等级 {top[i].Level}，消行 {top[i].Lines})\n");
        sb.Append("\n按任意键返回菜单…");

        var panel = new Panel(new Markup(sb.ToString()))
        {
            Header = new PanelHeader("排行榜", Justify.Center),
            Border = BoxBorder.Rounded,
        };
        AnsiConsole.Write(panel);
        System.Console.ReadKey(true);
    }

    private static string EscapeName(string name) =>
        Markup.Escape(string.IsNullOrWhiteSpace(name) ? "游客" : name);

    private static void RunVersus()
    {
        var game = new VersusGame();
        bool quit = false;

        AnsiConsole.Live(new Markup(""))
            .Start(ctx =>
            {
                var stopwatch = Stopwatch.StartNew();
                long lastTick1 = stopwatch.ElapsedMilliseconds;
                long lastTick2 = stopwatch.ElapsedMilliseconds;

                while (!game.IsOver && !quit)
                {
                    quit = ProcessVersusInput(game);

                    long now = stopwatch.ElapsedMilliseconds;

                    if (game.Player1.IsPaused)
                        lastTick1 = now;
                    else if (now - lastTick1 >= game.Player1.DropIntervalMs)
                    {
                        game.Player1.Tick();
                        game.ResolveAfter(PlayerSide.Player1);
                        lastTick1 = now;
                    }

                    if (game.Player2.IsPaused)
                        lastTick2 = now;
                    else if (now - lastTick2 >= game.Player2.DropIntervalMs)
                    {
                        game.Player2.Tick();
                        game.ResolveAfter(PlayerSide.Player2);
                        lastTick2 = now;
                    }

                    ctx.UpdateTarget(GameRenderer.RenderVersus(game));
                    Thread.Sleep(10);
                }
            });

        AnsiConsole.Clear();
        var sb = new StringBuilder();
        sb.Append("[bold red]对局结束[/]\n\n");
        sb.Append($"玩家1 分数：0x{game.Player1.Score:X}\n");
        sb.Append($"玩家2 分数：0x{game.Player2.Score:X}\n");
        if (game.Winner is PlayerSide w)
            sb.Append($"\n[bold green]{(w == PlayerSide.Player1 ? "玩家1" : "玩家2")} 获胜！[/]\n");
        else
            sb.Append("\n[grey]已退出对局[/]\n");
        sb.Append("\n按任意键返回菜单…");

        var panel = new Panel(new Markup(sb.ToString()))
        {
            Header = new PanelHeader("双人对战 TETRIS", Justify.Center),
            Border = BoxBorder.Rounded,
        };
        AnsiConsole.Write(panel);
        System.Console.ReadKey(true);
    }

    private static bool ProcessVersusInput(VersusGame game)
    {
        while (System.Console.KeyAvailable)
        {
            var key = System.Console.ReadKey(true).Key;
            switch (key)
            {
                // 玩家1
                case ConsoleKey.LeftArrow: game.Player1.MoveLeft(); game.ResolveAfter(PlayerSide.Player1); break;
                case ConsoleKey.RightArrow: game.Player1.MoveRight(); game.ResolveAfter(PlayerSide.Player1); break;
                case ConsoleKey.DownArrow: game.Player1.SoftDrop(); game.ResolveAfter(PlayerSide.Player1); break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.X: game.Player1.RotateClockwise(); game.ResolveAfter(PlayerSide.Player1); break;
                case ConsoleKey.Z: game.Player1.RotateCounterClockwise(); game.ResolveAfter(PlayerSide.Player1); break;
                case ConsoleKey.Spacebar: game.Player1.HardDrop(); game.ResolveAfter(PlayerSide.Player1); break;

                // 玩家2
                case ConsoleKey.A: game.Player2.MoveLeft(); game.ResolveAfter(PlayerSide.Player2); break;
                case ConsoleKey.D: game.Player2.MoveRight(); game.ResolveAfter(PlayerSide.Player2); break;
                case ConsoleKey.S: game.Player2.SoftDrop(); game.ResolveAfter(PlayerSide.Player2); break;
                case ConsoleKey.W: game.Player2.RotateClockwise(); game.ResolveAfter(PlayerSide.Player2); break;
                case ConsoleKey.Q: game.Player2.RotateCounterClockwise(); game.ResolveAfter(PlayerSide.Player2); break;
                case ConsoleKey.E: game.Player2.HardDrop(); game.ResolveAfter(PlayerSide.Player2); break;

                // 全局
                case ConsoleKey.P:
                    game.Player1.TogglePause();
                    game.Player2.TogglePause();
                    break;
                case ConsoleKey.Escape: return true;
            }
        }
        return false;
    }

    private static void RunGame()
    {
        var store = new HighScoreStore(Path.Combine(AppContext.BaseDirectory, "highscore.xml"));
        var highScore = store.Load();
        var leaderboard = new LeaderboardStore(Path.Combine(AppContext.BaseDirectory, "scores.db"));

        var name = SelectIdentity();

        var game = new GameState();
        bool quit = false;

        AnsiConsole.Live(new Markup(""))
            .Start(ctx =>
            {
                var stopwatch = Stopwatch.StartNew();
                long lastTick = stopwatch.ElapsedMilliseconds;

                while (!game.IsGameOver && !quit)
                {
                    quit = ProcessInput(game);

                    long now = stopwatch.ElapsedMilliseconds;
                    if (game.IsPaused)
                    {
                        lastTick = now; // 暂停时冻结重力计时
                    }
                    else if (now - lastTick >= game.DropIntervalMs)
                    {
                        game.Tick();
                        lastTick = now;
                    }

                    ctx.UpdateTarget(GameRenderer.Render(game, highScore));
                    Thread.Sleep(10);
                }
            });

        bool newRecord = false;
        if (game.IsGameOver && (highScore is null || game.Score > highScore.Score))
        {
            highScore = new HighScore(game.Score, game.LinesCleared, game.Level, DateTime.Now);
            store.Save(highScore);
            newRecord = true;
        }

        if (game.IsGameOver && game.Score > 0)
            leaderboard.Add(new HighScore(game.Score, game.LinesCleared, game.Level, DateTime.Now, name));

        AnsiConsole.Clear();
        var sb = new StringBuilder();
        sb.Append("[bold red]游戏结束[/]\n\n");
        sb.Append($"昵称：{EscapeName(name)}\n");
        sb.Append($"分数(十六进制)：0x{game.Score:X}\n");
        sb.Append($"等级：{game.Level}\n");
        sb.Append($"消行数：{game.LinesCleared}\n");
        sb.Append($"最高分：{(highScore is null ? "暂无" : $"0x{highScore.Score:X}")}\n");
        if (newRecord)
            sb.Append("\n[green]新纪录！[/]\n");

        var top = leaderboard.Top(5);
        if (top.Count > 0)
        {
            sb.Append("\n[bold]排行榜 (Top 5)：[/]\n");
            for (int i = 0; i < top.Count; i++)
            {
                var s = top[i];
                sb.Append($"  {i + 1}. {EscapeName(s.Name)}  0x{s.Score:X}  (等级 {s.Level}，消行 {s.Lines})\n");
            }
        }
        sb.Append("\n按任意键返回菜单…");

        var panel = new Panel(new Markup(sb.ToString()))
        {
            Header = new PanelHeader("俄罗斯方块 TETRIS", Justify.Center),
            Border = BoxBorder.Rounded,
        };
        AnsiConsole.Write(panel);
        System.Console.ReadKey(true);
    }

    private static bool ProcessInput(GameState game)
    {
        while (System.Console.KeyAvailable)
        {
            var key = System.Console.ReadKey(true).Key;
            switch (key)
            {
                case ConsoleKey.LeftArrow: game.MoveLeft(); break;
                case ConsoleKey.RightArrow: game.MoveRight(); break;
                case ConsoleKey.DownArrow: game.SoftDrop(); break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.X: game.RotateClockwise(); break;
                case ConsoleKey.Z: game.RotateCounterClockwise(); break;
                case ConsoleKey.Spacebar: game.HardDrop(); break;
                case ConsoleKey.P: game.TogglePause(); break;
                case ConsoleKey.Escape:
                case ConsoleKey.Q: return true;
            }
        }
        return false;
    }
}
