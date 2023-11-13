using Content.Server.Construction.Completions;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.Radium.Components;
using Content.Server.Revenant.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Radium.Genestealer;
using Content.Shared.Radium.Genestealer.Components;
using Content.Shared.Revenant;
using Content.Shared.Stunnable;
using HarvestEvent = Content.Shared.Radium.Genestealer.HarvestEvent;

namespace Content.Server.Radium.Genestealer.EntitySystems;

public sealed partial class GenestealerSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly DamageableSystem _heal = default!;

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<GenestealerComponent, GenestealerAbsorbDnaActionEvent>(OnInteract);
        SubscribeLocalEvent<GenestealerComponent, HarvestEvent>(OnHarvest);

        SubscribeLocalEvent<GenestealerComponent, GenestealerStasisActionEvent>(OnStasisAction);
        SubscribeLocalEvent<GenestealerComponent, GenestealerTransformActionEvent>(OnTransformAction);
        SubscribeLocalEvent<GenestealerComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private static void OnMobStateChanged(EntityUid uid, GenestealerComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Critical or MobState.Alive)
        {
            component.IsInStasis = false;
        }
    }

    private void OnInteract(EntityUid uid, GenestealerComponent component, InteractNoHandEvent args)
    {
        if (args.Target == args.User || args.Target == null)
            return;

        var target = args.Target.Value;

        if (!HasComp<MobStateComponent>(target) || !HasComp<HumanoidAppearanceComponent>(target) ||
            HasComp<GenestealerComponent>(target))
            return;

        args.Handled = true;
        if (!TryComp<ResourceComponent>(target, out var resource))
        {
            BeginHarvestDoAfter(uid, target, component, EnsureComp<ResourceComponent>(target));
        }
        else
        {
            BeginHarvestDoAfter(uid, target, component, resource);
        }
    }

    private void BeginHarvestDoAfter(EntityUid uid, EntityUid target, GenestealerComponent genestealer,
        ResourceComponent resource)
    {
        if (resource.Harvested)
        {
            _popup.PopupEntity(Robust.Shared.Localization.Loc.GetString("genestealer-dna-harvested"), target, uid,
                PopupType.SmallCaution);
            return;
        }

        if (TryComp<MobStateComponent>(target, out var mobstate) && mobstate.CurrentState == MobState.Alive &&
            !HasComp<SleepingComponent>(target) && !HasComp<StunnedComponent>(target))
        {
            _popup.PopupEntity(Robust.Shared.Localization.Loc.GetString("genestealer-cant-harvest-now"), target, uid);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, uid, genestealer.HarvestDebuffs.X,
            new HarvestEvent(), uid,
            target: target)
        {
            DistanceThreshold = 2,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            RequireCanInteract = false, // stuns itself
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _appearance.SetData(uid, GenestealerVisuals.Harvesting, true);

        _popup.PopupEntity(Robust.Shared.Localization.Loc.GetString("genestealer-begin-harvest", ("target", target)),
            target, PopupType.Large);

        TryUseAbility(uid, genestealer, 0, genestealer.HarvestDebuffs);
    }

    private void OnHarvest(EntityUid uid, GenestealerComponent component, HarvestEvent args)
    {
        if (args.Cancelled)
        {
            _appearance.SetData(uid, GenestealerVisuals.Harvesting, false);
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        _appearance.SetData(uid, RevenantVisuals.Harvesting, false);

        EnsureComp<ResourceComponent>(args.Args.Target.Value, out var resource);
        _popup.PopupEntity(
            Robust.Shared.Localization.Loc.GetString("genestealer-finish-harvest", ("target", args.Args.Target)),
            args.Args.Target.Value, PopupType.LargeCaution);

        resource.Harvested = true;
        ChangeEssenceAmount(uid, resource.ResourceAmount, component);
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
            { { component.StolenResourceCurrencyPrototype, resource.ResourceAmount } }, uid);
        if (!HasComp<MobStateComponent>(args.Args.Target))
            return;

        if (_mobState.IsAlive(args.Args.Target.Value) || _mobState.IsCritical(args.Args.Target.Value))
        {
            _popup.PopupEntity(Robust.Shared.Localization.Loc.GetString("genestealer-max-resource-increased"), uid, uid);
            component.ResourceRegenCap += component.MaxEssenceUpgradeAmount;
        }

        args.Handled = true;
    }

    private void OnStasisAction(EntityUid uid, GenestealerComponent component, GenestealerStasisActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.StasisCost, component.StasisDebuffs))
            return;

        args.Handled = true;

        if (!TryComp<MobStateComponent>(uid, out var state))
            return;
        if (!TryComp<DamageableComponent>(uid, out var damageComponent))
        {
            return;
        }
        if (state.CurrentState == MobState.Dead && component.IsInStasis)
        {
            _heal.SetAllDamage(uid, damageComponent, 0.1);
            _heal.TryChangeDamage(uid, new DamageSpecifier());
            _mobState.ChangeMobState(uid, MobState.Alive, state);
            component.IsInStasis = false;
            return;
        }

        _mobState.ChangeMobState(uid, MobState.Dead, state);
        component.IsInStasis = true;
    }

    private void OnTransformAction(EntityUid uid, GenestealerComponent component, GenestealerTransformActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.TransformCost, component.TransformDebuffs))
            return;

        args.Handled = true;

    }
}
