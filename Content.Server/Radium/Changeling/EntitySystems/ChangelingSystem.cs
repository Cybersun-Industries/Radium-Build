using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Actions;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Radium.Changeling.Components;
using Content.Server.Roles;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Server.Zombies;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Players;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Radium.Changeling;
using Content.Shared.Radium.Changeling.Components;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.StatusEffect;
using Content.Shared.Store.Components;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using LoadoutSystem = Content.Shared.Clothing.LoadoutSystem;

namespace Content.Server.Radium.Changeling.EntitySystems;

public sealed partial class ChangelingSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IServerConsoleHost _console = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly ActorSystem _actors = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string SpawnPointPrototype = "SpawnPointChangeling";

    [ValidatePrototypeId<EntityPrototype>] private const string GenesObjective = "GenesObjectiveChangeling";

    [ValidatePrototypeId<EntityPrototype>] private const string EscapeObjective = "EscapeShuttleObjectiveChangeling";

    [ValidatePrototypeId<JobPrototype>] private const string JobSpawn = "Passenger";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingSpawnerComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ChangelingComponent, ChangelingShopActionEvent>(OnShop);
        SubscribeLocalEvent<ChangelingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChangelingComponent, StatusEffectAddedEvent>(OnStatusAdded);
        SubscribeLocalEvent<ChangelingComponent, StatusEffectEndedEvent>(OnStatusEnded);
        SubscribeLocalEvent<SpawnChangelingEvent>(OnSpawn);
        SubscribeLocalEvent<ChangelingComponent, MobStateChangedEvent>(OnMobStateChange);
        SubscribeLocalEvent<ChangelingSpawnerComponent, GhostRoleSpawnerUsedEvent>(OnGhostRoleSpawnerUsed);

        InitializeAbilities();
    }

    private void OnMapInit(Entity<ChangelingComponent> ent, ref MapInitEvent args)
    {
        var component = ent.Comp;

        foreach (var prototype in component.BaseActions)
        {
            EntityUid? actionUid = EntityUid.Invalid;
            _action.AddAction(ent, ref actionUid, prototype.Key);
            component.BaseActions[prototype.Key] = actionUid!.Value;
        }

        EnsureComp<ZombieImmuneComponent>(ent);

        InitShop(ent);

        _npcFaction.RemoveFaction(ent.Owner, component.NanotrasenFactionId, false);

        RemComp<PacifiedComponent>(ent);
    }

    private void OnMobStateChange(EntityUid uid, ChangelingComponent comp, ref MobStateChangedEvent args)
    {
        switch (args.NewMobState)
        {
            case MobState.Critical or MobState.Alive:
                comp.IsInStasis = false;
                break;

            case MobState.Dead:
                RemoveAllChangelingEquipment(uid, comp);
                break;
        }
    }

    private void OnPlayerAttached(Entity<ChangelingSpawnerComponent> uid, ref PlayerAttachedEvent args)
    {
        QueueLocalEvent(new SpawnChangelingEvent(uid, args.Player));
    }

    private void OnSpawn(SpawnChangelingEvent ev)
    {
        var uid = ev.Entity.Owner;

        var xform = Transform(uid);
        var (changelingMob, pref) = SpawnChangeling(xform.Coordinates);
        var playerData = ev.Session.ContentData();
        if (playerData != null && _mindSystem.TryGetMind(playerData.UserId, out var mindId, out var mind))
        {
            if (!TryComp<ChangelingComponent>(changelingMob, out var component))
                return;

            var briefing = Loc.GetString("changeling-role-greeting");
            _antag.SendBriefing(mind.Session!.AttachedEntity!.Value, briefing, Color.Yellow, component.BriefingSound);

            _mindSystem.TransferTo(mindId.Value, null, true, false, mind);
            RemComp<MindContainerComponent>(changelingMob);
            Timer.Spawn(0,
                () =>
                {
                    _mindSystem.TransferTo(mindId.Value, changelingMob, true, false, mind);
                });
            var station = _stationSystem.GetStations()
                .FirstOrNull(HasComp<StationEventEligibleComponent>);
            {
                var session = _mindSystem.GetSession(Comp<MindComponent>(mindId.Value));
                if (station != null && session != null)
                {
                    RaiseLocalEvent(new PlayerSpawnCompleteEvent(
                        changelingMob,
                        session,
                        "Passenger",
                        false,
                        true,
                        0,
                        station.Value,
                        pref)
                    );
                }

                var changelingRule = EntityQuery<ChangelingRuleComponent>().FirstOrDefault();
                changelingRule?.Changelings.Add((mindId.Value, mind));

                component.Mind = mind;
                mind.PreventGhosting = true;

                var metaData = Comp<MetaDataComponent>(uid);

                var briefingShort = Loc.GetString("changeling-role-greeting-short", ("name", metaData.EntityName));

                if (!_roles.MindHasRole<ChangelingRoleComponent>(mindId.Value))
                {
                    _roles.MindAddRole(mindId.Value,
                        new ChangelingRoleComponent
                            { PrototypeId = component.ChangelingRole });
                    _roles.MindAddRole(mindId.Value,
                        new RoleBriefingComponent
                        {
                            Briefing = briefingShort,
                        });
                }

                _mindSystem.TryAddObjective(mindId.Value, mind, GenesObjective);
                _mindSystem.TryAddObjective(mindId.Value, mind, EscapeObjective);

                if (!_prototype.TryIndex<WeightedRandomPrototype>("TraitorObjectiveGroupSteal", out var prototype))
                    return;

                for (var i = 0; i < 2; i++)
                {
                    _mindSystem.TryAddObjective(mindId.Value, mind, prototype.Pick(_random));
                }
            }
        }

        QueueDel(uid);
    }

    private void InitShop(EntityUid stealerUid)
    {
        var component = EnsureComp<ChangelingComponent>(stealerUid);
        var store = EnsureComp<StoreComponent>(stealerUid);
        foreach (var storeCategory in component.StoreCategories)
        {
            store.Categories.Add(storeCategory);
        }

        store.RefundAllowed = false;
        store.CurrencyWhitelist.Add(component.EvolutionCurrencyPrototype);
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
                { { component.EvolutionCurrencyPrototype, component.Evolution } },
            stealerUid);
    }

    private (EntityUid stealerUid, HumanoidCharacterProfile pref) SpawnChangeling(EntityCoordinates coords)
    {
        var changelingUid = Spawn("MobChangeling", coords);
        var spawnJob = _prototype.Index<JobPrototype>(JobSpawn);

        if (_prototype.TryIndex<StartingGearPrototype>(spawnJob.StartingGear!, out var gear))
        {
            _stationSpawning.EquipStartingGear(changelingUid, gear);
        }

        var pref = HumanoidCharacterProfile.Random();
        // Run loadouts after so stuff like storage loadouts can get
        var jobLoadout = LoadoutSystem.GetJobPrototype(spawnJob.ID);

        if (!_prototype.TryIndex(jobLoadout, out RoleLoadoutPrototype? roleProto))
            return (changelingUid, pref);

        var loadout = new RoleLoadout(jobLoadout);

        loadout.SetDefault(
            pref,
            _actors.GetSession(changelingUid),
            _prototype,
            true);

        // Order loadout selections by the order they appear on the prototype.
        foreach (var group in
                 loadout.SelectedLoadouts.OrderBy(x => roleProto.Groups.FindIndex(e => e == x.Key)))
        {
            foreach (var items in group.Value)
            {
                if (!_prototype.TryIndex(items.Prototype, out var loadoutProto))
                {
                    Log.Error($"Unable to find loadout prototype for {items.Prototype}");
                    continue;
                }

                foreach (var equipment in loadoutProto.Equipment)
                {
                    if (!_prototype.TryIndex(equipment.Value, out var startingGear))
                    {
                        Log.Error(
                            $"Unable to find starting gear {loadoutProto.Equipment} for loadout {loadoutProto}");
                        continue;
                    }

                    // Handle any extra data here.
                    _stationSpawning.EquipStartingGear(changelingUid, startingGear.ID, raiseEvent: false);
                }

            }
        }

        return (changelingUid, pref);
    }

    private void OnStatusAdded(EntityUid uid, ChangelingComponent component, StatusEffectAddedEvent args)
    {
        if (args.Key == "Stun")
            _appearance.SetData(uid, ChangelingVisuals.Slowed, true);
    }

    private void OnStatusEnded(EntityUid uid, ChangelingComponent component, StatusEffectEndedEvent args)
    {
        if (args.Key == "Stun")
            _appearance.SetData(uid, ChangelingVisuals.Slowed, false);
    }

    private void UpdateChemicals(EntityUid uid, float? amount = null, ChangelingComponent? component = null)
    {
        if (!HasComp<ChangelingComponent>(uid)) //Ghosted or etc
        {
            return;
        }

        if (!Resolve(uid, ref component))
            return;

        ref var chemicals = ref component.Chemicals;

        chemicals += amount ?? component.RegenChemicalsAmount /*regen*/;

        component.Chemicals = Math.Clamp(chemicals, 0, component.MaxChemicals);

        Dirty(uid, component);

        _alerts.ShowAlert(uid, component.ChemicalsAlert);
    }

    public void RemoveAllChangelingEquipment(EntityUid target, ChangelingComponent comp)
    {
        foreach (var equipment in comp.ChangelingEquipment)
        {
            EntityManager.DeleteEntity(equipment.Value.Item1);
        }

        PlayMeatySound(target);
    }

    public bool TryUseAbility(EntityUid uid, BaseActionEvent action, ChangelingComponent? comp = null)
    {
        if (action.Handled)
            return false;

        if (!Resolve(uid, ref comp))
            return false;

        if (!TryComp<ChangelingActionComponent>(action.Action, out var lingAction))
            return false;

        if (!lingAction.UseInLesserForm && comp.IsInLesserForm)
        {
            _popup.PopupEntity(Loc.GetString("changeling-action-fail-lesserform"), uid, uid);
            return false;
        }

        var price = lingAction.ChemicalCost;
        if (comp.Chemicals < price)
        {
            _popup.PopupEntity(Loc.GetString("changeling-chemicals-deficit"), uid, uid);
            return false;
        }

        UpdateChemicals(uid, -price);

        action.Handled = true;

        return true;
    }

    private void OnShop(EntityUid uid, ChangelingComponent component, ChangelingShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;
        _store.ToggleUi(uid, uid, store);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        foreach (var comp in EntityManager.EntityQuery<ChangelingComponent>())
        {
            if (_timing.CurTime < comp.RegenTime)
                continue;

            comp.RegenTime = _timing.CurTime + TimeSpan.FromSeconds(comp.RegenCooldown);

            Cycle(comp);
        }
    }

    public bool MakeChangeling([NotNullWhen(true)] out EntityUid? changelingSpawn, EntityUid? uid = null)
    {
        changelingSpawn = null;

        EntityUid? station = null;

        if (uid.HasValue)
        {
            station = _stationSystem.GetOwningStation(uid.Value);
        }

        station ??= _stationSystem.GetStations().FirstOrNull(HasComp<StationEventEligibleComponent>);

        if (station == null || !TryComp<StationDataComponent>(station, out var stationDataComponent))
        {
            return false;
        }

        var spawnGrid = stationDataComponent.Grids.FirstOrNull(HasComp<BecomesStationComponent>);
        if (spawnGrid == null)
        {
            return false;
        }

        var latejoin = (from s in EntityQuery<SpawnPointComponent, TransformComponent>()
            where s.Item1.SpawnType == SpawnPointType.LateJoin && s.Item2.GridUid == spawnGrid
            select s.Item2.Coordinates).ToList();

        if (latejoin.Count == 0)
        {
            return false;
        }

        var coords = _random.Pick(latejoin);
        changelingSpawn = Spawn(SpawnPointPrototype, coords);

        if (uid.HasValue)
        {
            EnsureComp<ChangelingSpawnerComponent>(changelingSpawn.Value).TargetForce = uid.Value;
        }

        return true;
    }

    public sealed class SpawnChangelingEvent(Entity<ChangelingSpawnerComponent> entity, ICommonSession session)
        : EntityEventArgs
    {
        public Entity<ChangelingSpawnerComponent> Entity = entity;
        public ICommonSession Session = session;
    }

    private void OnGhostRoleSpawnerUsed(EntityUid uid,
        ChangelingSpawnerComponent component,
        GhostRoleSpawnerUsedEvent args)
    {
        if (TerminatingOrDeleted(args.Spawner) || EntityManager.IsQueuedForDeletion(args.Spawner))
        {
            return;
        }

        //forward
        if (TryComp<ChangelingSpawnerComponent>(args.Spawner, out var comp))
        {
            component.TargetForce = comp.TargetForce;
        }

        QueueDel(args.Spawner);
    }
}
