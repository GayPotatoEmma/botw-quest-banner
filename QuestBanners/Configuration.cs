using Dalamud.Configuration;
using System;

namespace QuestBanners;

public enum BannerTheme { BotW, TotK }

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool QuestBannersEnabled { get; set; } = true;
    public BannerTheme QuestBannerTheme { get; set; } = BannerTheme.BotW;

    public bool DutyBannersEnabled { get; set; } = true;
    public BannerTheme DutyBannerTheme { get; set; } = BannerTheme.BotW;
    [Obsolete] public BannerTheme Theme { get; set; } = BannerTheme.BotW;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
