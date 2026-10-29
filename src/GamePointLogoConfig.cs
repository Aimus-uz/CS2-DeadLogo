using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace GamePointLogo;

public class CenterHtmlSettings
{
    [JsonPropertyName("Title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("LogoUrl")]
    public string LogoUrl { get; set; } = "";

    [JsonPropertyName("LogoWidth")]
    public int LogoWidth { get; set; } = 280;

    [JsonPropertyName("Text")]
    public string Text { get; set; } = "";
}

public class DisplaySettings
{
    [JsonPropertyName("UpdateInterval")]
    public float UpdateInterval { get; set; } = 1.0f;

    [JsonPropertyName("ShowToDead")]
    public bool ShowToDead { get; set; } = true;

    [JsonPropertyName("ShowToSpectators")]
    public bool ShowToSpectators { get; set; } = true;

    [JsonPropertyName("DelayAfterDeath")]
    public float DelayAfterDeath { get; set; } = 3.0f;

    [JsonPropertyName("MaxShowTime")]
    public float MaxShowTime { get; set; } = 0f;

    [JsonPropertyName("ShowDuringWarmup")]
    public bool ShowDuringWarmup { get; set; } = true;

    [JsonPropertyName("ShowDuringFreezeTime")]
    public bool ShowDuringFreezeTime { get; set; } = true;

    [JsonPropertyName("ShowAfterRoundEnd")]
    public bool ShowAfterRoundEnd { get; set; } = true;

    [JsonPropertyName("HideForFlags")]
    public List<string> HideForFlags { get; set; } = new();
}

public class PlayerSettings
{
    [JsonPropertyName("ToggleEnabled")]
    public bool ToggleEnabled { get; set; } = true;

    [JsonPropertyName("ToggleCommands")]
    public List<string> ToggleCommands { get; set; } = new() { "css_logo" };

    [JsonPropertyName("ChatPrefix")]
    public string ChatPrefix { get; set; } = "[GAMEPOINT] ";

    [JsonPropertyName("MsgLogoOn")]
    public string MsgLogoOn { get; set; } = "Логотип включён.";

    [JsonPropertyName("MsgLogoOff")]
    public string MsgLogoOff { get; set; } = "Логотип выключен.";
}

public class CompatibilitySettings
{
    [JsonPropertyName("PauseOnCommands")]
    public List<string> PauseOnCommands { get; set; } = new()
    {
        "info", "help", "skins", "pws", "ws", "knife", "gloves",
        "store", "shop", "bp", "clan", "menu", "rtv"
    };

    [JsonPropertyName("PauseSeconds")]
    public float PauseSeconds { get; set; } = 30f;

    [JsonPropertyName("PauseCommand")]
    public string PauseCommand { get; set; } = "css_gplogo_pause";

    [JsonPropertyName("ResumeCommand")]
    public string ResumeCommand { get; set; } = "css_gplogo_resume";
}

public class GamePointLogoConfig : BasePluginConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("CenterHtml")]
    public CenterHtmlSettings CenterHtml { get; set; } = new();

    [JsonPropertyName("Display")]
    public DisplaySettings Display { get; set; } = new();

    [JsonPropertyName("Player")]
    public PlayerSettings Player { get; set; } = new();

    [JsonPropertyName("Compatibility")]
    public CompatibilitySettings Compatibility { get; set; } = new();

    [JsonPropertyName("Debug")]
    public bool Debug { get; set; } = false;
}
