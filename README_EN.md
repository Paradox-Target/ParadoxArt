# Hearts of Iron IV: Blueprint Editor

The Blueprint Editor is designed to provide a powerful desktop application for the Iron Hearts IV player community, with the ultimate goal of a high-level visual scripting system that references proven solutions for game engines in the industry, abstracting complex in-game logic into draggable, connectable nodes that MOD developers can use to build and manage in-game content in a more intuitive, modular way.

[English](README_EN.md) \ [中文](README.md)

---

### Table of Contents

- [Features](#features)
- [Project Vision](#project-vision)
- [Screenshots](#screenshots)
- [Getting Started](#getting-started)
- [Technology Stack](#technology-stack)

---

### Features

- **Visual National Focus Tree Editor**: No more manual script editing! Design, layout, and connect your national focuses in an intuitive, node-based canvas.
- **Blueprint Mode (Future Goal)**: An advanced visual scripting system inspired by game engine's blueprints system, allowing you to create complex event chains, decisions, and AI logic without writing a single line of code.
- **Real-time Validation**: Instantly catch common errors like broken dependencies, duplicate IDs, and logical fallacies before you even launch the game.
- **Multi-Language Support**: Available in both English and Chinese to support the global modding community.
- **Document Management**: Built to understand your entire mod structure, including GFX and localization files, for a seamless editing experience.

---

### Project Vision

This project began with a simple goal: to eliminate the tedious and error-prone process of manually scripting National Focus Trees in Hearts of Iron IV.

- **Phase 1: The Focus Tree Editor**: The initial phase focuses on delivering a best-in-class visual editor for National Focus Trees, arguably one of the central parts of HOI4 modding.

- **Phase 2: The Blueprint Mode**: The ultimate vision is to expand this tool into a "Blueprint Mode"—a versatile visual scripting system. This mode will cover a wider range of game mechanics, including events, decisions, national spirits, and AI behavior. By abstracting complex game logic into draggable and connectable nodes, we aim to make deep, intricate modding accessible to everyone, from beginners to seasoned veterans.

---

### Screenshots



---

### Getting Started



---

### Technology Stack

- **Platform**：.NET 10
- **UI Framework**：Windows Presentation Foundation (WPF)
- **Core Architecture**：Model-View-ViewModel (MVVM)
- **Dependency**：
    - [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
    - [Fody](https://github.com/Fody/Fody)
    - [MethodTimer.Fody](https://github.com/Fody/MethodTimer)
    - [Microsoft.Extensions.DependencyInjection](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
    - [NLog](https://nlog-project.org/)
    - [ObservableCollections](https://github.com/Cysharp/ObservableCollections)
    - [ParadoxPower](https://github.com/textGamex/ParadoxPower)
    - [ParadoxPower.CSharpExtensions](https://github.com/textGamex/ParadoxPower)
    - [ZLinq](https://github.com/Cysharp/ZLinq)
