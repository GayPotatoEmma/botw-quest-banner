using Dalamud.Game.DutyState;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

namespace QuestBanners;

public sealed class DutyBannerService : IDisposable
{
    private static readonly Dictionary<uint, string> ContentTypeTitles = new()
    {
        [2]  = "The Delver's Path",
        [3]  = "A Teaching of Tactics",
        [4]  = "A Clash of Echoes",
        [5]  = "The Crucible of Cooperation",
        [6]  = "A Test of Rivals",
        [9]  = "A Trial of Fortune",
        [21] = "The Endless Descent",
        [27] = "The Azure Puzzle",
        [28] = "An Ultimate Test of Perfection",
        [30] = "The Branching Fates",
        [31] = "Bounty of the Spectral Tide",
        [37] = "A Chaotic Test of Unity",
    };

    public event Action<string, string>? DutyBannerRequested;

    private readonly IDutyState _dutyState;

    public DutyBannerService(IDutyState dutyState)
    {
        _dutyState = dutyState;
        _dutyState.DutyStarted += OnDutyStarted;
    }

    private void OnDutyStarted(IDutyStateEventArgs args)
    {
        try
        {
            var cfc = _dutyState.ContentFinderCondition;
            if (!cfc.IsValid) return;
            if (!cfc.Value.ContentType.IsValid) return;

            var contentTypeId = cfc.Value.ContentType.Value.RowId;
            if (!ContentTypeTitles.TryGetValue(contentTypeId, out var bannerTitle)) return;

            var dutyName = cfc.Value.Name.ToString();
            if (string.IsNullOrWhiteSpace(dutyName)) return;

            Plugin.Log.Debug($"[DutyBanner] DutyStarted — ContentType {contentTypeId}: '{dutyName}'");
            DutyBannerRequested?.Invoke(bannerTitle, dutyName);
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "[DutyBanner] Failed to resolve duty banner on DutyStarted.");
        }
    }

    public void Dispose()
    {
        _dutyState.DutyStarted -= OnDutyStarted;
    }
}
