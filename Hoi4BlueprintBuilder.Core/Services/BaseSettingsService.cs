using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using NLog;

namespace Hoi4BlueprintBuilder.Core.Services;

/// <summary>
/// 配置文件服务的基类，封装通用的加载和保存逻辑
/// </summary>
/// <typeparam name="T">具体的配置类类型</typeparam>
public abstract class BaseSettingsService<T>
    where T : BaseSettingsService<T>
{
    // ReSharper disable once StaticMemberInGenericType
    protected static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 获取用于序列化的 JsonTypeInfo
    /// </summary>
    protected abstract JsonTypeInfo<T> JsonTypeInfo { get; }

    /// <summary>
    /// 获取配置文件的完整路径
    /// </summary>
    private string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 通用的加载逻辑
    /// </summary>
    /// <param name="filePath">配置文件的完整路径</param>
    /// <param name="jsonTypeInfo">Json类型信息</param>
    /// <param name="afterLoadAction">加载成功后的回调（可选）</param>
    /// <param name="defaultFactory">创建默认实例的工厂（可选，默认为无参构造）</param>
    /// <returns>加载的实例或默认实例</returns>
    protected static T LoadInternal(
        string filePath,
        JsonTypeInfo<T> jsonTypeInfo,
        Action<T>? afterLoadAction = null,
        Func<T>? defaultFactory = null
    )
    {
        Log.Info("尝试加载配置文件: {FilePath}", filePath);

        T result;
        defaultFactory ??= Activator.CreateInstance<T>;

        if (!File.Exists(filePath))
        {
            Log.Info("配置文件不存在: {FilePath}, 使用默认设置", filePath);
            result = defaultFactory();
        }
        else
        {
            try
            {
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                var deserialized = JsonSerializer.Deserialize(json, jsonTypeInfo);

                if (deserialized is null)
                {
                    Log.Warn("配置文件解析结果为空，使用默认设置");
                    result = defaultFactory();
                }
                else
                {
                    result = deserialized;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "加载配置文件失败，使用默认设置");
                result = defaultFactory();
            }
        }

        result.FilePath = filePath;
        afterLoadAction?.Invoke(result);
        return result;
    }

    /// <summary>
    /// 保存设置到文件
    /// </summary>
    public virtual void SaveSettings()
    {
        try
        {
            // 确保目录存在
            string? directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 既然当前类继承自 BaseSettingsService<T>，那么当前实例应当就是 T 类型
            // 如果 T 不是当前类，这里会抛出转换异常，这属于设计错误
            T instance = (T)(object)this;

            string json = JsonSerializer.Serialize(instance, JsonTypeInfo);
            File.WriteAllText(FilePath, json, Encoding.UTF8);
            Log.Info("已成功保存配置文件: {FilePath}", FilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存配置文件失败: {FilePath}", FilePath);
        }
    }
}
