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

        // TODO: 移除授权系统后再在发布构造中移除 consoleTarget
        config.AddRule(LogLevel.Trace, LogLevel.Fatal, consoleTarget);
#if RELEASE
        var fileTarget = new FileTarget("logfile")
        {
            FileName = Path.Combine(App.LogsFolder, "${shortdate}.log"), // 每天一个文件
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
