using Content.Server.Flash;
using Content.Server.Objectives;
using Content.Server.Polymorph.Systems;
using Content.Server.Radium.Genestealer.Components;
using Content.Shared.Actions;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Radium.Genestealer;
using Content.Shared.Radium.Genestealer.Components;
using Content.Shared.Revenant;
using Content.Shared.StatusEffect;
using Robust.Shared.GameObjects.Components.Localization;
using HarvestEvent = Content.Shared.Radium.Genestealer.HarvestEvent;

namespace Content.Server.Radium.Genestealer.EntitySystems;

public sealed partial class GenestealerSystem
{
    public const string Goggles = "eyes";

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _heal = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectivesSystem = default!;

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

        _inventorySystem.TryUnequip(target, Goggles, true, true);
        _inventorySystem.TryUnequip(uid, Goggles, true, true);
        _flash.Flash(target: uid, flashDuration: 1000f, user: uid, used: null, slowTo: 1000F, displayPopup: false);
        _flash.Flash(target: target, flashDuration: 12000f, user: target, used: null, slowTo: 1000F,
            displayPopup: false);
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
                if (TryComp<HumanoidAppearanceComponent>(args.Args.Target.Value, out var humanoidAppearance))
                {
                    component.SourceHumanoid = humanoidAppearance;
                }

                component.Preferences =
                    (HumanoidCharacterProfile) _prefs.GetPreferences(component.Session!.Value).SelectedCharacter;
                TryComp<ActionsComponent>(args.Args.Target.Value, out var actions);
                component.Actions = actions;
                /*
                if (TryComp<DetailExaminableComponent>(args.Args.Target.Value, out var detail))
                {
                    component.Detail = detail.Content;
                }
                */
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

        if (_mindSystem.TryGetObjectiveComp<GenesConditionComponent>(uid, out var obj))
            obj.GenesExtracted++;
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
        /*
        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            humanoid.Species = component.Preferences.Species;
            Dirty(uid, humanoid);
        }


        else
        {
            _popup.PopupEntity(
                Robust.Shared.Localization.Loc.GetString("genestealer-transform-failed", ("target", uid)),
                args.Performer, PopupType.LargeCaution);
        }
        */
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var targetHumanoid))
        {
            return;
        }

        if (component.SourceHumanoid == null)
            return;

        /*
        var polyString = component.SourceHumanoid.Species + "Morph";

        if (_prototype.TryIndex<PolymorphPrototype>(polyString, out var prototype))
        {
            var tempUid = _polymorphSystem.PolymorphEntity(args.Performer, polyString);
            if (tempUid.HasValue)
            {
                uid = tempUid.Value;
            }
        }
        */

        _metaSystem.SetEntityName(uid, component.Metadata!.EntityName);
        targetHumanoid.Species = component.SourceHumanoid.Species;
        targetHumanoid.SkinColor = component.SourceHumanoid.SkinColor;
        targetHumanoid.EyeColor = component.SourceHumanoid.EyeColor;
        targetHumanoid.Age = component.SourceHumanoid.Age;
        _humanoidSystem.SetSex(uid, component.SourceHumanoid.Sex, false, targetHumanoid);
        targetHumanoid.CustomBaseLayers =
        new Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>(component.SourceHumanoid.CustomBaseLayers);
        targetHumanoid.MarkingSet = new MarkingSet(component.SourceHumanoid.MarkingSet);
        _humanoidSystem.SetTTSVoice(uid, component.SourceHumanoid.Voice, targetHumanoid); // Corvax-TTS
        targetHumanoid.SpeakerColor = component.SourceHumanoid.SpeakerColor; // Corvax-SpeakerColor

        targetHumanoid.Gender = component.SourceHumanoid.Gender;

        if (TryComp<GrammarComponent>(uid, out var grammar))
        {
            grammar.Gender = component.SourceHumanoid.Gender;
        }

        _humanoidSystem.LoadProfile(uid, component.Preferences!);

        Dirty(uid, targetHumanoid);

        _inventorySystem.TryUnequip(uid, Goggles, true, true);

        _flash.Flash(target: uid, flashDuration: 12000f, user: args.Performer, used: null, slowTo: 0.8F,
            displayPopup: false);

        //EnsureComp<DetailExaminableComponent>(uid).Content = component.Detail;

        args.Handled = true;
    }
}
