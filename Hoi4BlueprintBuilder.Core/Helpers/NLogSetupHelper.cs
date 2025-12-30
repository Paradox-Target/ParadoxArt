using NLog;
using NLog.Config;
using NLog.Targets;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class NLogSetupHelper
{
    public static void Setup()
    {
        var config = new LoggingConfiguration();

        const string fileLayout =
            @"${date:format=HH\:mm\:ss} | ${level} | ${message:exceptionSeparator=\r\n:withException=true}";
        const string consoleLayout =
            @"${date:format=HH\:mm\:ss} ${callsite:includeNamespace=False} | ${level} | ${message:exceptionSeparator=\r\n:withException=true}";

        var consoleTarget = new DebugSystemTarget("logconsole") { Layout = consoleLayout };

        string logFolder;
        // [Release模式] 文件输出
        // 注意：在 Android/iOS 上不能直接写到程序根目录，必须使用 SpecialFolder.LocalApplicationData
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            logFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Hoi4BlueprintBuilder",
                "Logs"
            );
        }
        else
        {
            logFolder = Path.Combine(Environment.CurrentDirectory, "Logs");
        }

        // TODO: 移除授权系统后再在发布构造中移除 consoleTarget
        config.AddRule(LogLevel.Trace, LogLevel.Fatal, consoleTarget);
#if RELEASE
        var fileTarget = new FileTarget("logfile")
        {
            FileName = Path.Combine(logFolder, "${shortdate}.log"), // 每天一个文件
            Layout = fileLayout,
            ArchiveEvery = FileArchivePeriod.Day, // 每天归档
            MaxArchiveFiles = 7, // 最多保留7天
            Encoding = System.Text.Encoding.UTF8
        };

        var asyncFileTarget = new NLog.Targets.Wrappers.AsyncTargetWrapper(fileTarget) { Name = "asyncFile" };
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, asyncFileTarget);
#endif
        LogManager.Configuration = config;
    }
}
