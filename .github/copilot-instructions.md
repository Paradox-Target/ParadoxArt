# Hoi4-BlueprintBuilder Copilot Instructions

## 项目概述

这是一个为《钢铁雄心IV》设计的可视化国策树编辑器，使用 Avalonia UI + MVVM 架构。核心目标是将 HOI4 MOD 制作中繁琐的脚本编写转化为直观的节点拖拽操作。

## 技术栈

- **.NET 10** + **Avalonia UI** + **FluentAvaloniaUI**
- **MVVM**: CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`)
- **DI**: Microsoft.Extensions.DependencyInjection + **Injectio** 自动注册
- **解析器**: ParadoxPower 用于解析 Paradox 脚本格式
- **遥测**: TelemetryService (封装 Microsoft.ApplicationInsights.TelemetryClient) 发送使用数据到 Application Insights
- **日志**: NLog 记录运行时日志

## 架构要点

### 依赖注入 (Injectio 自动注册)

使用 `[RegisterSingleton<T>]` 或 `[RegisterTransient<T>]` 特性自动注册服务，无需手动配置：

```csharp
[RegisterSingleton<LocalizationService>]
public sealed class LocalizationService { }

[RegisterTransient<FocusTreeEditorViewModel>]
public sealed partial class FocusTreeEditorViewModel : ObservableObject { }
```

通过 `App.Current.Services.GetRequiredService<T>()` 获取服务实例。

### 消息传递 (CommunityToolkit.Mvvm)

- **StrongReferenceMessenger**: 用于必须送达的消息（如 `SaveFocusTreeMessage`）
- **WeakReferenceMessenger**: 弱引用消息

消息定义在 `Messages/` 目录，通常为 `record` 类型。

### 核心领域模型

- **FocusNode** (`Models/Focus/FocusNode.cs`): 国策节点，包含位置、前置条件、互斥关系等
- **FocusPoint**: 国策在画布上的网格坐标
- **FocusType**: 普通国策 vs 共享国策 (`shared_focus`)

### 文件解析流程

`FocusNodeHelper.GetAllNodesFromAst()` 解析国策树文件，支持 `shared_focus` 链接的递归解析。使用 `ParadoxPower.CSharpExtensions.TextParser` 解析 Paradox 脚本。

## 代码规范

### 方法性能追踪

使用 `[Time("描述")]` 特性（MethodTimer.Fody）自动记录方法执行时间：

```csharp
[Time("解析国策树")]
public static (Dictionary<string, FocusNode> Nodes, ...) GetAllNodesFromAst(...) { }
```

日志输出到 `MethodTimeLogger`。

### 本地化

- 应用内文本: `Hoi4BlueprintBuilder.Localization` 项目的 `LangResources.resx`
- 游戏数据关键字: `Keywords.cs` 常量类

### 日志

使用 NLog，通过 `LogManager.GetCurrentClassLogger()` 获取 Logger。Release 模式输出到 `Logs/` 目录。

## 开发命令

```bash
# 构建
dotnet build

# 运行测试 (NUnit)
dotnet test

# 发布 Windows 版本
dotnet publish -r win-x64 -o .\publish\win-x64 .\Hoi4BlueprintBuilder.Windows\Hoi4BlueprintBuilder.Windows.csproj --self-contained true
```

## 项目结构

```
Hoi4BlueprintBuilder.Core/     # 核心逻辑和 UI
  ├── Models/Focus/            # 国策领域模型
  ├── Views/                   # Avalonia 视图 (.axaml + .axaml.cs)
  ├── ViewsModels/             # MVVM ViewModels
  ├── Services/                # 业务服务 (自动注册)
  ├── Messages/                # 跨组件消息
  └── Helpers/                 # 纯函数工具类

Hoi4BlueprintBuilder.Windows/  # Windows 平台入口
Hoi4BlueprintBuilder.UnitTests/ # NUnit 测试
  └── TestData/                # 测试数据文件
```

## 测试约定

- 测试框架: NUnit 4
- 测试数据放在 `TestData/` 目录，通过 `TestApp.TestDataDirectory` 访问
- 需要隔离环境时使用 `TestHelper.CreateUniqueTempDirectory()`
