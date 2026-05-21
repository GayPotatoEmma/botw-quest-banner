using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FantasyOfTheWild.Windows;

namespace FantasyOfTheWild;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;

    private const string CommandName = "/fotw";
    private const string BannerTestCommand = "/questbanner";

    public Configuration Configuration { get; init; }
    internal static Configuration Config { get; private set; } = null!;

    public readonly WindowSystem WindowSystem = new("FantasyOfTheWild");
    private ConfigWindow ConfigWindow { get; init; }
    private QuestBannerOverlay BannerOverlay { get; init; }
    private QuestBannerService BannerService { get; init; }
    private BannerSuppressService BannerSuppress { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Config = Configuration;

        ConfigWindow = new ConfigWindow(this);
        BannerOverlay = new QuestBannerOverlay();

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(BannerOverlay);

        BannerService = new QuestBannerService();
        BannerService.BannerRequested += (name, type, category, iconId) => BannerOverlay.ShowBanner(name, type, category, iconId);
        BannerSuppress = new BannerSuppressService(GameInterop);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the Fantasy of the Wild settings window."
        });

        CommandManager.AddHandler(BannerTestCommand, new CommandInfo(OnBannerTestCommand)
        {
            HelpMessage = "Test the BotW quest banner. Usage: /questbanner [accepted|complete] [quest name]"
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        Log.Information($"{PluginInterface.Manifest.Name} loaded.");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        WindowSystem.RemoveAllWindows();

        BannerSuppress.Dispose();
        BannerService.Dispose();
        BannerOverlay.Dispose();
        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(BannerTestCommand);
    }

    private void OnCommand(string command, string args)
    {
        ConfigWindow.Toggle();
    }

    private void OnBannerTestCommand(string command, string args)
    {
        var parts = args.Split(' ', 2, System.StringSplitOptions.RemoveEmptyEntries);
        var type  = (parts.Length > 0 && parts[0].Equals("complete", System.StringComparison.OrdinalIgnoreCase))
                    ? BannerType.Complete
                    : BannerType.Accepted;
        var name  = parts.Length > 1 ? parts[1] : "Test of Will";
        BannerOverlay.ShowBanner(name, type);
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
}
