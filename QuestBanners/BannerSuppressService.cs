using System;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace FantasyOfTheWild;

public sealed unsafe class BannerSuppressService : IDisposable
{
    private static readonly int[] SuppressedIds = [120001, 120002, 120031, 120032, 121081, 121082];

    private delegate void ImageSetImageTextureDelegate(AtkUnitBase* addon, int bannerId, int a3, int sfxId);

    private readonly Hook<ImageSetImageTextureDelegate>? _hook;

    public BannerSuppressService(IGameInteropProvider gameInterop)
    {
        try
        {
            _hook = gameInterop.HookFromSignature<ImageSetImageTextureDelegate>(
                "48 89 5C 24 ?? 57 48 83 EC 30 48 8B D9 89 91",
                OnSetImageTexture);
            _hook.Enable();
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "[BannerSuppress] Failed to hook ImageSetImageTexture.");
        }
    }

    private void OnSetImageTexture(AtkUnitBase* addon, int bannerId, int a3, int sfxId)
    {
        if (Array.IndexOf(SuppressedIds, bannerId) >= 0)
        {
            _hook!.Original(addon, 0, a3, 0);
            return;
        }

        _hook!.Original(addon, bannerId, a3, sfxId);
    }

    public void Dispose()
    {
        _hook?.Dispose();
    }
}
