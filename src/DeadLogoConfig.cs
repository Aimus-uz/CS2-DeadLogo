using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace DeadLogo;

public class CompatibilitySettings
{
    // Если игрок пишет одну из этих команд (!sprays, /ws, css_knife...),
    // логотип для него ставится на паузу, чтобы не мешать меню/HUD других плагинов.
    [JsonPropertyName("PauseOnCommands")]
    public List<string> PauseOnCommands { get; set; } = new() { "sprays", "ws", "knife", "gloves", "menu", "vip" };

    [JsonPropertyName("PauseSeconds")]
    public float PauseSeconds { get; set; } = 15f;

    // Стирать логотип при возрождении (только если он был показан этим плагином)
    [JsonPropertyName("ClearOnSpawn")]
    public bool ClearOnSpawn { get; set; } = true;

    // Серверная команда, через которую другие плагины/админ могут поставить логотип на паузу:
    // css_deadlogo_pause <slot|all> <секунды>
    [JsonPropertyName("ExternalPauseCommand")]
    public string ExternalPauseCommand { get; set; } = "css_deadlogo_pause";
}

public class DeadLogoConfig : BasePluginConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;

    // Прямая https-ссылка на PNG (рекомендуется ~256px, до 200 КБ)
    [JsonPropertyName("LogoUrl")]
    public string LogoUrl { get; set; } = "https://example.com/logo.png";

    // 0 = не указывать размер
    [JsonPropertyName("LogoWidth")]
    public int LogoWidth { get; set; } = 256;

    [JsonPropertyName("LogoHeight")]
    public int LogoHeight { get; set; } = 0;

    // Текст под логотипом, можно HTML: <font color='#F2059F'>IP: 1.2.3.4:27015</font>
    [JsonPropertyName("TextUnderLogo")]
    public string TextUnderLogo { get; set; } = "";

    // Как часто обновлять (сек). Минимум 0.25
    [JsonPropertyName("UpdateInterval")]
    public float UpdateInterval { get; set; } = 1.0f;

    [JsonPropertyName("ShowToDead")]
    public bool ShowToDead { get; set; } = true;

    [JsonPropertyName("ShowToSpectators")]
    public bool ShowToSpectators { get; set; } = true;

    // Через сколько секунд после смерти показать (чтобы не закрывать инфо об убийце)
    [JsonPropertyName("DelayAfterDeath")]
    public float DelayAfterDeath { get; set; } = 3.0f;

    // Сколько секунд показывать после смерти. 0 = до возрождения
    [JsonPropertyName("MaxShowTime")]
    public float MaxShowTime { get; set; } = 0f;

    [JsonPropertyName("ShowDuringWarmup")]
    public bool ShowDuringWarmup { get; set; } = true;

    [JsonPropertyName("ShowDuringFreezeTime")]
    public bool ShowDuringFreezeTime { get; set; } = true;

    [JsonPropertyName("ShowAfterRoundEnd")]
    public bool ShowAfterRoundEnd { get; set; } = true;

    // Игроки с этими флагами не видят логотип (например VIP)
    [JsonPropertyName("HideForFlags")]
    public List<string> HideForFlags { get; set; } = new();

    // Игрок может сам выключить логотип командой
    [JsonPropertyName("PlayerToggleEnabled")]
    public bool PlayerToggleEnabled { get; set; } = true;

    [JsonPropertyName("ToggleCommands")]
    public List<string> ToggleCommands { get; set; } = new() { "css_logo" };

    [JsonPropertyName("ChatPrefix")]
    public string ChatPrefix { get; set; } = "[Server] ";

    [JsonPropertyName("MsgLogoOn")]
    public string MsgLogoOn { get; set; } = "Логотип включён.";

    [JsonPropertyName("MsgLogoOff")]
    public string MsgLogoOff { get; set; } = "Логотип выключен.";

    [JsonPropertyName("Compatibility")]
    public CompatibilitySettings Compatibility { get; set; } = new();

    [JsonPropertyName("Debug")]
    public bool Debug { get; set; } = false;
}
