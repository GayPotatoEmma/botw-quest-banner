using Dalamud.Configuration;
using System;

namespace QuestBanners;

public enum BannerTheme { BotW, TotK }

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public BannerTheme Theme { get; set; } = BannerTheme.BotW;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
