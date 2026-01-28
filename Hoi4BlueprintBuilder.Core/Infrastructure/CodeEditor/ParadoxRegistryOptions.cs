using Avalonia.Platform;
using Avalonia.Styling;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Themes.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace Hoi4BlueprintBuilder.Core.Infrastructure.CodeEditor;

public sealed class ParadoxRegistryOptions(ThemeVariant? themeVariant, RegistryOptions rawOptions)
    : IRegistryOptions
{
    private static string ThemesFolderPath => string.Join('/', AssetsFolder, "CodeEditor", "Themes");
    private static string GrammarsFolderPath => string.Join('/', AssetsFolder, "CodeEditor", "Grammars");
    private static string AssetsFolder { get; } =
        string.Join('/', $"avares://{typeof(ParadoxRegistryOptions).Assembly.GetName().Name}", "Assets");

    private readonly RegistryOptions _rawOptions = rawOptions;

    public IRawTheme GetTheme(string scopeName)
    {
        if (string.IsNullOrWhiteSpace(scopeName))
        {
            return GetDefaultTheme();
        }

        var path = string.Join('/', ThemesFolderPath, scopeName);
        if (!File.Exists(path))
        {
            return GetDefaultTheme();
        }

        return ThemeReader.ReadThemeSync(new StreamReader(AssetLoader.Open(new Uri(path))));
    }

    public IRawGrammar GetGrammar(string scopeName)
    {
        var grammar = _rawOptions.GetGrammar(scopeName);
        if (grammar is not null)
        {
            return grammar;
        }

        string path;
        if (scopeName == ScopeNameTypes.Yml)
        {
            path = string.Join('/', GrammarsFolderPath, "yaml", "syntaxes", "yaml.tmLanguage.json");
        }
        else
        {
            path = string.Join('/', GrammarsFolderPath, "paradox.tmLanguage.json");
        }
        return GrammarReader.ReadGrammarSync(new StreamReader(AssetLoader.Open(new Uri(path))));
    }

    public ICollection<string>? GetInjections(string scopeName)
    {
        return null;
    }

    public IRawTheme GetDefaultTheme()
    {
        var uri = new Uri(string.Join('/', ThemesFolderPath, GetThemeFileName(themeVariant)));
        return ThemeReader.ReadThemeSync(new StreamReader(AssetLoader.Open(uri)));
    }

    public IRawTheme LoadTheme(ThemeVariant theme)
    {
        var uri = new Uri(string.Join('/', ThemesFolderPath, GetThemeFileName(theme)));
        return ThemeReader.ReadThemeSync(new StreamReader(AssetLoader.Open(uri)));
    }

    private static string GetThemeFileName(ThemeVariant? theme)
    {
        if (theme == ThemeVariant.Dark)
        {
            return "dark_plus.json";
        }

        if (theme == ThemeVariant.Light)
        {
            return "light_plus.json";
        }

        return "dark_plus.json";
    }
}
