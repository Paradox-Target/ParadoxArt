using DeviceId;
using NLog;
#if ANDROID
using DeviceId.Components;
#endif

namespace Hoi4BlueprintBuilder.Core.Infrastructure;

public sealed class AndroidDeviceIdBuilder(DeviceIdBuilder builder)
{
#if ANDROID
    private const string FallbackPreferencesName = "device_id";
    private const string FallbackIdKey = "fallback_id";
#endif
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 添加 Android 设备标识 (AndroidId), 如果 AndroidId 缺失则使用本地持久化的回退设备标识.
    /// </summary>
    public void AddAndroidDeviceId()
    {
#if ANDROID
        var context = Android.App.Application.Context;
        var androidId = Android.Provider.Settings.Secure.GetString(
            context.ContentResolver,
            Android.Provider.Settings.Secure.AndroidId
        );

        if (string.IsNullOrEmpty(androidId))
        {
            Log.Warn("AndroidId 缺失，使用本地持久化的回退设备标识");
            androidId = GetOrCreateFallbackId(context);
        }

        builder.AddComponent("android_device_id", new DeviceIdComponent(androidId));
#endif
    }

#if ANDROID
    /// <summary>
    /// 获取或创建回退设备标识。AndroidId 缺失时生成随机 GUID 并持久化到 SharedPreferences，
    /// 保证同一安装内标识稳定（卸载或清除数据后会重新生成）。
    /// </summary>
    private static string GetOrCreateFallbackId(Android.Content.Context context)
    {
        var preferences = context.GetSharedPreferences(
            FallbackPreferencesName,
            Android.Content.FileCreationMode.Private
        );
        if (preferences is null)
        {
            Log.Warn("无法获取 SharedPreferences");
            return "unknown";
        }
        var fallbackId = preferences.GetString(FallbackIdKey, null);
        if (!string.IsNullOrEmpty(fallbackId))
        {
            return fallbackId;
        }

        fallbackId = Guid.NewGuid().ToString("N");
        var editor = preferences.Edit();
        editor?.PutString(FallbackIdKey, fallbackId);
        editor?.Apply();
        return fallbackId;
    }
#endif
}

public static class DeviceIdExtensions
{
    public static DeviceIdBuilder OnAndroid(
        this DeviceIdBuilder builder,
        Action<AndroidDeviceIdBuilder>? androidBuilderConfiguration
    )
    {
        if (OperatingSystem.IsAndroid() && androidBuilderConfiguration is not null)
        {
            var androidBuilder = new AndroidDeviceIdBuilder(builder);
            androidBuilderConfiguration(androidBuilder);
        }
        return builder;
    }
}
