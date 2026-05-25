using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace QuestBanners.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration _cfg;

    private static readonly string[] ThemeNames = ["Breath of the Wild", "Tears of the Kingdom"];

    public ConfigWindow(Plugin plugin) : base("Quest Banners###QuestBannersConfig")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(320, 160);
        SizeCondition = ImGuiCond.Always;

        _cfg = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        bool questEnabled = _cfg.QuestBannersEnabled;
        if (ImGui.Checkbox("Quest Banners", ref questEnabled))
        {
            _cfg.QuestBannersEnabled = questEnabled;
            _cfg.Save();
        }

        ImGui.BeginDisabled(!questEnabled);
        ImGui.Indent();
        var questTheme = (int)_cfg.QuestBannerTheme;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("###QuestTheme", ref questTheme, ThemeNames, ThemeNames.Length))
        {
            _cfg.QuestBannerTheme = (BannerTheme)questTheme;
            _cfg.Save();
        }
        ImGui.Unindent();
        ImGui.EndDisabled();

        ImGui.Spacing();

        bool dutyEnabled = _cfg.DutyBannersEnabled;
        if (ImGui.Checkbox("Duty Banners", ref dutyEnabled))
        {
            _cfg.DutyBannersEnabled = dutyEnabled;
            _cfg.Save();
        }

        ImGui.BeginDisabled(!dutyEnabled);
        ImGui.Indent();
        var dutyTheme = (int)_cfg.DutyBannerTheme;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("###DutyTheme", ref dutyTheme, ThemeNames, ThemeNames.Length))
        {
            _cfg.DutyBannerTheme = (BannerTheme)dutyTheme;
            _cfg.Save();
        }
        ImGui.Unindent();
        ImGui.EndDisabled();
    }
}
