using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Server.Actions;
using Content.Server.Backmen.Cloning;
using Content.Server.Backmen.EvilTwin;
using Content.Server.Backmen.Fugitive;
using Content.Server.Backmen.Loadout;
using Content.Server.DetailExaminable;
using Content.Server.Forensics;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.Shuttles.Components;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Alert;
using Content.Shared.CCVar;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NukeOps;
using Content.Shared.Players;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Radium.Changeling;
using Content.Shared.Radium.Changeling.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.StatusEffect;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Server.Console;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
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
    //Evolution shop categories

    [ValidatePrototypeId<StoreCategoryPrototype>]
    private const string ChangelingCategoriesDefensive = "ChangelingDefensive";

    [ValidatePrototypeId<StoreCategoryPrototype>]
    private const string ChangelingCategoriesOffensive = "ChangelingOffensive";

    //Base action set

    [ValidatePrototypeId<EntityPrototype>]
    private const string ChangelingShopId = "ActionChangelingShop";

    [ValidatePrototypeId<EntityPrototype>]
    private const string AbsorbDnaId = "ActionChangelingAbsorbDNA";

    [ValidatePrototypeId<EntityPrototype>]
    private const string StasisId = "ActionChangelingStasis";

    [ValidatePrototypeId<EntityPrototype>]
    private const string TransformId = "ActionChangelingTransform";

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IServerConsoleHost _console = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly ActorSystem _actors = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string SpawnPointPrototype = "SpawnPointChangeling";

    [ValidatePrototypeId<AntagPrototype>] private const string ChangelingRole = "Changeling";

    [ValidatePrototypeId<EntityPrototype>] private const string GenesObjective = "GenesObjectiveChangeling";

    [ValidatePrototypeId<EntityPrototype>] private const string EscapeObjective = "EscapeShuttleObjectiveChangeling";

    [ValidatePrototypeId<JobPrototype>] private const string JobSpawn = "Passenger";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChangelingSpawnerComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ChangelingComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<ChangelingComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<ChangelingComponent, ChangelingShopActionEvent>(OnShop);
        SubscribeLocalEvent<ChangelingComponent, StatusEffectAddedEvent>(OnStatusAdded);
        SubscribeLocalEvent<ChangelingComponent, StatusEffectEndedEvent>(OnStatusEnded);
        SubscribeLocalEvent<SpawnChangelingEvent>(OnSpawn);
        SubscribeLocalEvent<ChangelingSpawnerComponent, GhostRoleSpawnerUsedEvent>(OnGhostRoleSpawnerUsed);

        InitializeAbilities();
    }

    private void OnMindRemoved(Entity<ChangelingComponent> ent, ref MindRemovedMessage args)
    {
        _action.RemoveAction(ent, ent.Comp.AbsorbDnaAction);
        _action.RemoveAction(ent, ent.Comp.StasisAction);
        _action.RemoveAction(ent, ent.Comp.TransformAction);
        _action.RemoveAction(ent, ent.Comp.ShopAction);
    }

    private void OnMindAdded(EntityUid uid, ChangelingComponent component, MindAddedMessage args)
    {
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            return;
        }

        _action.AddAction(uid, ref component.AbsorbDnaAction, AbsorbDnaId);
        _action.AddAction(uid, ref component.StasisAction, StasisId);
        _action.AddAction(uid, ref component.TransformAction ,TransformId);
        _action.AddAction(uid, ref component.ShopAction , ChangelingShopId);

        InitShop(uid);

        if (_roles.MindHasRole<ChangelingRoleComponent>(mindId))
            return;

        _roles.MindAddRole(mindId,
            new ChangelingRoleComponent
                { PrototypeId = ChangelingRole });
        _mindSystem.TryAddObjective(mindId, mind, GenesObjective);
        _mindSystem.TryAddObjective(mindId, mind, EscapeObjective);



        component.Mind = mind;
        mind.PreventGhosting = true;

        RemComp<PacifiedComponent>(uid);

        EnsureComp<PendingClockInComponent>(uid);

        _tagSystem.AddTag(uid, "CannotSuicide");
    }

    private void OnPlayerAttached(Entity<ChangelingSpawnerComponent> uid, ref PlayerAttachedEvent args)
    {
        QueueLocalEvent(new SpawnChangelingEvent(uid, args.Player));
    }

    private void OnSpawn(SpawnChangelingEvent ev)
    {
        var uid = ev.Entity.Owner;
        var component = ev.Entity.Comp;

        var xform = Transform(uid);
        var (changelingMob, pref) = SpawnChangeling(xform.Coordinates);
        var playerData = ev.Session.ContentData();
        if (playerData != null && _mindSystem.TryGetMind(playerData.UserId, out var mindId, out var mind))
        {
            _mindSystem.TransferTo(mindId.Value, null, true, false, mind);
            RemComp<MindContainerComponent>(changelingMob);
            Timer.Spawn(0,
                () =>
                {
                    _mindSystem.TransferTo(mindId.Value, changelingMob, true, false, mind);
                });
            ;
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
                        0,
                        station.Value,
                        pref)
                    );
                }

                if (!_roles.MindHasRole<JobComponent>(mindId.Value))
                {
                    _roles.MindAddRole(mindId.Value, new JobComponent { Prototype = "Passenger" });
                }
            }
        }

        QueueDel(uid);
    }

    private void InitShop(EntityUid stealerUid)
    {
        var stealer = EnsureComp<ChangelingComponent>(stealerUid);
        var store = EnsureComp<StoreComponent>(stealerUid);
        store.Categories.Add(ChangelingCategoriesDefensive);
        store.Categories.Add(ChangelingCategoriesOffensive);
        store.RefundAllowed = false;
        store.CurrencyWhitelist.Add(stealer.EvolutionCurrencyPrototype);

        //_store.TryAddCurrency(new Dictionary<string, FixedPoint2>
        //        { { stealer.EvolutionCurrencyPrototype, stealer.Evolution } },
        //    stealerUid,
        //    store);

        //_prototype.TryIndex<ListingPrototype>("ChangelingSpeedUp", out var listing);
        //store.Listings.Add(listing!);
    }

    private (EntityUid stealerUid, HumanoidCharacterProfile pref) SpawnChangeling(EntityCoordinates coords)
    {
        var stealerUid = Spawn("MobChangeling", coords);

        //var stealerUid = Spawn(species.Prototype, coords);


        //_humanoid.LoadProfile(stealerUid, pref);
        //_metaSystem.SetEntityName(stealerUid, MetaData(target).EntityName);
        //if (TryComp<DetailExaminableComponent>(target, out var detail))
        //{
        //    EnsureComp<DetailExaminableComponent>(stealerUid).Content = detail.Content;
        //}

        //_humanoid.LoadProfile(stealerUid, pref);
        /*
        if (pref.FlavorText != "" && _configurationManager.GetCVar(CCVars.FlavorText))
        {
            EnsureComp<DetailExaminableComponent>(stealerUid).Content = pref.FlavorText;
        }

        if (TryComp<FingerprintComponent>(target, out var fingerprintComponent))
        {
            EnsureComp<FingerprintComponent>(stealerUid).Fingerprint = fingerprintComponent.Fingerprint;
        }

        if (TryComp<DnaComponent>(target, out var dnaComponent))
        {
            EnsureComp<DnaComponent>(stealerUid).DNA = dnaComponent.DNA;
        }

        if (_prototype.TryIndex(pref.Species, out var species)) //Probably I shouldn't do that..
        {
            if (species.Prototype.Id == "MobFelinid")
            {
                _console.ExecuteCommand($"scale {stealerUid} 0,8");
            }
        }

        if (!TryComp<JobComponent>(mindId, out var jobComponent) || jobComponent.Prototype == null ||
            !_prototype.TryIndex(jobComponent.Prototype, out var twinTargetMindJob))
            return (stealerUid, pref);
        if (_prototype.TryIndex<StartingGearPrototype>(twinTargetMindJob.StartingGear!, out var gear))
        {
            _stationSpawning.EquipStartingGear(stealerUid, gear);
        }

        // Run loadouts after so stuff like storage loadouts can get
        var jobLoadout = LoadoutSystem.GetJobPrototype(jobComponent.Prototype);

        if (!_prototype.TryIndex(jobLoadout, out RoleLoadoutPrototype? roleProto))
            return (stealerUid, pref);
        pref.Loadouts.TryGetValue(jobLoadout, out var loadout);

        // Set to default if not present
        if (loadout == null)
        {
            loadout = new RoleLoadout(jobLoadout);

            loadout.SetDefault(pref,
                _playerManager.TryGetSessionById(targetSession.Value, out var sess) ? sess : null,
                _prototype,
                true);
        }

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

                if (!_prototype.TryIndex(loadoutProto.Equipment, out var startingGear))
                {
                    Log.Error(
                        $"Unable to find starting gear {loadoutProto.Equipment} for loadout {loadoutProto}");
                    continue;
                }

                // Handle any extra data here.
                _stationSpawning.EquipStartingGear(stealerUid, startingGear, raiseEvent: false);
            }
        }
        */
        var spawnJob = _prototype.Index<JobPrototype>(JobSpawn);
        if (_prototype.TryIndex<StartingGearPrototype>(spawnJob.StartingGear!, out var gear))
        {
            _stationSpawning.EquipStartingGear(stealerUid, gear);
        }

        var pref = HumanoidCharacterProfile.Random();
        // Run loadouts after so stuff like storage loadouts can get
        var jobLoadout = LoadoutSystem.GetJobPrototype(spawnJob.ID);

        if (!_prototype.TryIndex(jobLoadout, out RoleLoadoutPrototype? roleProto))
            return (stealerUid, pref);

        var loadout = new RoleLoadout(jobLoadout);

        loadout.SetDefault(
            pref,
            _actors.GetSession(stealerUid),
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

                if (!_prototype.TryIndex(loadoutProto.Equipment, out var startingGear))
                {
                    Log.Error(
                        $"Unable to find starting gear {loadoutProto.Equipment} for loadout {loadoutProto}");
                    continue;
                }

                // Handle any extra data here.
                _stationSpawning.EquipStartingGear(stealerUid, startingGear, raiseEvent: false);
            }
        }

        return (stealerUid, pref);
    }

    private void OnStartup(EntityUid uid, ChangelingComponent component, ComponentStartup args)
    {
        //update the icon
        ChangeEssenceAmount(uid, 0, component);

        //default the visuals
        _appearance.SetData(uid, ChangelingVisuals.Idle, false);
        _appearance.SetData(uid, ChangelingVisuals.Harvesting, false);
        _appearance.SetData(uid, ChangelingVisuals.Slowed, false);
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

    public bool ChangeEssenceAmount(EntityUid uid,
        FixedPoint2 amount,
        ChangelingComponent? component = null,
        bool regenCap = false)
    {
        if (!Resolve(uid, ref component))
            return false;

        component.Resource += amount;

        if (regenCap)
            FixedPoint2.Min(component.Resource, component.ResourceRegenCap);

        if (TryComp<StoreComponent>(uid, out var store))
            _store.UpdateUserInterface(uid, uid, store);

        _alerts.ShowAlert(uid,
            "Resource",
            (short) Math.Clamp(Math.Round(component.Resource.Float() / 10f), 0, 16));
        return true;
    }

    private bool TryUseAbility(EntityUid uid, ChangelingComponent component, FixedPoint2 abilityCost, Vector2 debuffs)
    {
        if (component.Resource <= abilityCost)
        {
            _popup.PopupEntity(Loc.GetString("revenant-not-enough-essence"), uid, uid);
            return false;
        }

        ChangeEssenceAmount(uid, abilityCost, component, false);

        _stun.TryStun(uid, TimeSpan.FromSeconds(debuffs.X), false);

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

        var query = EntityQueryEnumerator<ChangelingComponent>();
        while (query.MoveNext(out var uid, out var rev))
        {
            rev.Accumulator += frameTime;

            if (rev.Accumulator <= 1)
                continue;
            rev.Accumulator -= 1;

            if (rev.Resource < rev.ResourceRegenCap)
            {
                ChangeEssenceAmount(uid, rev.ResourcePerSecond, rev, regenCap: true);
            }
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

    public sealed class SpawnChangelingEvent : EntityEventArgs
    {
        public Entity<ChangelingSpawnerComponent> Entity;
        public ICommonSession Session;

        public SpawnChangelingEvent(Entity<ChangelingSpawnerComponent> entity, ICommonSession session)
        {
            Entity = entity;
            Session = session;
        }
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
