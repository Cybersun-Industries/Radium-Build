using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Server.Actions;
using Content.Server.Backmen.Cloning;
using Content.Server.Backmen.EvilTwin;
using Content.Server.Backmen.Fugitive;
using Content.Server.DetailExaminable;
using Content.Server.Forensics;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Humanoid;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.Radium.Components;
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
using Content.Shared.Eye;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Maps;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NukeOps;
using Content.Shared.Physics;
using Content.Shared.Players;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Radium.Genestealer;
using Content.Shared.Radium.Genestealer.Components;
using Content.Shared.Revenant.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.StatusEffect;
using Content.Shared.Store;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Linguini.Syntax.Ast;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Radium.Genestealer.EntitySystems;

public sealed partial class GenestealerSystem : EntitySystem
{
    [ValidatePrototypeId<EntityPrototype>]
    private const string GenestealerShopId = "ActionGenestealerShop";

    [ValidatePrototypeId<EntityPrototype>]
    private const string StoreBoundUserInterfaceId = "StoreBoundUserInterface";
    private const string StasisId = "ActionGenestealerStasis";
    private const string TransformId = "ActionGenestealerTransform";

    private const string GenestealerCurrency = "StolenResource";
    private const string GenestealerCategories = "GenestealerAbilities";
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly RoleSystem _roles = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string SpawnPointPrototype = "SpawnPointGenestealer";

    [ValidatePrototypeId<AntagPrototype>] private const string EvilTwinRole = "Genestealer";

    [ValidatePrototypeId<EntityPrototype>] private const string KillObjective = "KillObjectiveEvilTwin1";

    [ValidatePrototypeId<EntityPrototype>] private const string EscapeObjective = "EscapeShuttleObjectiveEvilTwin1";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenestealerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GenestealerSpawnerComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<GenestealerComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<GenestealerComponent, GenestealerShopActionEvent>(OnShop);
        SubscribeLocalEvent<GenestealerComponent, StatusEffectAddedEvent>(OnStatusAdded);
        SubscribeLocalEvent<GenestealerComponent, StatusEffectEndedEvent>(OnStatusEnded);
        SubscribeLocalEvent<SpawnGenestealerEvent>(OnSpawn);
        SubscribeLocalEvent<GenestealerSpawnerComponent, GhostRoleSpawnerUsedEvent>(OnGhostRoleSpawnerUsed);

        InitializeAbilities();
    }

    private void OnMindAdded(EntityUid uid, GenestealerComponent component, MindAddedMessage args)
    {
        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            return;
        }

        _roles.MindAddRole(mindId,
            new EvilTwinRoleComponent
                { PrototypeId = EvilTwinRole });

        _mindSystem.TryAddObjective(mindId, mind, KillObjective);
        _mindSystem.TryAddObjective(mindId, mind, EscapeObjective);

        mind.PreventGhosting = true;

        RemComp<PacifiedComponent>(uid);

        EnsureComp<PendingClockInComponent>(uid);

        _tagSystem.AddTag(uid, "CannotSuicide");
    }

    private void OnPlayerAttached(Entity<GenestealerSpawnerComponent> uid, ref PlayerAttachedEvent args)
    {
        QueueLocalEvent(new SpawnGenestealerEvent(uid, args.Player));
    }

    private void OnSpawn(SpawnGenestealerEvent ev)
    {
        var uid = ev.Entity.Owner;
        var component = ev.Entity.Comp;
        HumanoidCharacterProfile? pref;

        EntityUid? targetUid = null;

        if (component.TargetForce != EntityUid.Invalid)
        {
            if (IsEligibleHumanoid(component.TargetForce))
            {
                targetUid = component.TargetForce;
            }
        }
        else
        {
            TryGetEligibleHumanoid(out targetUid);
        }

        if (targetUid.HasValue)
        {
            var xform = Transform(uid);
            (var genestealerMob, pref) = SpawnGenestealer(targetUid.Value, xform.Coordinates);
            if (genestealerMob != null)
            {
                var playerData = ev.Session.ContentData();
                if (playerData != null && _mindSystem.TryGetMind(playerData, out var mindId, out var mind))
                {
                    _mindSystem.TransferTo(mindId, null, true, false, mind);
                    RemComp<MindContainerComponent>(genestealerMob.Value);
                    Timer.Spawn(0, () =>
                    {
                        _mindSystem.TransferTo(mindId, genestealerMob, true, false, mind);
                    });

                    var station = _stationSystem.GetOwningStation(targetUid.Value) ?? _stationSystem.GetStations()
                        .FirstOrNull(HasComp<StationEventEligibleComponent>);
                    if (pref != null && station != null &&
                        _mindSystem.TryGetMind(targetUid.Value, out var targetMindId, out var targetMind)
                        && _roles.MindHasRole<JobComponent>(targetMindId))
                    {
                        var currentJob = Comp<JobComponent>(targetMindId);

                        var targetSession = targetMind.Session;
                        var targetUserId = targetMind.UserId ?? targetMind.OriginalOwnerUserId;
                        if (targetUserId == null)
                        {
                            targetSession = ev.Session;
                        }
                        else if (targetSession == null)
                        {
                            targetSession = _playerManager.GetSessionById(targetUserId.Value);
                        }

                        RaiseLocalEvent(new PlayerSpawnCompleteEvent(genestealerMob.Value,
                            targetSession,
                            currentJob.Prototype, false,
                            0, station.Value, pref));

                        if (!_roles.MindHasRole<JobComponent>(mindId))
                        {
                            _roles.MindAddRole(mindId, new JobComponent { Prototype = currentJob.Prototype });
                        }
                    }
                }
            }
        }

        QueueDel(uid);
    }

    private (EntityUid?, HumanoidCharacterProfile? pref) SpawnGenestealer(EntityUid target, EntityCoordinates coords)
    {
        if (!_mindSystem.TryGetMind(target, out var mindId, out var mind) ||
            !HasComp<HumanoidAppearanceComponent>(target))
        {
            return (null, null);
        }

        var targetSession = mind.UserId ?? mind.OriginalOwnerUserId;

        if (targetSession == null)
        {
            return (null, null);
        }

        var pref = (HumanoidCharacterProfile) _prefs.GetPreferences(targetSession.Value).SelectedCharacter;
        if (!_prototype.TryIndex<SpeciesPrototype>(pref.Species, out var species))
        {
            return (null, null);
        }

        var stealerUid = Spawn(species.Prototype, coords);
        _humanoid.LoadProfile(stealerUid, pref);
        _metaSystem.SetEntityName(stealerUid, MetaData(target).EntityName);
        if (TryComp<DetailExaminableComponent>(target, out var detail))
        {
            EnsureComp<DetailExaminableComponent>(stealerUid).Content = detail.Content;
        }

        _humanoidSystem.LoadProfile(stealerUid, pref);

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


        if (TryComp<JobComponent>(mindId, out var jobComponent) && jobComponent.Prototype != null &&
            _prototype.TryIndex<JobPrototype>(jobComponent.Prototype, out var targetMindJob))
        {
            if (_prototype.TryIndex<StartingGearPrototype>(targetMindJob.StartingGear!, out var gear))
            {
                _stationSpawning.EquipStartingGear(stealerUid, gear, pref);
                _stationSpawning.EquipIdCard(stealerUid, pref.Name, targetMindJob,
                    _stationSystem.GetOwningStation(target));
            }

            foreach (var special in targetMindJob.Special)
            {
                special.AfterEquip(stealerUid);
            }
        }

        var stealer = AddComp<GenestealerComponent>(stealerUid);
        var store = AddComp<StoreComponent>(stealerUid);
        store.Categories.Add(GenestealerCategories);
        store.CurrencyWhitelist.Add(GenestealerCurrency);
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
            { {stealer.StolenResourceCurrencyPrototype, stealer.Resource} }, stealerUid, store);
        _prototype.TryIndex<ListingPrototype>("GenestealerSpeedUp", out var listing);
        //store.Listings.Add(listing!);

        _action.AddAction(stealerUid, GenestealerShopId);
        _action.AddAction(stealerUid, StasisId);
        _action.AddAction(stealerUid, TransformId);
        return (stealerUid, pref);
    }

    private void OnStartup(EntityUid uid, GenestealerComponent component, ComponentStartup args)
    {
        //update the icon
        ChangeEssenceAmount(uid, 0, component);

        //default the visuals
        _appearance.SetData(uid, GenestealerVisuals.Idle, false);
        _appearance.SetData(uid, GenestealerVisuals.Harvesting, false);
        _appearance.SetData(uid, GenestealerVisuals.Slowed, false);
    }

    private void OnStatusAdded(EntityUid uid, GenestealerComponent component, StatusEffectAddedEvent args)
    {
        if (args.Key == "Stun")
            _appearance.SetData(uid, GenestealerVisuals.Slowed, true);
    }

    private void OnStatusEnded(EntityUid uid, GenestealerComponent component, StatusEffectEndedEvent args)
    {
        if (args.Key == "Stun")
            _appearance.SetData(uid, GenestealerVisuals.Slowed, false);
    }

    public bool ChangeEssenceAmount(EntityUid uid, FixedPoint2 amount, GenestealerComponent? component = null,
        bool regenCap = false)
    {
        if (!Resolve(uid, ref component))
            return false;

        component.Resource += amount;

        if (regenCap)
            FixedPoint2.Min(component.Resource, component.ResourceRegenCap);

        if (TryComp<StoreComponent>(uid, out var store))
            _store.UpdateUserInterface(uid, uid, store);

        _alerts.ShowAlert(uid, AlertType.Resource,
            (short) Math.Clamp(Math.Round(component.Resource.Float() / 10f), 0, 16));
        return true;
    }

    private bool TryUseAbility(EntityUid uid, GenestealerComponent component, FixedPoint2 abilityCost, Vector2 debuffs)
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

    private void OnShop(EntityUid uid, GenestealerComponent component, GenestealerShopActionEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;
        _store.ToggleUi(uid, uid, store);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GenestealerComponent>();
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

    public bool MakeGenestealer([NotNullWhen(true)] out EntityUid? genestealerSpawn, EntityUid? uid = null)
    {
        genestealerSpawn = null;

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
        genestealerSpawn = Spawn(SpawnPointPrototype, coords);

        if (uid.HasValue)
        {
            EnsureComp<GenestealerSpawnerComponent>(genestealerSpawn.Value).TargetForce = uid.Value;
        }

        return true;
    }

    public sealed class SpawnGenestealerEvent : EntityEventArgs
    {
        public Entity<GenestealerSpawnerComponent> Entity;
        public ICommonSession Session;

        public SpawnGenestealerEvent(Entity<GenestealerSpawnerComponent> entity, ICommonSession session)
        {
            Entity = entity;
            Session = session;
        }
    }

    private bool IsEligibleHumanoid(EntityUid? uid)
    {
        if (!uid.HasValue || !uid.Value.IsValid())
        {
            return false;
        }

        return !(HasComp<MetempsychosisKarmaComponent>(uid) ||
                 HasComp<FugitiveComponent>(uid) ||
                 HasComp<EvilTwinComponent>(uid) ||
                 HasComp<NukeOperativeComponent>(uid));
    }

    private void TryGetEligibleHumanoid([NotNullWhen(true)] out EntityUid? uid)
    {
        var targets = new List<EntityUid>();
        {
            var query = AllEntityQuery<ActorComponent, MindContainerComponent, HumanoidAppearanceComponent>();
            while (query.MoveNext(out var entityUid, out _, out var mindContainer, out _))
            {
                if (!IsEligibleHumanoid(entityUid))
                    continue;

                if (!mindContainer.HasMind || mindContainer.Mind == null ||
                    TerminatingOrDeleted(mindContainer.Mind.Value))
                {
                    continue;
                }

                if (!_roles.MindHasRole<JobComponent>(mindContainer.Mind.Value))
                {
                    continue;
                }

                targets.Add(entityUid);
            }
        }

        uid = null;

        if (targets.Count == 0)
        {
            return;
        }

        uid = _random.Pick(targets);
    }

    private void OnGhostRoleSpawnerUsed(EntityUid uid, GenestealerSpawnerComponent component,
        GhostRoleSpawnerUsedEvent args)
    {
        if (TerminatingOrDeleted(args.Spawner) || EntityManager.IsQueuedForDeletion(args.Spawner))
        {
            return;
        }

        //forward
        if (TryComp<GenestealerSpawnerComponent>(args.Spawner, out var comp))
        {
            component.TargetForce = comp.TargetForce;
        }

        QueueDel(args.Spawner);
    }
}
