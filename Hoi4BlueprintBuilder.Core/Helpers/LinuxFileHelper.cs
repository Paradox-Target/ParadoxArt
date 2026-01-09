using System.Web;

namespace Hoi4BlueprintBuilder.Core.Helpers;

public static class LinuxFileHelper
{
    // XDG 规范
    // 在 Ubuntu 下测试
    // https://cgit.freedesktop.org/xdg/xdg-specs/plain/trash/trash-spec.xml
    private const string TrashDir = ".local/share/Trash";
    private const string FilesDir = "files";
    private const string InfoDir = "info";

    public static bool TryMoveToRecycleBin(string fileOrDirectoryPath, out string? errorMessage)
    {
        bool isFile = File.Exists(fileOrDirectoryPath);

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string trashPath = Path.Combine(homeDir, TrashDir);
        string trashFilesPath = Path.Combine(trashPath, FilesDir);
        string trashInfoPath = Path.Combine(trashPath, InfoDir);

        // Ensure the trash directories exist
        Directory.CreateDirectory(trashFilesPath);
        Directory.CreateDirectory(trashInfoPath);

        string fileOrDirectoryName = Path.GetFileName(fileOrDirectoryPath);

        string destPath = Path.Combine(trashFilesPath, fileOrDirectoryName);
        string uniqueFileOrDirectoryName = GetUniqueName(destPath);

        // Create .trashinfo metadata file
        string infoFilePath = Path.Combine(
            trashInfoPath,
            $"{Path.GetFileName(uniqueFileOrDirectoryName)}.trashinfo"
        );
        CreateTrashInfoFile(infoFilePath, fileOrDirectoryPath);

        if (isFile)
        {
            File.Move(fileOrDirectoryPath, uniqueFileOrDirectoryName);
        }
        else
        {
            Directory.Move(fileOrDirectoryPath, uniqueFileOrDirectoryName);
        }

        errorMessage = null;
        return true;
    }

    private static string GetUniqueName(string basePath)
    {
        string uniquePath = basePath;
        int counter = 1;
        while (Path.Exists(uniquePath))
        {
            uniquePath = $"{basePath}.{counter}";
            counter++;
        }

        return uniquePath;
    }

    private static void CreateTrashInfoFile(string infoFilePath, string originalPath)
    {
        string path = HttpUtility.UrlEncode(originalPath, App.Utf8EncodingWithoutBom);
        // 不转也能正常工作, Ubuntu 是转了的
        path = path.Replace("%2f", "/");

        using StreamWriter writer = new(infoFilePath, false, App.Utf8EncodingWithoutBom);
        writer.WriteLine("[Trash Info]");
        writer.WriteLine($"Path={path}");
        writer.WriteLine($"DeletionDate={DateTime.Now:s}");
    }
}
