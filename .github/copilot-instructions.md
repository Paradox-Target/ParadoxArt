# Hoi4-BlueprintBuilder Copilot Instructions

跨平台程序, 支持 Windows 和 Linux 双端, 未来准备支持 Android 端.

## Code Style & Conventions

- **.NET 10 / C# 13**: Use strictly modern C# features (e.g. file-scoped namespaces).
- **MVVM Architecture**: Use CommunityToolkit.Mvvm, \[ObservableProperty], and \[RelayCommand] extensively to avoid boilerplate.
- **Dependency Injection**: Use Injectio attributes (\[RegisterSingleton<T>], \[RegisterTransient<T>]) for auto-registration instead of manual service configuration. Fetch via App.Current.Services.GetRequiredService<T>() only when necessary (prefer constructor injection).
- **Messaging**: Use StrongReferenceMessenger for guaranteed delivery (like SaveFocusTreeMessage) and WeakReferenceMessenger for decoupled broad events.
- **Localization**: Application text is in Hoi4BlueprintBuilder.Localization (LangResources.resx). Game keys/constants are in Keywords.cs.
- **Performance Profiling**: Use \[Time("description")] (MethodTimer.Fody) for automatic execution time tracking of critical methods (e.g. GetAllNodesFromAst).

## Architecture & Domain

### Core Components

- **FocusNode** (Models/Focus/FocusNode.cs): The primary domain entity. Contains grid position, dependencies, mutual exclusivity, etc.
- **FocusType**: Differs between a standard focus and shared\_focus.
- **ParadoxPower Parser**: Used for reading Paradox script format AST. Key logic is in FocusNodeHelper.GetAllNodesFromAst() supporting recursive resolution of shared focus links.

### Project Structure Principles

- Hoi4BlueprintBuilder.Core: Contains all UI Views (.axaml), ViewModels (.cs), Domain Models, and Services.
- Hoi4BlueprintBuilder.Windows / Hoi4BlueprintBuilder.Linux: Minimal platform-specific entry points.

*(Note: Avalonia UI conventions and XAML patterns are covered by the included xaml-csharp-development-skill-for-avalonia skill.)*

## Build and Test

Agents running tests or builds should use the following standard commands:

\`\`bash

# Build

dotnet build

# Run unit tests

dotnet test

# Publish Windows version

dotnet publish -r win-x64 -o .\publish\win-x64 .\Hoi4BlueprintBuilder.Windows\Hoi4BlueprintBuilder.Windows.csproj --self-contained true
\`\`

### Testing Conventions

- **Framework**: NUnit 4
- **Test Data**: Resides in Hoi4BlueprintBuilder.UnitTests/TestData/, accessible via TestApp.TestDataDirectory.
- **Setup/Teardown**: Always use TestHelper.CreateUniqueTempDirectory() for file-system-bound tests to ensure isolation.

