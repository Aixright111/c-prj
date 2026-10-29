using Spectre.Console.Testing;
using Tetris.Console;
using Tetris.Core;

namespace Tetris.Core.Tests;

public class GameRendererTests
{
    [Fact]
    public void Render_FreshGame_ContainsKeyInfo()
    {
        var console = new TestConsole();
        var game = new GameState(42);

        console.Write(GameRenderer.Render(game));

        Assert.Contains("分数(十六进制)：0x0", console.Output);
        Assert.Contains("等级：1", console.Output);
        Assert.Contains("消行数：0", console.Output);
        Assert.Contains("下一个", console.Output);
    }

    [Fact]
    public void Render_GameOverState_DoesNotThrow()
    {
        var console = new TestConsole();

        // 构造一个游戏结束状态：顶部三行几乎填满，锁定 I 后新方块无法生成
        var board = new Board();
        for (int y = 0; y <= 2; y++)
            for (int x = 0; x < Board.Width; x++)
                if (x != 0)
                    board.SetCell(x, y, PieceType.I);

        var game = new GameState(board, Tetromino.Create(PieceType.I), 4, 14, Tetromino.Create(PieceType.O));
        game.HardDrop();
        Assert.True(game.IsGameOver);

        var ex = Record.Exception(() => console.Write(GameRenderer.Render(game)));
        Assert.Null(ex);
    }

    [Fact]
    public void RenderVersus_FreshGame_ContainsBothPlayers()
    {
        var console = new TestConsole();
        var game = new VersusGame(42);

        console.Write(GameRenderer.RenderVersus(game));

        Assert.Contains("玩家1", console.Output);
        Assert.Contains("玩家2", console.Output);
    }
}
