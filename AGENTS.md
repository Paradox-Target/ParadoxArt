# 开发说明

实现每一个功能前必须去查询 Wiki, Wiki 在 `D:\Code\Project\Rid_CSharp\ParadoxEconomy\wiki` 文件夹下.

## 事实来源

- WIKI目录: `D:\Code\Project\Rid_CSharp\ParadoxEconomy\wiki`
- 游戏本体目录：`D:\SteamLibrary\steamapps\common\Hearts of Iron IV`
- 游戏脚本和定义文件是当前安装版本的数值与数据结构事实来源；Wiki 用于理解机制、术语和公式。
- 当 Wiki、代码注释和游戏文件不一致时，先确认游戏版本与 DLC 条件，再以当前游戏文件的实际行为为准，并在代码或测试中记录差异。
- 不修改游戏本体或用户 Mod 文件。它们只能作为输入和验证材料。
- 不把开发者机器上的绝对路径继续散落到业务代码中。路径应由设置或启动参数提供；示例默认值必须容易覆盖。

## C# 约定

- 目标框架为 `net10.0`，启用 nullable；nullable 警告视为错误。
- 遵循根目录 `.editorconfig` 和 `.csharpierrc`：4 空格缩进、CRLF、文件范围命名空间、显式大括号和现有命名规则。
- 优先使用项目已有依赖和扩展方法，包括 Microsoft DI、Injectio、NLog、ParadoxPower 与 ZLinq。引入新包前先证明标准库和现有依赖无法合理解决问题。
- 公共 API 和不直观的游戏规则使用简洁中文 XML 注释；普通代码不添加逐行叙述式注释。
- 日志使用结构化参数，不用字符串拼接。可恢复的数据缺失记录 `Warn`，导致结果不可信的缺失应快速失败并提供资源路径或键名。
- 不做与当前任务无关的大规模重构，尤其不要在新增一个游戏机制时顺手重写整个解析或模拟层。
