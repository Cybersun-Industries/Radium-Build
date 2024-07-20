using System.Linq;
using Content.Server.Cuffs;
using Content.Server.DetailExaminable;
using Content.Server.Emp;
using Content.Server.Flash;
using Content.Server.Flash.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Hands.Systems;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Radium.Changeling.Components;
using Content.Server.Radium.Medical.Surgery.Systems;
using Content.Shared.Camera;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Radium.Changeling;
using Content.Shared.Radium.Changeling.Components;
using Content.Shared.Radium.Changeling.Events;
using Content.Shared.Revenant;
using Content.Shared.StatusEffect;
using Content.Shared.Stealth.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization.Manager;
using HarvestEvent = Content.Shared.Radium.Changeling.HarvestEvent;

// This one has parts of Goob Station's code because I.. Um.. Kinda upset.. Sorry. (https://github.com/Goob-Station/Goob-Station/blob/dd4a7d37ab35ba8c2bbd6dfa72332916ad8805ab/Content.Server/Goobstation/Changeling/ChangelingSystem.cs)

namespace Content.Server.Radium.Changeling.EntitySystems;

public sealed partial class ChangelingSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _heal = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly DamageableSystem _damageSystem = default!;
    [Dependency] private readonly SurgerySystem _surgerySystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly BlindableSystem _blindableSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly CuffableSystem _cuffable = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly PullingSystem _pullingSystem = default!;

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<ChangelingComponent, ChangelingAbsorbDnaActionEvent>(OnAbsorbDNAActions);
        SubscribeLocalEvent<ChangelingComponent, HarvestEvent>(OnHarvest);

        SubscribeLocalEvent<ChangelingComponent, ChangelingStasisActionEvent>(OnStasisAction);
        SubscribeLocalEvent<ChangelingComponent, ChangelingTransformActionEvent>(OnTransformAction);

        SubscribeLocalEvent<ConfirmTransformation>(OnTransformationConfirmed);

        SubscribeLocalEvent<ChangelingComponent, ActionChangelingAdrenalineSacsEvent>(OnAdrenalineSacsEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingStrainedMusclesEvent>(OnToggleStrainedMuscles);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingAnatomicPanaceaEvent>(OnAnatomicPanaceaEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingChitinousArmorEvent>(OnChitinousArmorEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingVoidAdaptationEvent>(OnVoidAdaptationEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingOrganicShieldEvent>(OnOrganicShieldEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingMimicVoiceEvent>(OnMimicVoiceEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingLesserFormEvent>(OnLesserFormEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingFleshmendEvent>(OnFleshmendEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingChameleonSkinEvent>(OnChameleonSkinEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingBiodegradeEvent>(OnBiodegradeEventAction);
        SubscribeLocalEvent<ChangelingComponent, PassiveChangelingAugmentedEyesightEvent>(
            OnAugmentedEyesightEventAction);
        SubscribeLocalEvent<ChangelingComponent, PassiveChangelingDefibrillatorGraspEvent>(
            OnDefibrillatorGraspEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingDnaStingEvent>(OnDnaStingEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingTransformationStingEvent>(
            OnTransformationStingEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingMuteStingEvent>(OnMuteStingEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingBlindStingEvent>(OnBlindStingEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingHallutinationStingEvent>(
            OnHallutinationStingEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingCryogenicStingEvent>(OnCryogenicStingEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingFalseArmbladeStingEvent>(
            OnFalseArmbladeStingEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingArmBladeEvent>(OnArmBladeEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingResonantShriekEvent>(OnResonantShriekEventAction);
        SubscribeLocalEvent<ChangelingComponent, ActionChangelingDissonantShriekEvent>(OnDissonantShriekEventAction);
    }

    private void OnDissonantShriekEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingDissonantShriekEvent args)
    {
        if (!TryUseAbility(uid, args))
            return;

        DoScreech(uid, component);

        var pos = _transform.GetMapCoordinates(uid);
        var power = component.ShriekPower;
        _emp.EmpPulse(pos, power, 5000f, power * 2);
    }

    private void OnResonantShriekEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingResonantShriekEvent args)
    {
        if (!TryUseAbility(uid, args))
            return;

        DoScreech(uid, component);

        var power = component.ShriekPower;
        _flash.FlashArea(uid, uid, power, power * 2f * 1000f);

        var lookup = _lookup.GetEntitiesInRange(uid, power);
        var lights = GetEntityQuery<PoweredLightComponent>();

        foreach (var ent in lookup.Where(ent => lights.HasComponent(ent)))
        {
            _poweredLight.TryDestroyBulb(ent);
        }
    }

    private void OnArmBladeEventAction(EntityUid uid, ChangelingComponent component, ActionChangelingArmBladeEvent args)
    {
        if (!TryUseAbility(uid, args))
            return;

        if (!TryToggleItem(uid, ChangelingEquipment.Armblade))
            return;

        _popup.PopupEntity(component.ChangelingEquipment[ChangelingEquipment.Armblade].Item1.IsValid()
                ? Loc.GetString("changeling-armblade-start")
                : Loc.GetString("changeling-hand-transform-end"),
            uid,
            uid);

        PlayMeatySound(uid);
    }

    private void OnFalseArmbladeStingEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingFalseArmbladeStingEvent args)
    {
        if(!TryUseAbility(uid, args))
            return;

        if (!TrySting(uid, args))
            return;

        var target = args.Target;
        var fakeArmblade = EntityManager.SpawnEntity(
            component.ChangelingEquipment[ChangelingEquipment.FakeArmbladePrototype].Item2,
            Transform(target).Coordinates);
        if (!_handsSystem.TryPickupAnyHand(target, fakeArmblade))
        {
            EntityManager.DeleteEntity(fakeArmblade);
            component.Chemicals += Comp<ChangelingActionComponent>(args.Action).ChemicalCost;
            _popup.PopupEntity(Loc.GetString("changeling-sting-fail-simplemob"), uid, uid);
            return;
        }

        PlayMeatySound(target);
    }

    private void OnCryogenicStingEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingCryogenicStingEvent args)
    {
        if(!TryUseAbility(uid, args))
            return;

        var reagents = new List<(string, FixedPoint2)>
        {
            ("Fresium", 20f),
            ("ChloralHydrate", 10f)
        };

        TryReagentSting(uid, component, args, reagents);
    }

    private void OnHallutinationStingEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingHallutinationStingEvent args)
    {
        if(!TryUseAbility(uid, args))
            return;

        var reagents = new List<(string, FixedPoint2)>
        {
            ("SpaceDrugs", 60f),
        };

        TryReagentSting(uid, component, args, reagents);
    }

    private void OnBlindStingEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingBlindStingEvent args)
    {
        if(!TryUseAbility(uid, args))
            return;

        if (!TrySting(uid, args))
            return;

        var target = args.Target;

        if (!TryComp<BlindableComponent>(target, out var blindable) || blindable.IsBlind)
            return;

        _blindableSystem.AdjustEyeDamage((target, blindable), 5);
        var timeSpan = TimeSpan.FromSeconds(5f);
        _statusEffect.TryAddStatusEffect(target,
            TemporaryBlindnessSystem.BlindingStatusEffect,
            timeSpan,
            false,
            TemporaryBlindnessSystem.BlindingStatusEffect);
    }

    private void OnMuteStingEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingMuteStingEvent args)
    {
        if(!TryUseAbility(uid, args))
            return;

        var reagents = new List<(string, FixedPoint2)>
        {
            ("MuteToxin", 15f),
        };

        TryReagentSting(uid, component, args, reagents);
    }

    private void OnTransformationStingEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingTransformationStingEvent args)
    {
        if(!TryUseAbility(uid, args))
            return;

        throw new NotImplementedException();
    }

    private void OnDnaStingEventAction(EntityUid uid, ChangelingComponent component, ActionChangelingDnaStingEvent args)
    {
        if(!TryUseAbility(uid, args))
            return;

        throw new NotImplementedException();
    }

    private void OnDefibrillatorGraspEventAction(EntityUid uid,
        ChangelingComponent component,
        PassiveChangelingDefibrillatorGraspEvent args)
    {
        PlayMeatySound(uid);
        EnsureComp<ChangelingGraspPassiveComponent>(uid);
        _popup.PopupEntity(Loc.GetString("changeling-passive-activate"), uid, uid);
        PlayMeatySound(uid);
    }

    private void OnAugmentedEyesightEventAction(EntityUid uid,
        ChangelingComponent component,
        PassiveChangelingAugmentedEyesightEvent args)
    {
        PlayMeatySound(uid);
        EnsureComp<FlashImmunityComponent>(uid);
        _popup.PopupEntity(Loc.GetString("changeling-passive-activate"), uid, uid);
        PlayMeatySound(uid);
    }

    private void OnBiodegradeEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingBiodegradeEvent args)
    {
        if (!TryUseAbility(uid, args))
            return;

        if (TryComp<CuffableComponent>(uid, out var cuffs) && cuffs.Container.ContainedEntities.Count > 0)
        {
            var cuff = cuffs.LastAddedCuffs;

            _cuffable.Uncuff(uid, cuffs.LastAddedCuffs, cuff);
            QueueDel(cuff);
        }

        var solution = new Solution();
        solution.AddReagent("PolytrinicAcid", 10f);

        if (_pullingSystem.IsPulled(uid))
        {
            var puller = Comp<PullableComponent>(uid).Puller;
            if (puller != null)
            {
                _puddle.TrySplashSpillAt((EntityUid) puller,
                    Transform((EntityUid) puller).Coordinates,
                    solution,
                    out _);
                return;
            }
        }

        _puddle.TrySplashSpillAt(uid, Transform(uid).Coordinates, solution, out _);
    }

    private void OnChameleonSkinEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingChameleonSkinEvent args)
    {
        if (!TryUseAbility(uid, args))
            return;

        if (HasComp<StealthComponent>(uid) && HasComp<StealthOnMoveComponent>(uid))
        {
            RemComp<StealthComponent>(uid);
            _popup.PopupEntity(Loc.GetString("changeling-chameleon-end"), uid, uid);
            return;
        }

        EnsureComp<StealthComponent>(uid);
        EnsureComp<StealthOnMoveComponent>(uid);
        _popup.PopupEntity(Loc.GetString("changeling-chameleon-start"), uid, uid);
    }

    private void OnFleshmendEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingFleshmendEvent args)
    {
        if (!TryUseAbility(uid, args))
            return;

        var reagents = new List<(string, FixedPoint2)>()
        {
            ("Impedrezene", 2.5f),
            ("Ichor", 15f),
            ("TranexamicAcid", 5f)
        };
        if (TryInjectReagents(uid, reagents))
            _popup.PopupEntity(Loc.GetString("changeling-fleshmend"), uid, uid);
        else
            return;

        PlayMeatySound(uid);
    }

    private void OnLesserFormEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingLesserFormEvent args)
    {
        if(!TryUseAbility(uid, args))
            return;

        throw new NotImplementedException();
    }

    private void OnMimicVoiceEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingMimicVoiceEvent args)
    {
        if(!TryUseAbility(uid, args))
            return;

        throw new NotImplementedException();
    }

    private void OnOrganicShieldEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingOrganicShieldEvent args)
    {
        if (!TryUseAbility(uid, args))
            return;

        if (!TryToggleItem(uid, ChangelingEquipment.Shield))
            return;

        _popup.PopupEntity(component.ChangelingEquipment[ChangelingEquipment.Shield].Item1.IsValid()
                ? Loc.GetString("changeling-shield-start")
                : Loc.GetString("changeling-hand-transform-end"),
            uid,
            uid);

        PlayMeatySound(uid);
    }

    private void OnVoidAdaptationEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingVoidAdaptationEvent args)
    {
        if (!TryUseAbility(uid, args))
            return;

        if (!TryToggleItem(uid, ChangelingEquipment.Spacesuit, "outerClothing")
            || !TryToggleItem(uid, ChangelingEquipment.ArmorHelmet, "head"))
        {
            _popup.PopupEntity(Loc.GetString("changeling-equip-armor-fail"), uid, uid);
            return;
        }

        _popup.PopupEntity(component.ChangelingEquipment[ChangelingEquipment.Spacesuit].Item1.IsValid()
                ? Loc.GetString("changeling-equip-spacesuit-start")
                : Loc.GetString("changeling-equip-end"),
            uid,
            uid);

        PlayMeatySound(uid);
    }

    private void OnChitinousArmorEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingChitinousArmorEvent args)
    {
        if (!TryUseAbility(uid, args))
            return;

        if (!TryToggleItem(uid, ChangelingEquipment.Armor, "outerClothing")
            || !TryToggleItem(uid, ChangelingEquipment.ArmorHelmet, "head"))
        {
            _popup.PopupEntity(Loc.GetString("changeling-equip-armor-fail"), uid, uid);
            return;
        }

        _popup.PopupEntity(component.ChangelingEquipment[ChangelingEquipment.Armor].Item1.IsValid()
                ? Loc.GetString("changeling-equip-armor-start")
                : Loc.GetString("changeling-equip-end"),
            uid,
            uid);

        PlayMeatySound(uid);
    }

    private void OnAnatomicPanaceaEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingAnatomicPanaceaEvent args)
    {
        if (!TryUseAbility(uid, args))
            return;

        var reagents = new List<(string, FixedPoint2)>()
        {
            ("Diphenhydramine", 5f),
            ("Arithrazine", 10f),
            ("Ethylredoxrazine", 5f),
        };
        if (TryInjectReagents(uid, reagents))
            _popup.PopupEntity(Loc.GetString("changeling-panacea"), uid, uid);
        else
            return;
        PlayMeatySound(uid);
    }

    private void OnToggleStrainedMuscles(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingStrainedMusclesEvent args)
    {
        if (!TryUseAbility(uid, args))
            return;

        ToggleStrainedMuscles(uid);
    }

    private void ToggleStrainedMuscles(EntityUid uid, ChangelingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.StrainedMusclesActive)
        {
            _speedModifier.ChangeBaseSpeed(uid, 125f, 150f, 1f);
            _popup.PopupEntity(Loc.GetString("changeling-muscles-start"), uid, uid);
            component.StrainedMusclesActive = true;
        }
        else
        {
            _speedModifier.ChangeBaseSpeed(uid, 100f, 100f, 1f);
            _popup.PopupEntity(Loc.GetString("changeling-muscles-end"), uid, uid);
            component.StrainedMusclesActive = false;
        }

        PlayMeatySound(uid);
    }

    private void OnAdrenalineSacsEventAction(EntityUid uid,
        ChangelingComponent component,
        ActionChangelingAdrenalineSacsEvent args)
    {
        if(!TryUseAbility(uid, args))
            return;

        var reagents = new List<(string, FixedPoint2)>()
        {
            ("ChangelingAdrenaline", 6.5f),
            ("ChangelingStimulator", 4.5f),
        };
        if (TryInjectReagents(uid, reagents))
            _popup.PopupEntity(Loc.GetString("changeling-adrenaline"), uid, uid);
        else
            return;

        PlayMeatySound(uid);
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

        TryUseAbility(uid, args);

        EnsureComp<ResourceComponent>(uid, out var resource);

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

        BeginHarvestDoAfter(uid, target);
    }

    private void BeginHarvestDoAfter(EntityUid uid,
        EntityUid target)
    {
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
            10,
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

        UpdateChemicals(uid, 30);

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

        if (!TryUseAbility(uid, args))
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

        if (!TryUseAbility(uid, args))
        {
            _popup.PopupEntity(
                Robust.Shared.Localization.Loc.GetString("changeling-ability-failed", ("target", uid)),
                uid,
                PopupType.LargeCaution);
            return;
        }

        _userInterface.OpenUi(uid, ChangelingStorageUiKey.Key, uid);
    }

    private void OnTransformationConfirmed(ConfirmTransformation args)
    {
        var uid = GetEntity(args.Uid);
        if (!TryComp<ChangelingComponent>(uid, out var component))
            return;

        var ev = new AfterFlashedEvent(uid, uid, null);
        RaiseLocalEvent(uid, ref ev);

        if (!TryComp<HumanoidAppearanceComponent>(uid, out var targetHumanoid))
        {
            return;
        }

        if (component.SourceHumanoid == null)
            return;

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
        _humanoid.SetTTSVoice(uid, component.SourceHumanoid.Voice, targetHumanoid);

        targetHumanoid.Gender = component.SourceHumanoid.Gender;

        Dirty(uid, targetHumanoid);

        _flash.Flash(target: uid,
            flashDuration: 12000f,
            user: uid,
            used: null,
            slowTo: 0.8F,
            displayPopup: false,
            forced: true);
    }
}
