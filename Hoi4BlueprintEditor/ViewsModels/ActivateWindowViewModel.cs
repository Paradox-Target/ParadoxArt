using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hoi4BlueprintEditor.Messages;
using Hoi4BlueprintEditor.Services;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Hoi4BlueprintEditor.ViewsModels;

public sealed partial class ActivateWindowViewModel : ObservableObject
{
    public bool IsActivated { get; private set; }

    [ObservableProperty]
    private string _activationCode = string.Empty;

    [ObservableProperty]
    private string _buttonText = "激活设备";

    private readonly AuthService _authService = App.Current.Services.GetRequiredService<AuthService>();
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    [RelayCommand]
    private async Task Activate()
    {
        if (string.IsNullOrWhiteSpace(ActivationCode))
        {
            return;
        }

        ButtonText = "激活中...";
        try
        {
            var response = await _authService.ActivateDeviceAsync(ActivationCode);
            if (response is null)
            {
                MessageBox.Show("激活设备失败, 服务器未返回有效数据", "激活失败", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsActivated = response.IsActivated;

            if (IsActivated)
            {
                await MessageBox.ShowAsync(
                    "激活设备成功, 感谢您的支持!",
                    "激活成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                WeakReferenceMessenger.Default.Send(new ActivateSuccessMessage());
            }
            else
            {
                MessageBox.Show(
                    $"激活设备失败: {response.Message}",
                    "激活失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "激活设备失败");
            MessageBox.Show($"内部错误, 激活设备失败: {e.Message}", "激活失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ButtonText = "激活设备";
        }
    }
}
