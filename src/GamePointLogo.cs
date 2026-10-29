using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace GamePointLogo;

public class GamePointLogo : BasePlugin, IPluginConfig<GamePointLogoConfig>
{
    public override string ModuleName => "GamePointLogo";
    public override string ModuleAuthor => "GAMEPOINT.UZ";
    public override string ModuleDescription => "Banner in the center of the screen for dead players and spectators";
    public override string ModuleVersion => GetType().Assembly.GetName().Version?.ToString(3) ?? "dev";

    public GamePointLogoConfig Config { get; set; } = new();

    private bool _roundEnded;
    private float _globalPausedUntil;

    private readonly Dictionary<int, float> _deathTime = new();
    private readonly Dictionary<int, float> _pausedUntil = new();
    private readonly HashSet<int> _visible = new();
    private readonly HashSet<ulong> _disabled = new();

    public void OnConfigParsed(GamePointLogoConfig c)
    {
        c.Display.UpdateInterval = Math.Max(0.25f, c.Display.UpdateInterval);
        c.Display.DelayAfterDeath = Math.Max(0f, c.Display.DelayAfterDeath);
        c.Display.MaxShowTime = Math.Max(0f, c.Display.MaxShowTime);
        c.Compatibility.PauseSeconds = Math.Max(0f, c.Compatibility.PauseSeconds);
        Config = c;
    }

    public override void Load(bool hotReload)
    {
        AddTimer(Config.Display.UpdateInterval, Tick, TimerFlags.REPEAT);
        RegisterCommands();

        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventRoundStart>((_, _) => { _roundEnded = false; return HookResult.Continue; });
        RegisterEventHandler<EventRoundEnd>((_, _) => { _roundEnded = true; return HookResult.Continue; });

        RegisterListener<Listeners.OnMapStart>(_ => ResetState());
        RegisterListener<Listeners.OnClientDisconnect>(ForgetSlot);

        Log($"Loaded v{ModuleVersion}");
    }

    private void Tick()
    {
        var html = BuildHtml();
        var now = Server.CurrentTime;
        var globalOk = Config.Enabled && html != null && now >= _globalPausedUntil && PhaseAllowed();
        var duration = (int)Math.Ceiling(Config.Display.UpdateInterval) + 1;

        foreach (var p in Utilities.GetPlayers())
        {
            if (!IsRealPlayer(p)) continue;

            if (globalOk && ShouldShow(p, now))
            {
                p.PrintToCenterHtml(html!, duration);
                _visible.Add(p.Slot);
            }
            else
            {
                _visible.Remove(p.Slot);
            }
        }
    }

    private static bool IsRealPlayer(CCSPlayerController p) =>
                p.IsValid && !p.IsBot && !p.IsHLTV && (long)p.Connected == 0;

    private bool ShouldShow(CCSPlayerController p, float now)
    {
        var d = Config.Display;

        if (_disabled.Contains(p.SteamID)) return false;
        if (_pausedUntil.TryGetValue(p.Slot, out var until) && now < until) return false;

        foreach (var flag in d.HideForFlags)
            if (!string.IsNullOrWhiteSpace(flag) && AdminManager.PlayerHasPermissions(p, flag)) return false;

        var team = (CsTeam)p.TeamNum;
        if (team == CsTeam.Spectator || team == CsTeam.None) return d.ShowToSpectators;

        if (p.PawnIsAlive || !d.ShowToDead) return false;

        if (_deathTime.TryGetValue(p.Slot, out var died))
        {
            var since = now - died;
            if (since < d.DelayAfterDeath) return false;
            if (d.MaxShowTime > 0 && since > d.DelayAfterDeath + d.MaxShowTime) return false;
        }
        return true;
    }

    private bool PhaseAllowed()
    {
        var d = Config.Display;
        if (_roundEnded && !d.ShowAfterRoundEnd) return false;

        var rules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
            .FirstOrDefault()?.GameRules;
        if (rules == null) return true;

        if (rules.WarmupPeriod && !d.ShowDuringWarmup) return false;
        if (rules.FreezePeriod && !d.ShowDuringFreezeTime) return false;
        return true;
    }

    private string? BuildHtml()
    {
        var c = Config.CenterHtml;
        if (string.IsNullOrWhiteSpace(c.LogoUrl)) return null;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(c.Title)) parts.Add(c.Title);

        var size = c.LogoWidth > 0 ? $" width='{c.LogoWidth}'" : "";
        parts.Add($"<img src='{c.LogoUrl}'{size}>");

        if (!string.IsNullOrWhiteSpace(c.Text)) parts.Add(c.Text);

        return string.Join("<br>", parts);
    }

    private void Hide(CCSPlayerController p, bool clearCenter)
    {
        if (!_visible.Remove(p.Slot)) return;
        if (clearCenter) p.PrintToCenterHtml(" ", 1);
    }

    private HookResult OnPlayerDeath(EventPlayerDeath e, GameEventInfo info)
    {
        var p = e.Userid;
        if (p is { IsValid: true }) _deathTime[p.Slot] = Server.CurrentTime;
        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn e, GameEventInfo info)
    {
        var p = e.Userid;
        if (p is not { IsValid: true } || p.IsBot) return HookResult.Continue;

        _deathTime.Remove(p.Slot);
        Hide(p, clearCenter: true);
        return HookResult.Continue;
    }

    private void ResetState()
    {
        _deathTime.Clear();
        _pausedUntil.Clear();
        _visible.Clear();
        _globalPausedUntil = 0;
        _roundEnded = false;
    }

    private void ForgetSlot(int slot)
    {
        _deathTime.Remove(slot);
        _pausedUntil.Remove(slot);
        _visible.Remove(slot);
    }

    private static bool NotEmpty(string? s) => !string.IsNullOrWhiteSpace(s);

    private void RegisterCommands()
    {
        if (Config.Player.ToggleEnabled)
            foreach (var cmd in Config.Player.ToggleCommands.Where(NotEmpty))
                AddCommand(cmd, "Toggle GAMEPOINT banner", OnToggle);

        var c = Config.Compatibility;
        if (NotEmpty(c.PauseCommand)) AddCommand(c.PauseCommand, "<slot|all> <seconds>", OnExternalPause);
        if (NotEmpty(c.ResumeCommand)) AddCommand(c.ResumeCommand, "<slot|all>", OnExternalResume);

        foreach (var name in c.PauseOnCommands.Where(NotEmpty))
        {
            AddCommandListener("css_" + name.ToLowerInvariant(), (player, _) =>
            {
                if (player is { IsValid: true }) Pause(player, c.PauseSeconds);
                return HookResult.Continue;
            });
        }

        AddCommandListener("say", OnSay);
        AddCommandListener("say_team", OnSay);
    }

    private HookResult OnSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player is not { IsValid: true }) return HookResult.Continue;

        var text = info.ArgString.Trim().Trim('"').Trim();
        if (text.Length < 2 || (text[0] != '!' && text[0] != '/')) return HookResult.Continue;

        var word = text[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        if (Config.Compatibility.PauseOnCommands.Any(c => c.Equals(word, StringComparison.OrdinalIgnoreCase)))
            Pause(player, Config.Compatibility.PauseSeconds);

        return HookResult.Continue;
    }

    private void Pause(CCSPlayerController p, float seconds)
    {
        _pausedUntil[p.Slot] = Server.CurrentTime + seconds;
        Hide(p, clearCenter: false);
        Log($"Pause slot {p.Slot} for {seconds}s");
    }

    private static bool CanUseAdminCommand(CCSPlayerController? caller) =>
        caller == null || AdminManager.PlayerHasPermissions(caller, "@css/root");

    private void OnExternalPause(CCSPlayerController? caller, CommandInfo info)
    {
        if (!CanUseAdminCommand(caller)) return;

        if (info.ArgCount < 3 ||
            !float.TryParse(info.GetArg(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            info.ReplyToCommand($"Usage: {Config.Compatibility.PauseCommand} <slot|all> <seconds>");
            return;
        }

        var target = info.GetArg(1);
        if (target.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            _globalPausedUntil = Server.CurrentTime + seconds;
            _visible.Clear();
        }
        else if (int.TryParse(target, out var slot))
        {
            var p = Utilities.GetPlayerFromSlot(slot);
            if (p is { IsValid: true }) Pause(p, seconds);
        }
    }

    private void OnExternalResume(CCSPlayerController? caller, CommandInfo info)
    {
        if (!CanUseAdminCommand(caller)) return;

        if (info.ArgCount < 2)
        {
            info.ReplyToCommand($"Usage: {Config.Compatibility.ResumeCommand} <slot|all>");
            return;
        }

        var target = info.GetArg(1);
        if (target.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            _globalPausedUntil = 0;
            _pausedUntil.Clear();
        }
        else if (int.TryParse(target, out var slot))
        {
            _pausedUntil.Remove(slot);
        }
    }

    private void OnToggle(CCSPlayerController? player, CommandInfo info)
    {
        if (player is not { IsValid: true }) return;

        if (_disabled.Remove(player.SteamID))
        {
            player.PrintToChat(Config.Player.ChatPrefix + Config.Player.MsgLogoOn);
        }
        else
        {
            _disabled.Add(player.SteamID);
            Hide(player, clearCenter: true);
            player.PrintToChat(Config.Player.ChatPrefix + Config.Player.MsgLogoOff);
        }
    }

    private void Log(string msg)
    {
        if (Config.Debug) Logger.LogInformation("[GamePointLogo] {Msg}", msg);
    }
}
