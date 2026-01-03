namespace Newbe.BetterModLoader.Utils;

public abstract class FileHelper
{
    public static void OpenFolder(string path)
    {
        try
        {
            // open folder if it is a file path
            if (File.Exists(path))
            {
                path = Path.GetDirectoryName(path) ?? path;
            }

            if (Directory.Exists(path))
            {
                Application.OpenURL("file:///" + path);
            }
            else
            {
                Log.Warn($"Open folder: Folder does not exist: {path}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Open folder: Failed to open folder {path}");
        }
    }

    public static void CopyToClipboard(string text)
    {
        try
        {
            GUIUtility.systemCopyBuffer = text;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Copy to clipboard: Failed to copy text to clipboard");
        }
    }
}
