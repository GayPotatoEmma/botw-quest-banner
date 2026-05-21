using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Lumina.Excel.Sheets;
using System;
using System.Text.RegularExpressions;

namespace QuestBanners;

public enum BannerType { Accepted, Complete }

public sealed class QuestBannerService : IDisposable
{
    private static readonly Regex EnAcceptedRegex = new(@"^[\u201C\u0022](.+?)[\u201D\u0022]\s+accepted\.$",   RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EnCompleteRegex  = new(@"^[\u201C\u0022](.+?)[\u201D\u0022]\s+complete[.!]$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex JaAcceptedRegex = new(@"^クエスト[『](.+?)[』]を引き受けた！$", RegexOptions.Compiled);
    private static readonly Regex JaCompleteRegex  = new(@"^クエスト[『](.+?)[』]をコンプリートした！$", RegexOptions.Compiled);

    private static readonly Regex DeAcceptedRegex = new(@"^Auftrag [\u201E\u0022](.+?)[\u201C\u0022]\s+angenommen\.$",   RegexOptions.Compiled);
    private static readonly Regex DeCompleteRegex  = new(@"^Auftrag [\u201E\u0022](.+?)[\u201C\u0022]\s+abgeschlossen!$", RegexOptions.Compiled);

    private static readonly Regex FrAcceptedRegex = new(@"^Vous acceptez la qu\u00EAte [\u201C\u0022](.+?)[\u201D\u0022]\.$",        RegexOptions.Compiled);
    private static readonly Regex FrCompleteRegex  = new(@"^Vous avez accompli la qu\u00EAte [\u201C\u0022](.+?)[\u201D\u0022]\.$", RegexOptions.Compiled);

    public event Action<string, BannerType, string?, uint>? BannerRequested;

    public QuestBannerService()
    {
        Plugin.ChatGui.ChatMessage += OnChatMessage;
    }

    private void OnChatMessage(IHandleableChatMessage message)
    {
        if (message.LogKind != XivChatType.SystemMessage) return;

        var text = message.Message.TextValue;
        Plugin.Log.Debug($"[QuestBanner] SystemMessage: '{text}'");

        if (TryFirstMatch(text, EnAcceptedRegex, JaAcceptedRegex, DeAcceptedRegex, FrAcceptedRegex, out var acceptName))
        {
            LookupQuestMeta(acceptName, out var category, out var iconId);
            BannerRequested?.Invoke(CleanDisplayName(acceptName), BannerType.Accepted, category, iconId);
            return;
        }

        if (TryFirstMatch(text, EnCompleteRegex, JaCompleteRegex, DeCompleteRegex, FrCompleteRegex, out var completeName))
        {
            LookupQuestMeta(completeName, out var category, out var iconId);
            BannerRequested?.Invoke(CleanDisplayName(completeName), BannerType.Complete, category, iconId);
        }
    }

    private static bool TryFirstMatch(string text, Regex r1, Regex r2, Regex r3, Regex r4, out string result)
    {
        foreach (var r in new[] { r1, r2, r3, r4 })
        {
            var m = r.Match(text);
            if (m.Success)
            {
                result = m.Groups[1].Value;
                return true;
            }
        }
        result = string.Empty;
        return false;
    }

    private static string CleanDisplayName(string name) => name.Replace("\uE0BE", "").Trim();

    private static void LookupQuestMeta(string questName, out string? category, out uint iconId)
    {
        category = null;
        iconId   = 0;
        try
        {
            foreach (var row in Plugin.DataManager.GetExcelSheet<Quest>())
            {
                var name = row.Name.ToString();
                if (string.IsNullOrEmpty(name) || name != questName) continue;
                if (!row.JournalGenre.IsValid) break;
                var genre = row.JournalGenre.Value;
                var genreName = genre.Name.ToString();
                if (!string.IsNullOrEmpty(genreName))
                    category = genreName;
                iconId = (uint)genre.Icon;
                break;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "[QuestBanner] Failed to look up quest category.");
        }
    }

    public void Dispose()
    {
        Plugin.ChatGui.ChatMessage -= OnChatMessage;
    }
}
