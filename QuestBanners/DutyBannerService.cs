using Dalamud.Game;
using Dalamud.Game.DutyState;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;

namespace QuestBanners;

public sealed class DutyBannerService : IDisposable
{
    private static readonly Dictionary<uint, Dictionary<ClientLanguage, string>> ContentTypeTitles = new()
    {
        [2] = new()
        {
            [ClientLanguage.English] = "The Delver's Path",
            [ClientLanguage.German] = "Der Pfad des Erkunders",
            [ClientLanguage.French] = "Le chemin de l'explorateur",
            [ClientLanguage.Japanese] = "「探究者の歩み」",
        },
        [3] = new()
        {
            [ClientLanguage.English] = "A Teaching of Tactics",
            [ClientLanguage.German] = "Eine Lehre der Taktik",
            [ClientLanguage.French] = "L'enseignement des tactiques",
            [ClientLanguage.Japanese] = "「戦術の指南」",
        },
        [4] = new()
        {
            [ClientLanguage.English] = "A Clash of Echoes",
            [ClientLanguage.German] = "Widerhall des Konflikts",
            [ClientLanguage.French] = "Le choc des échos",
            [ClientLanguage.Japanese] = "「共鳴する衝突」",
        },
        [5] = new()
        {
            [ClientLanguage.English] = "The Crucible of Cooperation",
            [ClientLanguage.German] = "Die Feuerprobe der Eintracht",
            [ClientLanguage.French] = "Le creuset de la coopération",
            [ClientLanguage.Japanese] = "「協調の坩堝」",
        },
        [6] = new()
        {
            [ClientLanguage.English] = "A Test of Rivals",
            [ClientLanguage.German] = "Die Prüfung der Rivalen",
            [ClientLanguage.French] = "L'épreuve des rivaux",
            [ClientLanguage.Japanese] = "「好敵手の試練」",
        },
        [9] = new()
        {
            [ClientLanguage.English] = "A Trial of Fortune",
            [ClientLanguage.German] = "Eine Prüfung des Glücks",
            [ClientLanguage.French] = "L'épreuve de la fortune",
            [ClientLanguage.Japanese] = "「幸運の試練」",
        },
        [21] = new()
        {
            [ClientLanguage.English] = "The Endless Descent",
            [ClientLanguage.German] = "Der endlose Abstieg",
            [ClientLanguage.French] = "La descente infinie",
            [ClientLanguage.Japanese] = "「終わりなき降下」",
        },
        [26] = new()
        {
            [ClientLanguage.English] = "The Elemental Wilderness",
            [ClientLanguage.German] = "Die elementare Wildnis",
            [ClientLanguage.French] = "La contrée élémentaire",
            [ClientLanguage.Japanese] = "「属性の荒野」",
        },
        [27] = new()
        {
            [ClientLanguage.English] = "The Azure Puzzle",
            [ClientLanguage.German] = "Das azurblaue Rätsel",
            [ClientLanguage.French] = "L'énigme azur",
            [ClientLanguage.Japanese] = "「蒼天のパズル」",
        },
        [28] = new()
        {
            [ClientLanguage.English] = "An Ultimate Test of Perfection",
            [ClientLanguage.German] = "Ultimative Prüfung der Vollendung",
            [ClientLanguage.French] = "L'épreuve ultime de la perfection",
            [ClientLanguage.Japanese] = "「完璧たる究極の試練」",
        },
        [29] = new()
        {
            [ClientLanguage.English] = "Remnants of the Blade",
            [ClientLanguage.German] = "Die Überreste der Klinge",
            [ClientLanguage.French] = "Les vestiges de la lame",
            [ClientLanguage.Japanese] = "「女王の遺刃」",
        },
        [30] = new()
        {
            [ClientLanguage.English] = "The Branching Fates",
            [ClientLanguage.German] = "Die verzweigten Schicksale",
            [ClientLanguage.French] = "Les destins ramifiés",
            [ClientLanguage.Japanese] = "「分岐せし宿命」",
        },
        [31] = new()
        {
            [ClientLanguage.English] = "Bounty of the Spectral Tide",
            [ClientLanguage.German] = "Segen des Phantomstroms",
            [ClientLanguage.French] = "Le butin du courant spectral",
            [ClientLanguage.Japanese] = "「幻海流の恵み」",
        },
        [37] = new()
        {
            [ClientLanguage.English] = "A Chaotic Test of Unity",
            [ClientLanguage.German] = "Eine chaotische Prüfung der Einheit",
            [ClientLanguage.French] = "L'épreuve chaotique de l'unité",
            [ClientLanguage.Japanese] = "「混沌たる結束の試練」",
        },
        [38] = new()
        {
            [ClientLanguage.English] = "Shadows of the Crescent",
            [ClientLanguage.German] = "Schatten der Mondsichel",
            [ClientLanguage.French] = "Les ombres du croissant",
            [ClientLanguage.Japanese] = "「三日月の影」",
        },
        [40] = new()
        {
            [ClientLanguage.English] = "The Beast's Gambit",
            [ClientLanguage.German] = "Das Gambit der Bestie",
            [ClientLanguage.French] = "Le gambit bestial",
            [ClientLanguage.Japanese] = "「魔獣の布石」",
        },
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
        if (!Plugin.Config.DutyBannersEnabled) return;
        try
        {
            var cfc = _dutyState.ContentFinderCondition;
            if (!cfc.IsValid) return;
            if (!cfc.Value.ContentType.IsValid) return;

            var contentTypeId = cfc.Value.ContentType.Value.RowId;
            if (!ContentTypeTitles.TryGetValue(contentTypeId, out var titles)) return;

            var clientLanguage = Plugin.ClientState.ClientLanguage;
            var bannerTitle = titles.TryGetValue(clientLanguage, out var title) 
                ? title 
                : titles[ClientLanguage.English];

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
