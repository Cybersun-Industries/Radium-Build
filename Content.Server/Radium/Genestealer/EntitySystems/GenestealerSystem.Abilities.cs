using Content.Server.Construction.Completions;
using Content.Server.Cuffs;
using Content.Server.DetailExaminable;
using Content.Server.Flash;
using Content.Server.Flash.Components;
using Content.Server.Radium.Genestealer.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Radium.Genestealer;
using Content.Shared.Radium.Genestealer.Components;
using Content.Shared.Revenant;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using HarvestEvent = Content.Shared.Radium.Genestealer.HarvestEvent;

namespace Content.Server.Radium.Genestealer.EntitySystems;

public sealed partial class GenestealerSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _heal = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<GenestealerComponent, GenestealerAbsorbDnaActionEvent>(OnAbsorbDNAActions);
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

    private void OnAbsorbDNAActions(EntityUid uid, GenestealerComponent component, GenestealerAbsorbDnaActionEvent args)
    {
        if (args.Target == args.Performer)
            return;

        var target = args.Target;

        if (!HasComp<MobStateComponent>(target) || !HasComp<HumanoidAppearanceComponent>(target) ||
            HasComp<GenestealerComponent>(target))
            return;

        args.Handled = true;
        BeginHarvestDoAfter(uid, target, component,
            !TryComp<ResourceComponent>(target, out var resource) ? EnsureComp<ResourceComponent>(target) : resource);
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

        if (!TryComp<CuffableComponent>(target, out var cuffableComponent))
        {
            return;
        }
        if (cuffableComponent.CanStillInteract)
        {
            _popup.PopupEntity(Robust.Shared.Localization.Loc.GetString("genestealer-too-powerful"), target, uid);
            return;
        }

        _flash.Flash(target:target, flashDuration:20000f, user:uid, used:null, slowTo: 1000F, displayPopup:false, melee: true);
        _stun.TryStun(target, TimeSpan.FromSeconds(25), true);
        _stun.TryKnockdown(target, TimeSpan.FromSeconds(25), true);

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
            target, PopupType.LargeCaution);

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

        resource.Harvested = true;
        ChangeEssenceAmount(uid, resource.ResourceAmount, component);
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
            { { component.StolenResourceCurrencyPrototype, resource.ResourceAmount } }, uid);
        if (!HasComp<MobStateComponent>(args.Args.Target))
            return;

        if (_mobState.IsAlive(args.Args.Target.Value) || _mobState.IsCritical(args.Args.Target.Value))
        {
            _popup.PopupEntity(Robust.Shared.Localization.Loc.GetString("genestealer-max-resource-increased"), uid,
                uid);
            component.ResourceRegenCap += component.MaxEssenceUpgradeAmount;
        }

        if (TryComp<MindContainerComponent>(args.Args.Target.Value, out var mindContainer))
        {
            if (_mindSystem.TryGetSession(mindContainer.Mind, out var session))
            {
                component.Metadata = MetaData(args.Args.Target.Value);
                component.Session = session.UserId;
                component.Preferences =
                    (HumanoidCharacterProfile) _prefs.GetPreferences(component.Session!.Value).SelectedCharacter;
                if (TryComp<DetailExaminableComponent>(args.Args.Target.Value, out var detail))
                {
                    component.Detail = detail.Content;
                }
            }
            else
            {
                _popup.PopupEntity(
                    Robust.Shared.Localization.Loc.GetString("genestealer-no-session", ("target", args.Args.User)),
                    args.Args.User, PopupType.LargeCaution);
            }
        }
        else
        {
            _popup.PopupEntity(
                Robust.Shared.Localization.Loc.GetString("no-mind-container", ("target", args.Args.User)),
                args.Args.User, PopupType.LargeCaution);
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
        {
            _popup.PopupEntity(
                Robust.Shared.Localization.Loc.GetString("genestealer-handled", ("target", uid)),
                args.Performer, PopupType.LargeCaution);
            return;
        }

        if (!TryUseAbility(uid, component, component.TransformCost, component.TransformDebuffs))
        {
            _popup.PopupEntity(
                Robust.Shared.Localization.Loc.GetString("genestealer-abiltiy-failed", ("target", uid)),
                args.Performer, PopupType.LargeCaution);
            return;
        }

        if (component.Session == null)
        {
            _popup.PopupEntity(
                Robust.Shared.Localization.Loc.GetString("genestealer-no-session", ("target", uid)),
                args.Performer, PopupType.LargeCaution);
            return;
        }

        var ev = new AfterFlashedEvent(uid, uid, null);
        RaiseLocalEvent(uid, ref ev);
        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            humanoid.Species = component.Preferences.Species;
            Dirty(uid, humanoid);
        }
        
        _metaSystem.SetEntityName(args.Performer, component.Metadata!.EntityName);
        _flash.Flash(target:uid, flashDuration:12000f, user:args.Performer, used:null, slowTo: 1000F, displayPopup:false);
        _humanoidSystem.LoadProfile(uid, component.Preferences);

        EnsureComp<DetailExaminableComponent>(uid).Content = component.Detail;

        args.Handled = true;
    }
}
