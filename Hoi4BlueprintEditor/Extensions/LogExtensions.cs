using NLog;
using ParadoxPower.CSharp;

namespace Hoi4BlueprintEditor.Extensions;

public static class LogExtensions
{
    public static void LogParseError(this Logger logger, ParserError error)
    {
        logger.Warn(
            "文件解析失败, 原因: {Message}, path: {Path}, Line: {Line} Column: {Column}",
            error.ErrorMessage,
            error.Filename,
            error.Line,
            error.Column
        );
    }
}
