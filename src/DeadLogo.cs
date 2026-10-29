using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace DeadLogo;

public class DeadLogo : BasePlugin, IPluginConfig<DeadLogoConfig>
{
    public override string ModuleName => "DeadLogo";
    public override string ModuleAuthor => "YourName";
    public override string ModuleDescription => "Server logo in the center of the screen for dead players and spectators";
    public override string ModuleVersion => GetType().Assembly.GetName().Version?.ToString(3) ?? "dev";

    public DeadLogoConfig Config { get; set; } = new();

    private readonly Dictionary<int, float> _deathTime = new();
    private readonly Dictionary<int, float> _pausedUntil = new();
    private readonly HashSet<int> _shown = new();
    private readonly HashSet<ulong> _disabledBySteamId = new();
    private float _globalPausedUntil;
    private bool _roundEnded;

    public void OnConfigParsed(DeadLogoConfig config)
    {
        if (config.UpdateInterval < 0.25f) config.UpdateInterval = 0.25f;
        if (config.DelayAfterDeath < 0) config.DelayAfterDeath = 0;
        if (config.MaxShowTime < 0) config.MaxShowTime = 0;
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        AddTimer(Config.UpdateInterval, Tick, TimerFlags.REPEAT);

        if (Config.PlayerToggleEnabled)
            foreach (var cmd in Config.ToggleCommands.Where(c => !string.IsNullOrWhiteSpace(c)))
                AddCommand(cmd, "Toggle server logo", OnToggle);

        if (!string.IsNullOrWhiteSpace(Config.Compatibility.ExternalPauseCommand))
            AddCommand(Config.Compatibility.ExternalPauseCommand, "Pause DeadLogo: <slot|all> <seconds>", OnExternalPause);

        foreach (var name in Config.Compatibility.PauseOnCommands.Where(c => !string.IsNullOrWhiteSpace(c)))
        {
            AddCommandListener("css_" + name.ToLowerInvariant(), (player, _) =>
            {
                if (player is { IsValid: true }) Pause(player.Slot, Config.Compatibility.PauseSeconds);
                return HookResult.Continue;
            });
        }

        AddCommandListener("say", OnSay);
        AddCommandListener("say_team", OnSay);

        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventRoundStart>((_, _) => { _roundEnded = false; return HookResult.Continue; });
        RegisterEventHandler<EventRoundEnd>((_, _) => { _roundEnded = true; return HookResult.Continue; });

        RegisterListener<Listeners.OnClientDisconnect>(slot =>
        {
            _deathTime.Remove(slot);
            _pausedUntil.Remove(slot);
            _shown.Remove(slot);
        });
        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            _deathTime.Clear();
            _pausedUntil.Clear();
            _shown.Clear();
            _globalPausedUntil = 0;
            _roundEnded = false;
        });

        Log($"Loaded v{ModuleVersion}, interval {Config.UpdateInterval}s");
    }

    // ---------- main loop ----------

    private void Tick()
    {
        if (!Config.Enabled || string.IsNullOrWhiteSpace(Config.LogoUrl)) return;
        if (!PhaseAllowed()) return;

        var now = Server.CurrentTime;
        if (now < _globalPausedUntil) return;

        var html = BuildHtml();
        var duration = (int)Math.Ceiling(Config.UpdateInterval) + 1;

        foreach (var p in Utilities.GetPlayers())
        {
            if (!ShouldShow(p, now)) continue;
            p.PrintToCenterHtml(html, duration);
            _shown.Add(p.Slot);
        }
    }

    private bool ShouldShow(CCSPlayerController p, float now)
    {
        if (!p.IsValid || p.IsBot || p.IsHLTV) return false;
        if (p.Connected != PlayerConnectedState.PlayerConnected) return false;
        if (_disabledBySteamId.Contains(p.SteamID)) return false;
        if (_pausedUntil.TryGetValue(p.Slot, out var until) && now < until) return false;

        foreach (var flag in Config.HideForFlags)
            if (!string.IsNullOrWhiteSpace(flag) && AdminManager.PlayerHasPermissions(p, flag)) return false;

        var team = (CsTeam)p.TeamNum;
        if (team == CsTeam.Spectator || team == CsTeam.None)
            return Config.ShowToSpectators;

        if (p.PawnIsAlive || !Config.ShowToDead) return false;

        if (_deathTime.TryGetValue(p.Slot, out var died))
        {
            var since = now - died;
            if (since < Config.DelayAfterDeath) return false;
            if (Config.MaxShowTime > 0 && since > Config.DelayAfterDeath + Config.MaxShowTime) return false;
        }
        return true;
    }

    private bool PhaseAllowed()
    {
        if (_roundEnded && !Config.ShowAfterRoundEnd) return false;

        var rules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
            .FirstOrDefault()?.GameRules;
        if (rules == null) return true;

        if (rules.WarmupPeriod && !Config.ShowDuringWarmup) return false;
        if (rules.FreezePeriod && !Config.ShowDuringFreezeTime) return false;
        return true;
    }

    private string BuildHtml()
    {
        var size = "";
        if (Config.LogoWidth > 0) size += $" width='{Config.LogoWidth}'";
        if (Config.LogoHeight > 0) size += $" height='{Config.LogoHeight}'";

        var html = $"<img src='{Config.LogoUrl}'{size}>";
        if (!string.IsNullOrWhiteSpace(Config.TextUnderLogo))
            html += "<br>" + Config.TextUnderLogo;
        return html;
    }

    // ---------- events ----------

    private HookResult OnPlayerDeath(EventPlayerDeath e, GameEventInfo info)
    {
        var p = e.Userid;
        if (p is { IsValid: true }) _deathTime[p.Slot] = Server.CurrentTime;
        return HookResult.Continue;
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn e, GameEventInfo info)
    {
        var p = e.Userid;
        if (p is not { IsValid: true }) return HookResult.Continue;

        _deathTime.Remove(p.Slot);
        // стираем только СВОЙ логотип, чтобы не затирать HUD других плагинов
        if (_shown.Remove(p.Slot) && Config.Compatibility.ClearOnSpawn && !p.IsBot)
            p.PrintToCenterHtml(" ", 1);

        return HookResult.Continue;
    }

    // ---------- compatibility ----------

    private HookResult OnSay(CCSPlayerController? player, CommandInfo info)
    {
        if (player is not { IsValid: true }) return HookResult.Continue;

        var text = info.ArgString.Trim().Trim('"').Trim();
        if (text.Length < 2 || (text[0] != '!' && text[0] != '/')) return HookResult.Continue;

        var word = text[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        if (Config.Compatibility.PauseOnCommands.Any(c => c.Equals(word, StringComparison.OrdinalIgnoreCase)))
            Pause(player.Slot, Config.Compatibility.PauseSeconds);

        return HookResult.Continue;
    }

    private void Pause(int slot, float seconds)
    {
        _pausedUntil[slot] = Server.CurrentTime + seconds;
        _shown.Remove(slot);
        Log($"Paused slot {slot} for {seconds}s");
    }

    private void OnExternalPause(CCSPlayerController? caller, CommandInfo info)
    {
        if (caller != null && !AdminManager.PlayerHasPermissions(caller, "@css/root")) return;

        if (info.ArgCount < 3 ||
            !float.TryParse(info.GetArg(2), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            info.ReplyToCommand($"Usage: {Config.Compatibility.ExternalPauseCommand} <slot|all> <seconds>");
            return;
        }

        var target = info.GetArg(1);
        if (target.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            _globalPausedUntil = Server.CurrentTime + seconds;
            _shown.Clear();
        }
        else if (int.TryParse(target, out var slot))
        {
            Pause(slot, seconds);
        }
    }

    // ---------- player toggle ----------

    private void OnToggle(CCSPlayerController? player, CommandInfo info)
    {
        if (player is not { IsValid: true }) return;

        if (_disabledBySteamId.Remove(player.SteamID))
        {
            player.PrintToChat(Config.ChatPrefix + Config.MsgLogoOn);
        }
        else
        {
            _disabledBySteamId.Add(player.SteamID);
            if (_shown.Remove(player.Slot)) player.PrintToCenterHtml(" ", 1);
            player.PrintToChat(Config.ChatPrefix + Config.MsgLogoOff);
        }
    }

    private void Log(string msg)
    {
        if (Config.Debug) Logger.LogInformation("[DeadLogo] {Msg}", msg);
    }
}
