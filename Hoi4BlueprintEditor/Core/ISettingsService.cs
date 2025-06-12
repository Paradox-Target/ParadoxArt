using Hoi4BlueprintEditor.Models;

namespace Hoi4BlueprintEditor.Core;

public interface ISettingsService
{
    SettingsModel CurrentSettings { get; }
    void LoadSettings();
    void SaveSettings();
}