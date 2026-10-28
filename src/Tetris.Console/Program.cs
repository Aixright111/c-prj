using System.Diagnostics;
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
            RunGame();
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

    private static void RunGame()
    {
        var store = new HighScoreStore(Path.Combine(AppContext.BaseDirectory, "highscore.xml"));
        var highScore = store.Load();

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

        AnsiConsole.Clear();
        var panel = new Panel(
            new Markup(
                $"[bold red]游戏结束[/]\n\n" +
                $"分数(十六进制)：0x{game.Score:X}\n" +
                $"等级：{game.Level}\n" +
                $"消行数：{game.LinesCleared}\n" +
                $"最高分：{(highScore is null ? "暂无" : $"0x{highScore.Score:X}")}\n" +
                (newRecord ? "\n[green]新纪录！[/]\n" : "") +
                "\n按任意键退出…"))
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
