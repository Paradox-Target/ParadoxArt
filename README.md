# 《钢铁雄心IV》：蓝图编辑器

蓝图编辑器旨在为《钢铁雄心IV》玩家社区提供一款功能强大的桌面应用程序，其最终目标是一种高级的可视化脚本系统，参考行业内游戏引擎的成熟方案，将游戏内的复杂逻辑抽象为可拖拽、连接的节点，MOD开发者能够以更直观、模块化的方式构建和管理游戏内容。

[English](README_EN.md) \ [中文](README.md)

---

### 目录

- [功能特性](#功能特性)
- [项目愿景](#项目愿景)
- [软件截图](#软件截图)
- [快速上手](#快速上手)
- [技术栈](#技术栈)

---

### 功能特性

- **可视化国策树编辑器**：告别手动编写脚本！在直观的、基于节点的画布上设计、布局和连接您的国策。
- **蓝图模式（未来目标）**：一个受行业内游戏引擎蓝图模式启发的先进可视化脚本系统，让您无需编写一行代码即可创建复杂的事件链、决议和AI逻辑。
- **实时校验**：在您启动游戏之前，即时捕获常见的错误，如损坏的依赖关系、重复的ID和逻辑谬误。
- **多语言支持**：提供英语和中文两种语言，以支持全球的MOD制作者社群。
- **文件管理**：旨在理解您的整个MOD目录结构，包括GFX和本地化文件，以提供无缝的编辑体验。

---

### 项目愿景

这个项目始于一个简单的目标：消除在《钢铁雄心IV》中手动编写国策树的繁琐和易错过程。

- **第一阶段：国策树编辑器**：项目的初始阶段专注于为国策树（可以说是HOI4 MOD制作中最核心的部分之一）提供一个最佳的可视化编辑器。

- **第二阶段：蓝图模式**：最终的愿景是将此工具扩展为一个“蓝图模式”——一个多功能的可视化脚本系统。此模式将覆盖更广泛的游戏机制，包括事件、决议、国家精神和AI行为。通过将复杂的游戏逻辑抽象为可拖拽、可连接的节点，我们的目标是让深刻、复杂的MOD制作对从初学者到资深专家的每一个人都触手可及。

---

### 软件截图



---

### 快速上手



---

### 技术栈

- **平台**：.NET 10
- **UI框架**：Windows Presentation Foundation (WPF)
- **核心架构**：Model-View-ViewModel (MVVM)
- **依赖**：
    - [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
    - [Fody](https://github.com/Fody/Fody)
    - [MethodTimer.Fody](https://github.com/Fody/MethodTimer)
    - [Microsoft.Extensions.DependencyInjection](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
    - [NLog](https://nlog-project.org/)
    - [ObservableCollections](https://github.com/Cysharp/ObservableCollections)
    - [ParadoxPower](https://github.com/textGamex/ParadoxPower)
    - [ParadoxPower.CSharpExtensions](https://github.com/textGamex/ParadoxPower)
    - [ZLinq](https://github.com/Cysharp/ZLinq)
