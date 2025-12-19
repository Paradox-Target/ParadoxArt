using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hoi4BlueprintBuilder.Core.Services;
using Hoi4BlueprintBuilder.Core.Views.Initialization;
using NLog;

namespace Hoi4BlueprintBuilder.Core.ViewsModels;

[RegisterTransient<ActivateViewModel>]
public sealed partial class ActivateViewModel : ObservableObject
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

    public ActivateViewModel(
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
                _notificationService.Show("激活设备失败, 服务器未返回有效数据", "激活失败", NotificationType.Error);
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
                _notificationService.Show($"激活设备失败: {response.Message}", "激活失败", NotificationType.Error);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "激活设备失败");
            _notificationService.Show($"内部错误, 激活设备失败: {e.Message}", "激活失败", NotificationType.Error);
        }
        finally
        {
            ButtonText = "激活设备";
        }
    }
}
