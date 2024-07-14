using Content.Server.DetailExaminable;
using Content.Server.Flash;
using Content.Server.Objectives;
using Content.Server.Polymorph.Systems;
using Content.Server.Radium.Changeling.Components;
using Content.Server.Radium.Medical.Surgery.Systems;
using Content.Shared.Actions;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Radium.Changeling;
using Content.Shared.Radium.Changeling.Components;
using Content.Shared.Radium.Changeling.Events;
using Content.Shared.Revenant;
using Content.Shared.StatusEffect;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Serialization.Manager;
using HarvestEvent = Content.Shared.Radium.Changeling.HarvestEvent;

namespace Content.Server.Radium.Changeling.EntitySystems;

public sealed partial class ChangelingSystem
{
    public const string Goggles = "eyes";

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _heal = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly DamageableSystem _damageSystem = default!;
    [Dependency] private readonly SurgerySystem _surgerySystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectivesSystem = default!;
    [Dependency] private readonly GrammarSystem _grammar = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<ChangelingComponent, ChangelingAbsorbDnaActionEvent>(OnAbsorbDNAActions);
        SubscribeLocalEvent<ChangelingComponent, HarvestEvent>(OnHarvest);

        SubscribeLocalEvent<ChangelingComponent, ChangelingStasisActionEvent>(OnStasisAction);
        SubscribeLocalEvent<ChangelingComponent, ChangelingTransformActionEvent>(OnTransformAction);
        SubscribeLocalEvent<ChangelingComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<ConfirmTransformation>(OnTransformationConfirmed);

        SubscribeLocalEvent<ChangelingComponent, ActionChangelingAdrenalineSacsEvent>(OnAdrenalineSacsEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingStrainedMusclesEvent>(OnStrainedMusclesEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingAnatomicPanaceaEvent>(OnAnatomicPanaceaEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingChitinousArmorEvent>(OnChitinousArmorEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingVoidAdaptationEvent>(OnVoidAdaptationEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingOrganicShieldEvent>(OnOrganicShieldEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingMimicVoiceEvent>(OnMimicVoiceEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingLesserFormEvent>(OnLesserFormEventAction); //TODO
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingFleshmendEvent>(OnFleshmendEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingChameleonSkinEvent>(OnChameleonSkinEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingBiodegradeEvent>(OnStrainedBiodegradeEventAction);
        SubscribeLocalEvent<ChangelingComponent, PassiveChangelingAugmentedEyesightEvent>(OnAugmentedEyesightEventAction);
        SubscribeLocalEvent<ChangelingComponent, PassiveChangelingDefibrillatorGraspEvent>(OnDefibrillatorGraspEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingDnaStingEvent>(OnDnaStingEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingTransformationStingEvent>(OnTransformationStingEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingMuteStingEvent>(OnMuteStingEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingBlindStingEvent>(OnBlindStingEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingHallutinationStingEvent>(OnHallutinationStingEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingCryogenicStingEvent>(OnCryogenicStingEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingFalseArmbladeStingEvent>(OnFalseArmbladeStingEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingArmBladeEvent>(OnArmBladeEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingResonantShriekEvent>(OnResonantShriekEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingDissonantShriekEvent>(OnDissonantShriekEventAction);
    }

    private void OnDissonantShriekEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingDissonantShriekEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnResonantShriekEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingResonantShriekEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnArmBladeEventAction(EntityUid uid, ChangelingComponent component, ActionChangelingArmBladeEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnFalseArmbladeStingEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingFalseArmbladeStingEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnCryogenicStingEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingCryogenicStingEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnHallutinationStingEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingHallutinationStingEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnBlindStingEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingBlindStingEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnMuteStingEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingMuteStingEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnTransformationStingEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingTransformationStingEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnDnaStingEventAction(EntityUid uid, ChangelingComponent component, ActionChangelingDnaStingEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnDefibrillatorGraspEventAction(EntityUid uid,
        ChangelingComponent component,
        PassiveChangelingDefibrillatorGraspEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnAugmentedEyesightEventAction(EntityUid uid,
        ChangelingComponent component,
        PassiveChangelingAugmentedEyesightEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnStrainedBiodegradeEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingBiodegradeEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnChameleonSkinEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingChameleonSkinEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnFleshmendEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingFleshmendEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnLesserFormEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingLesserFormEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnMimicVoiceEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingMimicVoiceEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnOrganicShieldEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingOrganicShieldEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnVoidAdaptationEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingVoidAdaptationEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnChitinousArmorEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingChitinousArmorEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnAnatomicPanaceaEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingAnatomicPanaceaEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnStrainedMusclesEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingStrainedMusclesEvent args)
    {
        throw new NotImplementedException();
    }

    private void OnAdrenalineSacsEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingAdrenalineSacsEvent args)
    {
        throw new NotImplementedException();
    }

    private static void OnMobStateChanged(EntityUid uid, ChangelingComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Critical or MobState.Alive)
        {
            component.IsInStasis = false;
        }
    }

    private void OnAbsorbDNAActions(EntityUid uid, ChangelingComponent component, ChangelingAbsorbDnaActionEvent args)
    {
        if (args.Target == args.Performer)
            return;

        var target = args.Target;

        if (!HasComp<MobStateComponent>(target) || !HasComp<HumanoidAppearanceComponent>(target) ||
            HasComp<ChangelingComponent>(target))
            return;

        args.Handled = true;
        BeginHarvestDoAfter(uid,
            target,
            component,
            !TryComp<ResourceComponent>(target, out var resource) ? EnsureComp<ResourceComponent>(target) : resource);
    }

    private void BeginHarvestDoAfter(EntityUid uid,
        EntityUid target,
        ChangelingComponent changeling,
        ResourceComponent resource)
    {
        if (resource.Harvested)
        {
            _popup.PopupEntity(Robust.Shared.Localization.Loc.GetString("changeling-dna-harvested"),
                target,
                uid,
                PopupType.SmallCaution);
            return;
        }

        if (!TryComp<CuffableComponent>(target, out var cuffableComponent))
        {
            return;
        }

        if (cuffableComponent.CanStillInteract)
        {
            _popup.PopupEntity(Robust.Shared.Localization.Loc.GetString("changeling-too-powerful"), target, uid);
            return;
        }

        _flash.Flash(target: uid,
            flashDuration: 1000f,
            user: uid,
            used: null,
            slowTo: 1000F,
            displayPopup: false,
            forced: true);
        _flash.Flash(target: target,
            flashDuration: 12000f,
            user: target,
            used: null,
            slowTo: 1000F,
            displayPopup: false,
            forced: true);
        _stun.TryStun(target, TimeSpan.FromSeconds(25), true);
        _stun.TryKnockdown(target, TimeSpan.FromSeconds(25), true);

        var doAfter = new DoAfterArgs(EntityManager,
            uid,
            changeling.HarvestDebuffs.X,
            new HarvestEvent(),
            uid,
            target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = false, // stuns itself
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _appearance.SetData(uid, ChangelingVisuals.Harvesting, true);

        _popup.PopupEntity(Robust.Shared.Localization.Loc.GetString("changeling-begin-harvest", ("target", target)),
            target,
            PopupType.LargeCaution);

        TryUseAbility(uid, changeling, 0, changeling.HarvestDebuffs);
    }

    private void OnHarvest(EntityUid uid, ChangelingComponent component, HarvestEvent args)
    {
        if (args.Cancelled)
        {
            _appearance.SetData(uid, ChangelingVisuals.Harvesting, false);
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        _appearance.SetData(uid, RevenantVisuals.Harvesting, false);

        EnsureComp<ResourceComponent>(args.Args.Target.Value, out var resource);

        resource.Harvested = true;

        ChangeEssenceAmount(uid, 30, component);

        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
                { { component.EvolutionCurrencyPrototype, 1 } },
            uid);

        var damage = new DamageSpecifier();

        damage.DamageDict.Add("Slash", 25);
        damage.DamageDict.Add("Heat", 400);

        _damageSystem.TryChangeDamage(target, damage, true);
        RemComp<DamageableComponent>(target);
        if (!HasComp<MobStateComponent>(target))
            return;

        if (TryComp<HumanoidAppearanceComponent>(args.Args.Target.Value, out var humanoidAppearance) &&
            TryComp<MetaDataComponent>(args.Args.Target.Value, out var metaData))
        {
            _serializationManager.CopyTo(humanoidAppearance, ref component.SourceHumanoid);
            _serializationManager.CopyTo(metaData, ref component.Metadata);

            component.ServerIdentitiesList.Add(component.ServerIdentitiesList.Count,
                (component.Metadata, humanoidAppearance)!);
            component.ClientIdentitiesList.Add(component.ServerIdentitiesList.Count - 1,
                component.Metadata!.EntityName); //Idk how to do better. Was messing with component but no luck there..

            Dirty(uid, component);
            _userInterface.SetUiState(uid, ChangelingStorageUiKey.Key, new ChangelingStorageUiState());
            humanoidAppearance.SkinColor = Color.Gray;

            Dirty(args.Args.Target.Value, humanoidAppearance);

            _metaSystem.SetEntityName(target, Loc.GetString("changeling-absorbed-corpse-name"));
            _metaSystem.SetEntityDescription(target, Loc.GetString("changeling-absorbed-corpse-description"));
        }

        TryComp<ActionsComponent>(args.Args.Target.Value, out var actions);
        component.Actions = actions;

        if (TryComp<DetailExaminableComponent>(args.Args.Target.Value, out var detail))
        {
            detail.Content = Loc.GetString("changeling-absorbed-corpse-detailed-description");
        }


        if (_mindSystem.TryGetObjectiveComp<GenesConditionComponent>(uid, out var obj))
            obj.GenesExtracted++;

        args.Handled = true;
    }

    private void OnStasisAction(EntityUid uid, ChangelingComponent component, ChangelingStasisActionEvent args)
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
            _surgerySystem.HealAllWounds(uid);
            return;
        }

        _mobState.ChangeMobState(uid, MobState.Dead, state);
        component.IsInStasis = true;
    }

    private void OnTransformAction(EntityUid uid, ChangelingComponent component, ChangelingTransformActionEvent args)
    {
        if (!TryComp<ChangelingComponent>(uid, out _))
            return;
        _userInterface.OpenUi(uid, ChangelingStorageUiKey.Key, uid);
    }

    private void OnTransformationConfirmed(ConfirmTransformation args)
    {
        var uid = GetEntity(args.Uid);
        if (!TryComp<ChangelingComponent>(uid, out var component))
            return;

        if (!TryUseAbility(uid, component, component.TransformCost, component.TransformDebuffs))
        {
            _popup.PopupEntity(
                Robust.Shared.Localization.Loc.GetString("changeling-abiltiy-failed", ("target", uid)),
                uid,
                PopupType.LargeCaution);
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
                Robust.Shared.Localization.Loc.GetString("changeling-transform-failed", ("target", uid)),
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
        if (targetHumanoid.Species.Id == "Felinid" && component.SourceHumanoid.Species.Id != "Felinid")
        {
            _console.ExecuteCommand($"scale {uid} 1,2");
        }
        else if (targetHumanoid.Species.Id != "Felinid" && component.SourceHumanoid.Species.Id == "Felinid")
        {
            _console.ExecuteCommand($"scale {uid} 0,8");
        }

        targetHumanoid.Species = component.SourceHumanoid.Species;
        targetHumanoid.SkinColor = component.SourceHumanoid.SkinColor;
        targetHumanoid.EyeColor = component.SourceHumanoid.EyeColor;
        targetHumanoid.Age = component.SourceHumanoid.Age;
        _humanoid.SetSex(uid, component.SourceHumanoid.Sex, false, targetHumanoid);
        targetHumanoid.CustomBaseLayers =
            new Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>(component.SourceHumanoid.CustomBaseLayers);
        targetHumanoid.MarkingSet = new MarkingSet(component.SourceHumanoid.MarkingSet);
        _humanoid.SetTTSVoice(uid, component.SourceHumanoid.Voice, targetHumanoid); // Corvax-TTS

        targetHumanoid.Gender = component.SourceHumanoid.Gender;

        //_humanoid.LoadProfile(uid, component.Preferences!);

        Dirty(uid, targetHumanoid);

        _flash.Flash(target: uid,
            flashDuration: 12000f,
            user: uid,
            used: null,
            slowTo: 0.8F,
            displayPopup: false,
            forced: true);

        //EnsureComp<DetailExaminableComponent>(uid).Content = component.Detail;
    }
}
