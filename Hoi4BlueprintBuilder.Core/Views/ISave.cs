namespace Hoi4BlueprintBuilder.Core.Views;

/// <summary>
/// 表示当前界面的数据是否可以保存到文件中
/// </summary>
public interface ISave
{
    /// <summary>
    /// 保存数据到文件中
    /// </summary>
    void Save();
}