using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hoi4BlueprintEditor.Services;
using Hoi4BlueprintEditor.Views.Initialization;
using NLog;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Hoi4BlueprintEditor.ViewsModels;

[RegisterTransient<ActivateWindowViewModel>]
public sealed partial class ActivateWindowViewModel : ObservableObject
{
    public bool IsActivated { get; private set; }

    [ObservableProperty]
    private string _activationCode = string.Empty;

    [ObservableProperty]
    private string _buttonText = "激活设备";

    private readonly AuthService _authService;
    private readonly NavigationService _navigationService;
    private readonly NotificationService _notificationService;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ActivateWindowViewModel(
        AuthService authService,
        NavigationService navigationService,
        NotificationService notificationService
    )
    {
        _authService = authService;
        _navigationService = navigationService;
        _notificationService = notificationService;
    }

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
                _notificationService.Show("设备激活成功，感谢您的支持！");
                _navigationService.NavigateTo<MainWelcomeView>();
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
