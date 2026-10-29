namespace Tetris.Core;

/// <summary>双人对战的胜场统计。</summary>
public sealed record VersusStats(int Player1Wins, int Player2Wins, int TotalMatches);
