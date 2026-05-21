using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace FantasyOfTheWild.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(Plugin plugin) : base("Fantasy of the Wild###FantasyOfTheWildConfig")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(300, 90);
        SizeCondition = ImGuiCond.Always;

        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text("Theme");

        var themes = new[] { "Breath of the Wild", "Tears of the Kingdom" };
        var themeIndex = (int)configuration.Theme;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("###Theme", ref themeIndex, themes, themes.Length))
        {
            configuration.Theme = (BannerTheme)themeIndex;
            configuration.Save();
        }

        }
}
