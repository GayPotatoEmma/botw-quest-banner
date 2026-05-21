using System;
using System.IO;
using System.Numerics;
using NAudio.Wave;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;

namespace FantasyOfTheWild.Windows;

public sealed class QuestBannerOverlay : Window, IDisposable
{
    private const float WipeInDuration       = 0.55f;
    private const float HoldPhase1Duration   = 1.2f;
    private const float CompleteFadeInDur    = 0.45f;
    private const float HoldPhase2Duration   = 2.0f;
    private const float FadeOutDuration      = 0.5f;
    private const float TotalDuration        = WipeInDuration + HoldPhase1Duration + CompleteFadeInDur + HoldPhase2Duration + FadeOutDuration;
    private const float AcceptedHoldDuration = 2.8f;
    private const float AcceptedTotal        = WipeInDuration + AcceptedHoldDuration + FadeOutDuration;

    private const float BannerH        = 88f;
    private const float TitleFontSz    = 62f;
    private const float CompleteFontSz = 42f;
    private const float CategoryIconSz = 20f;
    private const float CategoryFontSz = 16f;

    private readonly record struct ThemeData(
        Vector4 Bg,
        Vector4 Accent,
        Vector4 Title,
        Vector4 Complete,
        Vector4 CategoryText);

    private static readonly ThemeData ThemeBotW = new(
        Bg:           new(0.94f, 0.91f, 0.82f, 0.45f),
        Accent:       new(0.62f, 0.52f, 0.28f, 1.00f),
        Title:        new(0.10f, 0.08f, 0.02f, 1.00f),
        Complete:     new(0.98f, 0.96f, 0.78f, 1.00f),
        CategoryText: new(1.00f, 1.00f, 1.00f, 1.00f));

    private static readonly ThemeData ThemeTotK = new(
        Bg:           new(0.00f, 0.00f, 0.00f, 0.00f),
        Accent:       new(0.08f, 0.06f, 0.02f, 1.00f),
        Title:        new(0.08f, 0.06f, 0.02f, 1.00f),
        Complete:     new(0.98f, 0.96f, 0.78f, 1.00f),
        CategoryText: new(0.08f, 0.06f, 0.02f, 1.00f));

    private static ThemeData ActiveTheme => Plugin.Config.Theme == BannerTheme.TotK ? ThemeTotK : ThemeBotW;

    private readonly IFontHandle _titleFont;
    private readonly IFontHandle _completeFont;
    private readonly IFontHandle _categoryFont;
    private readonly IFontHandle _categoryFontRegular;

    private readonly string _acceptedSoundPath;
    private readonly string _completeSoundPath;
    private WaveOutEvent?    _waveOut;
    private AudioFileReader? _audioReader;

    private string     _questName              = string.Empty;
    private string?    _questCategory          = null;
    private uint       _questCategoryIconId    = 0;
    private BannerType _bannerType             = BannerType.Accepted;
    private float      _elapsed                = TotalDuration;
    private bool       _completePhaseTriggered = false;

    public QuestBannerOverlay() : base(
        "###BotwQuestBannerOverlay",
        ImGuiWindowFlags.NoDecoration         |
        ImGuiWindowFlags.NoNav                |
        ImGuiWindowFlags.NoMove               |
        ImGuiWindowFlags.NoInputs             |
        ImGuiWindowFlags.NoBackground         |
        ImGuiWindowFlags.NoBringToFrontOnFocus,
        forceMainWindow: true)
    {
        IsOpen = true;
        RespectCloseHotkey = false;

        var dir = Plugin.PluginInterface.AssemblyLocation.Directory!.FullName;
        var fontPath = Path.Combine(dir, "HyliaSerifBeta-Regular.otf");
        _acceptedSoundPath = Path.Combine(dir, "assets", "questbanner.mp3");
        _completeSoundPath = Path.Combine(dir, "assets", "questcomplete.mp3");

        var atlas = Plugin.PluginInterface.UiBuilder.FontAtlas;

        _titleFont = atlas.NewDelegateFontHandle(e => e.OnPreBuild(tk =>
        {
            var cfg = new SafeFontConfig { SizePx = TitleFontSz, OversampleH = 3, OversampleV = 3 };
            tk.AddFontFromFile(fontPath, ref cfg);
        }));

        _completeFont = atlas.NewDelegateFontHandle(e => e.OnPreBuild(tk =>
        {
            var cfg = new SafeFontConfig { SizePx = CompleteFontSz, OversampleH = 3, OversampleV = 3 };
            tk.AddFontFromFile(fontPath, ref cfg);
        }));

        var italicFontPath   = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeuii.ttf");
        var regularFontPath  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf");
        _categoryFont = atlas.NewDelegateFontHandle(e => e.OnPreBuild(tk =>
        {
            var cfg = new SafeFontConfig { SizePx = CategoryFontSz, OversampleH = 3, OversampleV = 3 };
            tk.AddFontFromFile(italicFontPath, ref cfg);
        }));
        _categoryFontRegular = atlas.NewDelegateFontHandle(e => e.OnPreBuild(tk =>
        {
            var cfg = new SafeFontConfig { SizePx = CategoryFontSz, OversampleH = 3, OversampleV = 3 };
            tk.AddFontFromFile(regularFontPath, ref cfg);
        }));
    }

    public void ShowBanner(string questName, BannerType type, string? category = null, uint categoryIconId = 0)
    {
        _questName              = questName;
        _questCategory          = category;
        _questCategoryIconId    = categoryIconId;
        _bannerType             = type;
        _elapsed                = 0f;
        _completePhaseTriggered = false;
        Plugin.Log.Debug($"[QuestBanner] ShowBanner: '{questName}' ({type})");
        PlaySound(_acceptedSoundPath);
    }

    private void PlaySound(string path)
    {
        try
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _audioReader?.Dispose();

            _audioReader = new AudioFileReader(path);
            _waveOut     = new WaveOutEvent();
            _waveOut.Init(_audioReader);
            _waveOut.Play();
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, $"[QuestBanner] Failed to play sound: {path}");
        }
    }

    public void Dispose()
    {
        _titleFont.Dispose();
        _completeFont.Dispose();
        _categoryFont.Dispose();
        _categoryFontRegular.Dispose();
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _audioReader?.Dispose();
    }

    public override void PreDraw()
    {
        var io = ImGui.GetIO();
        ImGui.SetNextWindowSize(io.DisplaySize);
        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowBgAlpha(0f);
    }

    public override void Draw()
    {
        _elapsed += ImGui.GetIO().DeltaTime;

        float totalDur = _bannerType == BannerType.Complete ? TotalDuration : AcceptedTotal;
        if (_elapsed >= totalDur) return;

        float wipeProgress, bannerAlpha, completeAlpha;

        if (_bannerType == BannerType.Complete)
        {
            if (_elapsed < WipeInDuration)
            {
                wipeProgress  = _elapsed / WipeInDuration;
                bannerAlpha   = 1f;
                completeAlpha = 0f;
            }
            else if (_elapsed < WipeInDuration + HoldPhase1Duration)
            {
                wipeProgress  = 1f;
                bannerAlpha   = 1f;
                completeAlpha = 0f;
            }
            else if (_elapsed < WipeInDuration + HoldPhase1Duration + CompleteFadeInDur)
            {
                if (!_completePhaseTriggered)
                {
                    _completePhaseTriggered = true;
                    PlaySound(_completeSoundPath);
                }
                float t       = (_elapsed - WipeInDuration - HoldPhase1Duration) / CompleteFadeInDur;
                wipeProgress  = 1f;
                bannerAlpha   = 1f;
                completeAlpha = Math.Clamp(t, 0f, 1f);
            }
            else if (_elapsed < WipeInDuration + HoldPhase1Duration + CompleteFadeInDur + HoldPhase2Duration)
            {
                wipeProgress  = 1f;
                bannerAlpha   = 1f;
                completeAlpha = 1f;
            }
            else
            {
                float t       = (_elapsed - WipeInDuration - HoldPhase1Duration - CompleteFadeInDur - HoldPhase2Duration) / FadeOutDuration;
                float fade    = 1f - Math.Clamp(t, 0f, 1f);
                wipeProgress  = 1f;
                bannerAlpha   = fade;
                completeAlpha = fade;
            }
        }
        else
        {
            completeAlpha = 0f;
            if (_elapsed < WipeInDuration)
            {
                wipeProgress = _elapsed / WipeInDuration;
                bannerAlpha  = 1f;
            }
            else if (_elapsed < WipeInDuration + AcceptedHoldDuration)
            {
                wipeProgress = 1f;
                bannerAlpha  = 1f;
            }
            else
            {
                float t      = (_elapsed - WipeInDuration - AcceptedHoldDuration) / FadeOutDuration;
                wipeProgress = 1f;
                bannerAlpha  = 1f - Math.Clamp(t, 0f, 1f);
            }
        }

        wipeProgress = EaseInOut(wipeProgress);

        var io   = ImGui.GetIO();
        float sw = io.DisplaySize.X;
        float sh = io.DisplaySize.Y;

        float bannerY = sh * 0.22f;
        var p0 = new Vector2(0f,  bannerY);
        var p1 = new Vector2(sw, bannerY + BannerH);

        float clipLeft = p1.X - sw * wipeProgress;
        var dl = ImGui.GetForegroundDrawList();

        var theme = ActiveTheme;

        dl.PushClipRect(new Vector2(clipLeft, p0.Y), p1, true);

        bool isTotK = Plugin.Config.Theme == BannerTheme.TotK;

        if (!isTotK)
        {
            dl.AddRectFilled(p0, p1, ColorU32(theme.Bg, bannerAlpha));

            const float lineThick = 2.5f;
            dl.AddLine(p0,                            new Vector2(p1.X, p0.Y),      ColorU32(theme.Accent, bannerAlpha), lineThick);
            dl.AddLine(new Vector2(p0.X, p1.Y - 1f),  new Vector2(p1.X, p1.Y - 1f), ColorU32(theme.Accent, bannerAlpha), lineThick);
        }

        var (titleX, titleY, titleWidth) = DrawTitleText(dl, p0, p1, sw, bannerAlpha, theme, isTotK);

        dl.PopClipRect();

        if (_questCategory != null)
            DrawCategoryStrip(dl, p0, sw, wipeProgress, bannerAlpha, theme, isTotK);

        if (completeAlpha > 0f)
            DrawCompleteText(dl, p0, p1, sw, completeAlpha, theme, isTotK, titleX, titleY, titleWidth);
    }

    private void DrawCategoryStrip(ImDrawListPtr dl, Vector2 bannerTopLeft, float sw, float wipeProgress, float alpha, ThemeData theme, bool isTotK = false)
    {
        if (_questCategory == null) return;

        const float iconSz   = CategoryIconSz;
        const float gap      = 5f;
        const float totalH   = iconSz;
        const float aboveGap = 6f;

        IFontHandle activeCategoryFont = isTotK ? _categoryFontRegular : _categoryFont;

        ImFontPtr fontPtr = default;
        bool fontReady = activeCategoryFont.Available;
        if (fontReady)
            using (activeCategoryFont.Push())
                fontPtr = ImGui.GetFont();

        Vector2 textSize = fontReady
            ? ImGui.CalcTextSizeA(fontPtr, CategoryFontSz, float.MaxValue, 0f, _questCategory, out _)
            : ImGui.CalcTextSize(_questCategory);

        var iconTex = _questCategoryIconId > 0
            ? Plugin.TextureProvider.GetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup(_questCategoryIconId)).GetWrapOrDefault()
            : null;

        float totalW = (iconTex != null ? iconSz + gap : 0f) + textSize.X;

        float blockX;
        if (isTotK && _titleFont.Available)
        {
            ImFontPtr titleFontPtr;
            using (_titleFont.Push())
                titleFontPtr = ImGui.GetFont();
            var titleSize = ImGui.CalcTextSizeA(titleFontPtr, TitleFontSz, float.MaxValue, 0f, _questName, out _);
            blockX = (sw - titleSize.X) * 0.5f;
        }
        else
        {
            blockX = sw * 0.5f - totalW * 0.5f;
        }
        float blockY = bannerTopLeft.Y - aboveGap - totalH;

        float clipLeft = sw - sw * wipeProgress;
        dl.PushClipRect(new Vector2(clipLeft, blockY - 2f), new Vector2(sw, bannerTopLeft.Y), true);

        float curX = blockX;

        if (iconTex != null)
        {
            var iconMin = new Vector2(curX, blockY);
            var iconMax = new Vector2(curX + iconSz, blockY + iconSz);
            dl.AddImage(iconTex.Handle, iconMin, iconMax,
                        Vector2.Zero, Vector2.One,
                        ColorU32(Vector4.One, alpha));
            curX += iconSz + gap;
        }

        float textY = blockY + (totalH - textSize.Y) * 0.5f;
        if (fontReady)
        {
            if (isTotK)
                DrawGlow(dl, fontPtr, CategoryFontSz, _questCategory, curX, textY, alpha);
            dl.AddText(fontPtr, CategoryFontSz, new Vector2(curX, textY), ColorU32(theme.CategoryText, alpha), _questCategory);
        }
        else
            dl.AddText(new Vector2(curX, textY), ColorU32(theme.CategoryText, alpha), _questCategory);

        dl.PopClipRect();
    }

    private (float titleX, float titleY, float titleWidth) DrawTitleText(ImDrawListPtr dl, Vector2 p0, Vector2 p1, float sw, float alpha, ThemeData theme, bool isTotK = false)
    {
        if (!_titleFont.Available) return (0f, 0f, 0f);

        ImFontPtr titleFontPtr;
        using (_titleFont.Push())
            titleFontPtr = ImGui.GetFont();

        int remaining = 0;
        var titleSize = ImGui.CalcTextSizeA(titleFontPtr, TitleFontSz, float.MaxValue, 0f, _questName, out remaining);
        float titleX  = (sw - titleSize.X) * 0.5f;
        float titleY  = p0.Y + (BannerH - TitleFontSz) * 0.5f;

        if (isTotK)
        {
            const float lineThick = 2f;
            float lineY = titleY - 4f;
            dl.AddLine(new Vector2(titleX - 10f, lineY), new Vector2(sw, lineY), ColorU32(theme.Accent, alpha), lineThick);

            DrawGlow(dl, titleFontPtr, TitleFontSz, _questName, titleX, titleY, alpha);
        }

        dl.AddText(titleFontPtr, TitleFontSz, new Vector2(titleX, titleY), ColorU32(theme.Title, alpha), _questName);
        return (titleX, titleY, titleSize.X);
    }

    private void DrawCompleteText(ImDrawListPtr dl, Vector2 p0, Vector2 p1, float sw, float alpha, ThemeData theme, bool isTotK, float titleX, float titleY, float titleWidth)
    {
        if (!_completeFont.Available) return;

        ImFontPtr completeFontPtr;
        using (_completeFont.Push())
            completeFontPtr = ImGui.GetFont();

        const string completeText = "Complete";
        int cRemaining = 0;
        var cSize = ImGui.CalcTextSizeA(completeFontPtr, CompleteFontSz, float.MaxValue, 0f, completeText, out cRemaining);

        float cX, cY;
        if (isTotK && titleWidth > 0f)
        {
            // Overlap the bottom-right of the quest title, like in TotK
            cX = titleX + titleWidth - cSize.X * 0.15f;
            cY = titleY + TitleFontSz - cSize.Y * 0.85f;
        }
        else
        {
            cX = sw * 0.65f;
            cY = p1.Y - cSize.Y * 0.5f;
        }

        dl.AddText(completeFontPtr, CompleteFontSz, new Vector2(cX, cY), ColorU32(theme.Complete, alpha), completeText);
    }

    private static void DrawGlow(ImDrawListPtr dl, ImFontPtr font, float fontSize, string text, float x, float y, float alpha)
    {
        var glowColor = new Vector4(1.00f, 0.97f, 0.88f, 1.00f);
        Span<(float radius, float glowAlpha)> passes = [
            (7f, 0.03f),
            (5f, 0.05f),
            (3f, 0.08f),
            (1.5f, 0.13f),
        ];
        foreach (var (radius, glowAlpha) in passes)
        {
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                dl.AddText(font, fontSize,
                           new Vector2(x + dx * radius, y + dy * radius),
                           ColorU32(glowColor, alpha * glowAlpha),
                           text);
            }
        }
    }

    private static float EaseInOut(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;
    }

    private static uint ColorU32(Vector4 col, float alpha)
    {
        byte r = (byte)(col.X * 255f);
        byte g = (byte)(col.Y * 255f);
        byte b = (byte)(col.Z * 255f);
        byte a = (byte)(col.W * alpha * 255f);
        return (uint)((a << 24) | (b << 16) | (g << 8) | r);
    }
}
