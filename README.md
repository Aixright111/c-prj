# 俄罗斯方块（Tetris）

基于 **.NET 10** + **Spectre.Console** 的控制台俄罗斯方块游戏，作为 C# 课程大作业。

## 功能特性

- 12 列 × 16 行棋盘，七种方块（I/J/L/O/S/T/Z），支持左右移动、旋转（含简单墙踢）、软降、硬降
- 消行计分：一次消除 k 行得 `100 × k` 分，分数以**十六进制**显示
- 等级随消行数提升，下落速度逐级加快
- 暂停、游戏结束、下一个方块预览
- **双人对战**：两块棋盘并排、各自独立按键，一方一次消 2 行及以上向对方棋盘送入 1 行垃圾行，先被顶出棋盘者落败
- **高分持久化（XML）**：重启后读取历史最高分
- **排行榜（SQLite）**：游戏结束展示历史 Top 5，按昵称区分（游客 / 登录模式）
- **健壮性**：存档文件缺失 / 损坏 / 被占用 / 只读、窗口调整、快速 / 同时按键、无效输入均不会导致崩溃

## 环境要求

- .NET SDK 10.0（`global.json` 已锁定 `10.0.403`）
- Windows / Linux / macOS 任一支持 ANSI 的终端

## 构建与运行

```bash
# 还原依赖并构建
dotnet build Tetris.slnx

# 运行游戏
dotnet run --project src/Tetris.Console

# 运行单元测试
dotnet test tests/Tetris.Core.Tests/Tetris.Core.Tests.csproj
```

## 操作按键

启动后进入主菜单：按 `1` 单人模式、`2` 双人对战、`3` 排行榜、`Esc` 退出。

单人模式会先进入身份选择：`1` 游客模式（昵称显示为「游客」）、`2` 登录模式（输入昵称，用于区分排行榜）。

### 单人模式

| 按键 | 功能 |
| --- | --- |
| ← / → | 左右移动 |
| ↓ | 软降 |
| ↑ 或 X | 顺时针旋转 |
| Z | 逆时针旋转 |
| 空格 | 硬降（直接落底） |
| P | 暂停 / 继续 |
| Q 或 Esc | 退出游戏 |

### 双人对战

| 玩家1 按键 | 玩家2 按键 | 功能 |
| --- | --- | --- |
| ← / → | A / D | 左右移动 |
| ↓ | S | 软降 |
| ↑ 或 X | W | 顺时针旋转 |
| Z | Q | 逆时针旋转 |
| 空格 | E | 硬降（直接落底） |
| P | P | 暂停 / 继续 |
| Esc | Esc | 退出游戏 |

## 项目结构

```
Tetris.slnx
├─ src/
│  ├─ Tetris.Core/     核心逻辑（棋盘、方块、游戏状态、随机数），不依赖任何 UI
│  ├─ Tetris.Console/  控制台界面（Spectre.Console 渲染 + 游戏主循环）
│  └─ Tetris.Data/     数据持久化（XML 高分 + SQLite 排行榜）
└─ tests/
   └─ Tetris.Core.Tests/  单元测试（xUnit，65 个用例）
```

### 分层设计

- **Tetris.Core**：纯逻辑层，`GameState` 可独立创建、可 `Clone()` 深拷贝，与渲染完全解耦
- **Tetris.Console**：`GameRenderer` 把状态渲染成可渲染对象；`Program` 负责游戏主循环与输入轮询
- **Tetris.Data**：`HighScoreStore`（XML）与 `LeaderboardStore`（SQLite），所有 IO / 数据库异常均被吞掉并优雅降级

## 数据文件

运行后会在程序目录（`AppContext.BaseDirectory`）生成：

- `highscore.xml` —— 历史最高分
- `scores.db` —— SQLite 单人排行榜数据库

这些文件均已加入 `.gitignore`（`*.db` / `highscore.xml`），且任何读写失败都不会导致程序崩溃。
