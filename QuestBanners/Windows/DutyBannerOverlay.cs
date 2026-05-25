using System;
using System.IO;
using System.Numerics;
using NAudio.Wave;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Windowing;

namespace QuestBanners.Windows;

public sealed class DutyBannerOverlay : Window, IDisposable
{
    private const float FadeInDuration  = 1.2f;
    private const float HoldDuration    = 2.5f;
    private const float FadeOutDuration = 0.9f;
    private const float TotalDuration   = FadeInDuration + HoldDuration + FadeOutDuration;

    private const float ScaleStart = 0.95f;

    private const float TitleFontSz    = 68f;
    private const float SubtitleFontSz = 34f;

    private string _titleText    = "A Major Test of Strength";
    private string _subtitleText = string.Empty;
    private float  _elapsed      = TotalDuration;

    private readonly string      _soundPath;
    private WaveOutEvent?        _waveOut;
    private AudioFileReader?     _audioReader;

    private readonly IFontHandle _titleFont;
    private readonly IFontHandle _subtitleFont;

    private static readonly Vector4 BotW_ColTitle     = new(1.00f, 1.00f, 1.00f, 1.00f);
    private static readonly Vector4 BotW_ColSubtitle  = new(0.82f, 0.93f, 1.00f, 1.00f);
    private static readonly Vector4 BotW_ColGlow      = new(0.55f, 0.82f, 1.00f, 1.00f);
    private static readonly Vector4 BotW_ColSeparator = new(0.65f, 0.88f, 1.00f, 1.00f);

    private static readonly Vector4 TotK_ColTitle     = new(0.72f, 1.00f, 0.82f, 1.00f);
    private static readonly Vector4 TotK_ColSubtitle  = new(0.65f, 0.95f, 0.75f, 1.00f);
    private static readonly Vector4 TotK_ColGlow      = new(0.20f, 0.85f, 0.50f, 1.00f);
    private static readonly Vector4 TotK_ColSeparator = new(0.40f, 0.90f, 0.60f, 1.00f);

    public DutyBannerOverlay() : base(
        "###BotwDutyBannerOverlay",
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

        var dir      = Plugin.PluginInterface.AssemblyLocation.Directory!.FullName;
        var hyliaPath  = Path.Combine(dir, "assets", "fonts", "HyliaSerifBeta-Regular.otf");
        var regularPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segoeui.ttf");
        _soundPath = Path.Combine(dir, "assets", "audio", "shrinestart2.mp3");

        var atlas = Plugin.PluginInterface.UiBuilder.FontAtlas;

        _titleFont = atlas.NewDelegateFontHandle(e => e.OnPreBuild(tk =>
        {
            var cfg = new SafeFontConfig { SizePx = TitleFontSz, OversampleH = 3, OversampleV = 3 };
            tk.AddFontFromFile(hyliaPath, in cfg);
        }));

        _subtitleFont = atlas.NewDelegateFontHandle(e => e.OnPreBuild(tk =>
        {
            var cfg = new SafeFontConfig { SizePx = SubtitleFontSz, OversampleH = 3, OversampleV = 3 };
            tk.AddFontFromFile(regularPath, in cfg);
        }));
    }

    public void ShowBanner(string bannerTitle, string dutyName)
    {
        _titleText    = bannerTitle;
        _subtitleText = dutyName;
        _elapsed      = 0f;
        Plugin.Log.Debug($"[DutyBanner] ShowBanner: '{bannerTitle}' / '{dutyName}'");
        PlaySound();
    }

    private void PlaySound()
    {
        try
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _audioReader?.Dispose();

            _audioReader = new AudioFileReader(_soundPath);
            _waveOut     = new WaveOutEvent();
            _waveOut.Init(_audioReader);
            _waveOut.Play();
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, $"[DutyBanner] Failed to play sound: {_soundPath}");
        }
    }

    public void Dispose()
    {
        _titleFont.Dispose();
        _subtitleFont.Dispose();
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
        if (_elapsed >= TotalDuration) return;

        float alpha;
        float scaleT;
        if (_elapsed < FadeInDuration)
        {
            alpha  = _elapsed / FadeInDuration;
            scaleT = alpha;
        }
        else if (_elapsed < FadeInDuration + HoldDuration)
        {
            alpha  = 1f;
            scaleT = 1f;
        }
        else
        {
            alpha  = 1f - (_elapsed - FadeInDuration - HoldDuration) / FadeOutDuration;
            scaleT = 1f;
        }
        alpha  = Math.Clamp(alpha,  0f, 1f);
        float scale = ScaleStart + (1f - ScaleStart) * EaseInOut(scaleT);

        var io = ImGui.GetIO();
        float sw = io.DisplaySize.X;
        float sh = io.DisplaySize.Y;

        var dl = ImGui.GetForegroundDrawList();

        float titleSz    = TitleFontSz    * scale;
        float subtitleSz = SubtitleFontSz * scale;

        ImFontPtr titleFontPtr    = default;
        ImFontPtr subtitleFontPtr = default;

        bool titleReady    = _titleFont.Available;
        bool subtitleReady = _subtitleFont.Available;

        if (titleReady)
            using (_titleFont.Push())
                titleFontPtr = ImGui.GetFont();

        if (subtitleReady)
            using (_subtitleFont.Push())
                subtitleFontPtr = ImGui.GetFont();

        Vector2 titleSize = titleReady
            ? ImGui.CalcTextSizeA(titleFontPtr, titleSz, float.MaxValue, 0f, _titleText, out _)
            : ImGui.CalcTextSize(_titleText);

        Vector2 subtitleSize = subtitleReady
            ? ImGui.CalcTextSizeA(subtitleFontPtr, subtitleSz, float.MaxValue, 0f, _subtitleText, out _)
            : ImGui.CalcTextSize(_subtitleText);

        bool isTotK = Plugin.Config.Theme == BannerTheme.TotK;

        Vector4 colTitle     = isTotK ? TotK_ColTitle     : BotW_ColTitle;
        Vector4 colSubtitle  = isTotK ? TotK_ColSubtitle  : BotW_ColSubtitle;
        Vector4 colGlow      = isTotK ? TotK_ColGlow      : BotW_ColGlow;
        Vector4 colSeparator = isTotK ? TotK_ColSeparator : BotW_ColSeparator;

        const float separatorGapV = 10f;
        const float ornGap        = 10f;
        const float lineLen       = 40f;
        const float cirR          = 4f;
        const float ornTotalW     = lineLen + cirR * 2f + ornGap;

        float subtitleRowW = ornTotalW * 2f + subtitleSize.X;

        float totalH   = titleSz + separatorGapV + subtitleSz;
        float blockTop = sh * 0.28f - totalH * 0.5f;

        float titleX       = (sw - titleSize.X) * 0.5f;
        float titleY       = blockTop;
        float subtitleRowX = (sw - subtitleRowW) * 0.5f;
        float subtitleY    = titleY + titleSz + separatorGapV;
        float subtitleX    = subtitleRowX + ornTotalW;
        float midY         = subtitleY + subtitleSz * 0.5f;

        if (titleReady)
        {
            DrawGlow(dl, titleFontPtr, titleSz, _titleText, titleX, titleY, alpha, colGlow);
            dl.AddText(titleFontPtr, titleSz, new Vector2(titleX, titleY),
                       ColorU32(colTitle, alpha), _titleText);
        }
        else
        {
            dl.AddText(new Vector2(titleX, titleY), ColorU32(colTitle, alpha), _titleText);
        }

        if (subtitleReady)
        {
            DrawGlow(dl, subtitleFontPtr, subtitleSz, _subtitleText, subtitleX, subtitleY, alpha * 0.55f, colGlow);
            dl.AddText(subtitleFontPtr, subtitleSz, new Vector2(subtitleX, subtitleY),
                       ColorU32(colSubtitle, alpha), _subtitleText);
        }
        else
        {
            dl.AddText(new Vector2((sw - subtitleSize.X) * 0.5f, subtitleY), ColorU32(colSubtitle, alpha), _subtitleText);
        }

        if (isTotK)
            DrawTotKOrnaments(dl, subtitleRowX, subtitleX, subtitleSize.X, midY, lineLen, ornGap, alpha, colSeparator);
        else
            DrawBotWOrnaments(dl, subtitleRowX, subtitleX, subtitleSize.X, midY, lineLen, cirR, ornGap, alpha, colSeparator);
    }

    private static void DrawBotWOrnaments(ImDrawListPtr dl,
        float rowX, float subtitleX, float subtitleW,
        float midY, float lineLen, float cirR, float ornGap,
        float alpha, Vector4 col)
    {
        uint c = ColorU32(col, alpha * 0.90f);

        float leftCircX  = rowX + lineLen + cirR;
        dl.AddLine(new Vector2(rowX, midY), new Vector2(leftCircX - cirR, midY), c, 1.5f);
        dl.AddCircleFilled(new Vector2(leftCircX, midY), cirR, c);
        dl.AddCircle(new Vector2(leftCircX, midY), cirR + 2.5f, c, 0, 1f);

        float rightCircX = subtitleX + subtitleW + ornGap + cirR;
        dl.AddCircleFilled(new Vector2(rightCircX, midY), cirR, c);
        dl.AddCircle(new Vector2(rightCircX, midY), cirR + 2.5f, c, 0, 1f);
        dl.AddLine(new Vector2(rightCircX + cirR, midY), new Vector2(rightCircX + cirR + lineLen, midY), c, 1.5f);
    }

    private static void DrawTotKOrnaments(ImDrawListPtr dl,
        float rowX, float subtitleX, float subtitleW,
        float midY, float lineLen, float ornGap,
        float alpha, Vector4 col)
    {
        uint c    = ColorU32(col, alpha * 0.85f);
        uint cDim = ColorU32(col, alpha * 0.45f);

        const float dSize  = 3.5f;
        const float dStep  = 11f;
        int   count = (int)(lineLen / dStep);

        float leftEndX = subtitleX - ornGap;
        for (int i = 0; i < count; i++)
        {
            float cx = leftEndX - i * dStep;
            DrawDiamond(dl, cx, midY, dSize, i == 0 ? c : cDim);
            if (i < count - 1)
                dl.AddLine(new Vector2(cx - dSize, midY), new Vector2(cx - dStep + dSize, midY), cDim, 1f);
        }

        float rightStartX = subtitleX + subtitleW + ornGap;
        for (int i = 0; i < count; i++)
        {
            float cx = rightStartX + i * dStep;
            DrawDiamond(dl, cx, midY, dSize, i == 0 ? c : cDim);
            if (i < count - 1)
                dl.AddLine(new Vector2(cx + dSize, midY), new Vector2(cx + dStep - dSize, midY), cDim, 1f);
        }
    }

    private static void DrawDiamond(ImDrawListPtr dl, float cx, float cy, float half, uint col)
    {
        var p0 = new Vector2(cx,        cy - half);
        var p1 = new Vector2(cx + half, cy);
        var p2 = new Vector2(cx,        cy + half);
        var p3 = new Vector2(cx - half, cy);
        dl.AddQuadFilled(p0, p1, p2, p3, col);
    }

    private static void DrawGlow(ImDrawListPtr dl, ImFontPtr font, float fontSize, string text, float x, float y, float alpha, Vector4 glowColor)
    {
        Span<(float radius, float glowAlpha)> passes =
        [
            (18f, 0.012f),
            (12f, 0.025f),
            (8f,  0.05f),
            (5f,  0.09f),
            (3f,  0.14f),
            (1.5f, 0.20f),
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
