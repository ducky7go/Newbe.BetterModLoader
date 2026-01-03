using System.Text.RegularExpressions;

namespace Newbe.BetterModLoader.DeveloperModes.Uploads;

/// <summary>
/// 提供 Markdown 到 Steam Workshop BBCode 格式的转换工具。
/// 参考：https://steamcommunity.com/comment/Recommendation/formattinghelp
/// </summary>
internal static class MarkdownToSteamTextHelper
{
    /// <summary>
    /// 将 Markdown 文本转换为 Steam 支持的格式。
    /// 支持标题、粗体、斜体、删除线、代码块、列表、引用、链接等常见 Markdown 语法。
    /// </summary>
    /// <param name="markdown">Markdown 格式的文本</param>
    /// <returns>Steam 格式的文本</returns>
    public static string ConvertMarkdownToSteam(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return markdown;
        }

        var result = markdown;

        // 标题转换 (需要在其他转换之前处理，避免 # 被误处理)
        // H1: # Title -> [h1]Title[/h1]
        result = Regex.Replace(result, @"^#\s+(.+)$", "[h1]$1[/h1]", RegexOptions.Multiline);
        // H2: ## Title -> [h2]Title[/h2]
        result = Regex.Replace(result, @"^##\s+(.+)$", "[h2]$1[/h2]", RegexOptions.Multiline);
        // H3: ### Title -> [h3]Title[/h3]
        result = Regex.Replace(result, @"^###\s+(.+)$", "[h3]$1[/h3]", RegexOptions.Multiline);

        // 粗体: **text** 或 __text__ -> [b]text[/b]
        result = Regex.Replace(result, @"\*\*(.+?)\*\*", "[b]$1[/b]");
        result = Regex.Replace(result, @"__(.+?)__", "[b]$1[/b]");

        // 斜体: *text* 或 _text_ -> [i]text[/i]
        // 注意：需要在粗体之后处理，避免被粗体的 ** 干扰
        result = Regex.Replace(result, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "[i]$1[/i]");
        result = Regex.Replace(result, @"(?<!_)_(?!_)(.+?)(?<!_)_(?!_)", "[i]$1[/i]");

        // 删除线: ~~text~~ -> [strike]text[/strike]
        result = Regex.Replace(result, @"~~(.+?)~~", "[strike]$1[/strike]");

        // 行内代码: `code` -> [b]code[/b]
        result = Regex.Replace(result, @"`(.+?)`", "[b]$1[/b]");

        // 代码块: ```...``` -> [code]...[/code]
        result = Regex.Replace(result, @"```[\w]*\s*\n?([\s\S]+?)```", "[code]$1[/code]", RegexOptions.Multiline);

        // 链接: [text](url) -> [url=url]text[/url]
        result = Regex.Replace(result, @"\[([^\]]+)\]\(([^\)]+)\)", "[url=$2]$1[/url]");

        // 水平分隔线: --- 或 *** 或 ___ -> [hr][/hr]
        result = Regex.Replace(result, @"^(\*\*\*|---|___)$", "[hr][/hr]", RegexOptions.Multiline);

        // 无序列表: - item 或 * item -> [list][*]item[/list]
        result = ConvertUnorderedList(result);

        // 有序列表: 1. item -> [olist][*]item[/olist]
        result = ConvertOrderedList(result);

        // 引用: > text -> [quote]text[/quote]
        result = ConvertBlockquote(result);

        return result;
    }

    /// <summary>
    /// 转换无序列表（- 或 * 开头的多行列表为 BBCode [list]）。
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <returns>转换后的文本</returns>
    private static string ConvertUnorderedList(string text)
    {
        // 匹配连续的无序列表项
        const string pattern = @"(?:^[\*\-]\s+.+$\n?)+";
        return Regex.Replace(text, pattern, match =>
        {
            var items = match.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var listItems = string.Join("\n", items.Select(item =>
            {
                var content = Regex.Replace(item, @"^[\*\-]\s+", "");
                return $"[*]{content}";
            }));
            return $"[list]\n{listItems}\n[/list]\n";
        }, RegexOptions.Multiline);
    }

    /// <summary>
    /// 转换有序列表（数字. 开头的多行列表为 BBCode [olist]）。
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <returns>转换后的文本</returns>
    private static string ConvertOrderedList(string text)
    {
        // 匹配连续的有序列表项
        const string pattern = @"(?:^\d+\.\s+.+$\n?)+";
        return Regex.Replace(text, pattern, match =>
        {
            var items = match.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var listItems = string.Join("\n", items.Select(item =>
            {
                var content = Regex.Replace(item, @"^\d+\.\s+", "");
                return $"[*]{content}";
            }));
            return $"[olist]\n{listItems}\n[/olist]\n";
        }, RegexOptions.Multiline);
    }

    /// <summary>
    /// 转换引用块（> 开头的多行为 BBCode [quote]）。
    /// </summary>
    /// <param name="text">原始文本</param>
    /// <returns>转换后的文本</returns>
    private static string ConvertBlockquote(string text)
    {
        // 匹配连续的引用行
        const string pattern = @"(?:^>\s*.+$\n?)+";
        return Regex.Replace(text, pattern, match =>
        {
            var lines = match.Value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var content = string.Join("\n", lines.Select(line =>
                Regex.Replace(line, @"^>\s*", "")
            ));
            return $"[quote]{content}[/quote]\n";
        }, RegexOptions.Multiline);
    }
}
