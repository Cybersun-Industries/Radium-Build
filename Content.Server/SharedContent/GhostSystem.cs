using System.Linq;
using Content.Corvax.Interfaces.Server;
using Content.Server.GameTicking;
using Content.Shared.Backmen.GhostTheme;
using Content.Shared.Ghost;
using Content.Shared.Radium.Events;
using Robust.Server.Configuration;
using Robust.Server.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.SharedContent;

public sealed class GhostThemeSystem : EntitySystem
{
    [Dependency] private readonly IServerSponsorsManager _sponsorsMgr = default!; // Corvax-Sponsors
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly IServerNetConfigurationManager _netConfigManager = default!;
    [Dependency] private readonly IServerConsoleHost _console = default!;

    private readonly Dictionary<string, bool> _respawnUsedDictionary = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
        SubscribeNetworkEvent<RespawnRequestEvent>(OnGhostRespawnRequest);
    }

    private void OnGameRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New == GameRunLevel.PreRoundLobby)
        {
            _respawnUsedDictionary.Clear();
        }
    }

    private void OnGhostRespawnRequest(RespawnRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null ||
            !TryComp<GhostComponent>(args.SenderSession.AttachedEntity.Value, out var ghostComponent))
            return;
        if (_respawnUsedDictionary.ContainsKey(args.SenderSession.UserId.UserId.ToString()))
        {
            return;
        }

        if (ghostComponent.TimeOfDeath.CompareTo(ghostComponent.TimeOfDeath.Add(TimeSpan.FromMinutes(15))) is -1 or 0)
            _console.ExecuteCommand($"respawn {args.SenderSession.Name}");
        _respawnUsedDictionary.Add(args.SenderSession.UserId.UserId.ToString(), true);
    }

    private void OnPlayerAttached(EntityUid uid, GhostComponent component, PlayerAttachedEvent args)
    {
        if (TryComp<GhostComponent>(uid, out var ghostComponent))
        {
            RaiseNetworkEvent(new SyncTimeEvent(
                (int) Math.Round(900 -
                                 ghostComponent.TimeOfDeath.TotalSeconds),
                !_respawnUsedDictionary.ContainsKey(args.Player.UserId.UserId.ToString())));
        }

        var prefGhost =
            _netConfigManager.GetClientCVar(args.Player.Channel, Shared.Backmen.CCVar.CCVars.SponsorsSelectedGhost);
        {
#if DEBUG
            if (!_sponsorsMgr.TryGetPrototypes(args.Player.UserId, out var items))
            {
                items = new List<string>();
                items.Add("tier1");
                items.Add("tier2");
                items.Add("tier01");
                items.Add("tier02");
                items.Add("tier03");
                items.Add("tier04");
                items.Add("tier05");
                items.Add("tier6");
                items.Add("tier7");
                items.Add("tier8");
                items.Add("tier9");
                items.Add("tier10");
                items.Add("tier11");
                items.Add("tier12");
                items.Add("tier13");
                items.Add("tier14");
                items.Add("tier15");
                items.Add("tier16");
                items.Add("tier17");
                items.Add("tier18");
                items.Add("tier19");
                items.Add("tier20");
                items.Add("tier21");
                items.Add("tier22");
                items.Add("tier23");
            }

            if (!items.Contains(prefGhost))
            {
                prefGhost = "";
            }
#else
            if (!_sponsorsMgr.TryGetPrototypes(args.Player.UserId, out var items) || !items.Contains(prefGhost))
            {
                prefGhost = "";
            }
#endif
        }

        GhostThemePrototype? ghostThemePrototype;
        if (string.IsNullOrEmpty(prefGhost) ||
            !_prototypeManager.TryIndex(prefGhost, out ghostThemePrototype))
        {
            if (!_sponsorsMgr.TryGetGhostTheme(args.Player.UserId, out var ghostTheme) ||
                !_prototypeManager.TryIndex(ghostTheme, out ghostThemePrototype)
               )
            {
                return;
            }
        }

        foreach (var comp in ghostThemePrototype.Components.Values.Select(entry =>
                     (Component) _serialization.CreateCopy(entry.Component, notNullableOverride: true)))
        {
            comp.Owner = uid;
            EntityManager.AddComponent(uid, comp);
        }

        EnsureComp<GhostThemeComponent>(uid).GhostTheme = ghostThemePrototype.ID;
    }
}
