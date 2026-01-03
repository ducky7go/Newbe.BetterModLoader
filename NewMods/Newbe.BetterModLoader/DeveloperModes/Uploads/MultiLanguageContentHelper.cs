namespace Newbe.BetterModLoader.DeveloperModes.Uploads;

/// <summary>
/// Helper class for loading multi-language content files (titles and descriptions) for Steam Workshop uploads.
/// Supports directory structure: description/{langcode}.md, title/{langcode}.md, etc.
/// Falls back to description.md for English if description/en.md doesn't exist.
/// </summary>
internal static class MultiLanguageContentHelper
{
    /// <summary>
    /// Maps game language codes to Steam API language codes.
    /// Based on: [LanguageSupport("en", "zh-hant", "zh", "fr", "de", "es", "ru", "ja", "ko", "pt")]
    /// </summary>
    private static readonly Dictionary<string, string> LanguageCodeMap = new()
    {
        { "en", "english" },
        { "zh", "schinese" }, // Simplified Chinese
        { "zh-hant", "tchinese" }, // Traditional Chinese
        { "ja", "japanese" },
        { "ko", "koreana" },
        { "ru", "russian" },
        { "de", "german" },
        { "fr", "french" },
        { "es", "latam" }, // Spanish - Latin America
        { "pt", "brazilian" } // Portuguese - Brazil
    };

    /// <summary>
    /// Loads all available language descriptions from a mod folder.
    /// Looks for files in description/ directory: description/en.md, description/zh.md, etc.
    /// Falls back to description.md for English if description/en.md doesn't exist.
    /// </summary>
    /// <param name="modFolderPath">Path to the mod folder</param>
    /// <returns>Dictionary mapping Steam API language codes to description content (converted to BBCode)</returns>
    public static Dictionary<string, string> LoadMultiLanguageDescriptions(string modFolderPath)
    {
        var descriptions = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(modFolderPath) || !Directory.Exists(modFolderPath))
        {
            Log.Debug($"[MultiLanguageContentHelper] Invalid mod folder path: {modFolderPath}");
            return descriptions;
        }

        var descriptionDir = Path.Combine(modFolderPath, "description");

        // Check for language-specific description files in description/ directory
        foreach (var kvp in LanguageCodeMap)
        {
            var gameLangCode = kvp.Key;
            var steamLangCode = kvp.Value;

            var langDescPath = Path.Combine(descriptionDir, $"{gameLangCode}.md");
            if (File.Exists(langDescPath))
            {
                try
                {
                    var content = File.ReadAllText(langDescPath);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var bbCode = MarkdownToSteamTextHelper.ConvertMarkdownToSteam(content);
                        descriptions[steamLangCode] = bbCode;
                        Log.Debug(
                            $"[MultiLanguageContentHelper] Loaded description/{gameLangCode}.md as {steamLangCode}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(
                        $"[MultiLanguageContentHelper] Failed to read description/{gameLangCode}.md: {ex.Message}");
                }
            }
        }

        // If English wasn't found in description/en.md, check for fallback description.md
        if (!descriptions.ContainsKey("english"))
        {
            var defaultDescPath = Path.Combine(modFolderPath, "description.md");
            if (File.Exists(defaultDescPath))
            {
                try
                {
                    var content = File.ReadAllText(defaultDescPath);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var bbCode = MarkdownToSteamTextHelper.ConvertMarkdownToSteam(content);
                        descriptions["english"] = bbCode;
                        Log.Debug($"[MultiLanguageContentHelper] Loaded fallback description.md as English");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[MultiLanguageContentHelper] Failed to read description.md: {ex.Message}");
                }
            }
        }

        Log.Debug(
            $"[MultiLanguageContentHelper] Loaded {descriptions.Count} language description(s) from {modFolderPath}");
        return descriptions;
    }

    /// <summary>
    /// Loads all available language titles from a mod folder.
    /// Looks for files in title/ directory: title/en.md, title/zh.md, etc.
    /// Falls back to defaultDisplayName when title file not found for a language.
    /// </summary>
    /// <param name="modFolderPath">Path to the mod folder</param>
    /// <param name="defaultDisplayName">Default display name to use as fallback</param>
    /// <returns>Dictionary mapping Steam API language codes to title content</returns>
    public static Dictionary<string, string> LoadMultiLanguageTitles(string modFolderPath, string defaultDisplayName)
    {
        var titles = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(modFolderPath) || !Directory.Exists(modFolderPath))
        {
            Log.Debug($"[MultiLanguageContentHelper] Invalid mod folder path: {modFolderPath}");
            return titles;
        }

        var titleDir = Path.Combine(modFolderPath, "title");

        // Check for language-specific title files in title/ directory
        foreach (var kvp in LanguageCodeMap)
        {
            var gameLangCode = kvp.Key;
            var steamLangCode = kvp.Value;

            var langTitlePath = Path.Combine(titleDir, $"{gameLangCode}.md");
            if (File.Exists(langTitlePath))
            {
                try
                {
                    var content = File.ReadAllText(langTitlePath);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        titles[steamLangCode] = content.Trim();
                        Log.Debug($"[MultiLanguageContentHelper] Loaded title/{gameLangCode}.md as {steamLangCode}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[MultiLanguageContentHelper] Failed to read title/{gameLangCode}.md: {ex.Message}");
                }
            }
            else
            {
                // Fallback to defaultDisplayName for this language
                titles[steamLangCode] = defaultDisplayName;
            }
        }

        Log.Debug($"[MultiLanguageContentHelper] Loaded {titles.Count} language title(s) from {modFolderPath}");
        return titles;
    }

    /// <summary>
    /// Gets a list of available language codes for descriptions in the mod folder.
    /// Checks description/ directory first, then falls back to description.md for English.
    /// </summary>
    /// <param name="modFolderPath">Path to the mod folder</param>
    /// <returns>List of game language codes (e.g., "en", "zh", "ja")</returns>
    public static List<string> GetAvailableLanguages(string modFolderPath)
    {
        var languages = new List<string>();

        if (string.IsNullOrEmpty(modFolderPath) || !Directory.Exists(modFolderPath))
        {
            return languages;
        }

        var descriptionDir = Path.Combine(modFolderPath, "description");

        // Check for language-specific files in description/ directory
        foreach (var gameLangCode in LanguageCodeMap.Keys)
        {
            var langDescPath = Path.Combine(descriptionDir, $"{gameLangCode}.md");
            if (File.Exists(langDescPath))
            {
                languages.Add(gameLangCode);
            }
        }

        return languages;
    }

    /// <summary>
    /// Gets a list of available language codes for titles in the mod folder.
    /// Checks title/ directory for language-specific title files.
    /// </summary>
    /// <param name="modFolderPath">Path to the mod folder</param>
    /// <returns>List of game language codes (e.g., "en", "zh", "ja")</returns>
    public static List<string> GetAvailableTitleLanguages(string modFolderPath)
    {
        var languages = new List<string>();

        if (string.IsNullOrEmpty(modFolderPath) || !Directory.Exists(modFolderPath))
        {
            return languages;
        }

        var titleDir = Path.Combine(modFolderPath, "title");

        // Check for language-specific files in title/ directory
        foreach (var gameLangCode in LanguageCodeMap.Keys)
        {
            var langTitlePath = Path.Combine(titleDir, $"{gameLangCode}.md");
            if (File.Exists(langTitlePath))
            {
                languages.Add(gameLangCode);
            }
        }

        return languages;
    }

    /// <summary>
    /// Gets a human-readable display name for a language code.
    /// </summary>
    /// <param name="gameLangCode">Game language code (e.g., "zh", "ja")</param>
    /// <returns>Display name (e.g., "Simplified Chinese", "Japanese")</returns>
    public static string GetLanguageDisplayName(string gameLangCode)
    {
        return gameLangCode switch
        {
            "en" => "English",
            "zh" => "Simplified Chinese",
            "zh-hant" => "Traditional Chinese",
            "ja" => "Japanese",
            "ko" => "Korean",
            "ru" => "Russian",
            "de" => "German",
            "fr" => "French",
            "es" => "Spanish - Latin America",
            "pt" => "Portuguese - Brazil",
            _ => gameLangCode
        };
    }
}
