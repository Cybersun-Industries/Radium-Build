using System.Globalization;
using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Buckle.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Drunk;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Forensics;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.Radium.Medical.Surgery.Components;
using Content.Server.Speech.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Radium.Medical.Surgery.Components;
using Content.Shared.Radium.Medical.Surgery.Events;
using Content.Shared.Radium.Medical.Surgery.Prototypes;
using Content.Shared.Radium.Medical.Surgery.Systems;
using Content.Shared.Stacks;
using Content.Shared.Weapons.Melee;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Content.Server.Radium.Medical.Surgery.Systems;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly IDynamicTypeFactory _dynamicTypeFactory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly DamagePartsSystem _damageParts = default!;
    [Dependency] private readonly DrunkSystem _drunkSystem = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly StutteringSystem _stutteringSystem = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly ForensicsSystem _forensicsSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly BuckleSystem _buckleSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<BeginSurgeryEvent>(OnSurgeryStarted);
        SubscribeLocalEvent<SurgeryInProgressComponent, InteractUsingEvent>(OnSurgeryInteract);
        SubscribeLocalEvent<SurgeryInProgressComponent, SurgeryDoAfterEvent>(OnSurgeryDoAfter);
        SubscribeLocalEvent<MeleeWeaponComponent, DamageChangedEvent>(OnMeleeEvent);
        //SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerSpawnComplete);
        InitializePostActions();
    }

    //private void OnPlayerSpawnComplete(PlayerAttachedEvent ev)
    //{
    //RaiseNetworkEvent(new SyncPartsEvent(GetNetEntity(ev.Entity)), ev.Entity);
    //}


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BodyComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            var a = _damageParts.GetDamagedParts(uid).Where(g => g.Value.Item1 != 0);
            var list = _bodySystem.GetBodyChildren(uid).ToList();
            foreach (var pair in a)
            {
                var part = list.FirstOrNull(g =>
                    g.Component.PartType == pair.Key.Item1 &&
                    g.Component.Symmetry == pair.Key.Item2);
                if (part == null)
                {
                    continue;
                }

                switch (pair.Key.Item1)
                {
                    case BodyPartType.Torso:
                        if (part.Value.Component.Wounds.Count == 0)
                            return;
                        part.Value.Component.AccumulatedFrameTime += frameTime;
                        var frametimeThreshold = part.Value.Component.Wounds.Count switch
                        {
                            >= 2 and <= 5 => 180f,
                            >= 5 and <= 7 => 80f,
                            _ => 100f
                        };
                        if (part.Value.Component.AccumulatedFrameTime <= frametimeThreshold)
                        {
                            break;
                        }

                        if (!TryComp<BloodstreamComponent>(uid, out var component))
                            break;
                        if (!_solutionContainerSystem.ResolveSolution(uid,
                                component.BloodTemporarySolutionName, ref component.TemporarySolution,
                                out var tempSolution))
                            break;
                        if (component.BloodSolution == null)
                            break;
                        var newSol =
                            _solutionContainerSystem.SplitSolution(component.BloodSolution.Value, 50);

                        tempSolution.AddSolution(newSol, _prototypeManager);
                        if (tempSolution.Volume > component.BleedPuddleThreshold)
                        {
                            var amt = component.BloodlossDamage * 20;
                            _damageableSystem.TryChangeDamage(uid, amt);
                            // Pass some of the chemstream into the spilled blood.
                            if (_solutionContainerSystem.ResolveSolution(uid,
                                    component.ChemicalSolutionName, ref component.ChemicalSolution))
                            {
                                var temp = _solutionContainerSystem.SplitSolution(
                                    component.ChemicalSolution.Value, tempSolution.Volume / 10);
                                tempSolution.AddSolution(temp, _prototypeManager);
                            }

                            if (_puddleSystem.TrySpillAt(uid, tempSolution, out var puddleUid, false))
                            {
                                _forensicsSystem.TransferDna(puddleUid, uid, false);
                            }

                            tempSolution.RemoveAllSolution();
                            _player.TryGetSessionByEntity(uid, out var session);
                            if (session != null)
                            {
                                _popupSystem.PopupEntity(Loc.GetString("surgery-bleeding"), uid, session,
                                    PopupType.LargeCaution);
                            }
                            else
                            {
                                _popupSystem.PopupEntity(Loc.GetString("surgery-bleeding"), uid,
                                    PopupType.LargeCaution);
                            }
                        }


                        _solutionContainerSystem.UpdateChemicals(component.TemporarySolution.Value);
                        _stutteringSystem.DoStutter(uid, new TimeSpan(0, 0, 0, 7), true);
                        part.Value.Component.AccumulatedFrameTime = 0f;
                        break;
                    case BodyPartType.Head:
                        if (part.Value.Component.AccumulatedFrameTime <= 5)
                        {
                            part.Value.Component.AccumulatedFrameTime += frameTime;
                            break;
                        }

                        switch (part.Value.Component.Wounds.Count)
                        {
                            case >= 1 and < 3:
                                _drunkSystem.TryApplyDrunkenness(uid, 20);
                                break;
                            case > 3 and < 5:
                                _drunkSystem.TryApplyDrunkenness(uid, 28);
                                break;
                            case > 5:
                                _drunkSystem.TryApplyDrunkenness(uid, 36);
                                break;
                        }

                        part.Value.Component.AccumulatedFrameTime = 0;

                        break;
                    case BodyPartType.Arm:
                        break;
                    case BodyPartType.Leg:
                        if (part.Value.Component.AccumulatedFrameTime <= 10)
                        {
                            part.Value.Component.AccumulatedFrameTime += frameTime;
                            break;
                        }

                        if (!TryComp<MovementSpeedModifierComponent>(uid, out var move))
                            break;
                        var currentBaseSprintSpeed = move.BaseSprintSpeed;
                        switch (part.Value.Component.Wounds.Count)
                        {
                            case > 2 and < 6:
                                if (currentBaseSprintSpeed <= 3f)
                                {
                                    break;
                                }

                                _movement.ChangeBaseSpeed(uid, 2f, 3f, 8);
                                break;
                            case > 6:
                                if (currentBaseSprintSpeed <= 2f)
                                {
                                    break;
                                }

                                _movement.ChangeBaseSpeed(uid, 1f, 2f, 4);
                                break;
                        }

                        part.Value.Component.AccumulatedFrameTime = 0;
                        break;
                    //Empty
                    case BodyPartType.Other:
                        break;
                    case BodyPartType.Hand:
                        break;
                    case BodyPartType.Foot:
                        break;
                    case BodyPartType.Tail:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    private void OnMeleeEvent(EntityUid uid, MeleeWeaponComponent component, DamageChangedEvent args)
    {
        if (args.Origin == null || args.DamageDelta == null || args.DamageDelta.GetTotal() < 5)
            return;

        foreach (var keyValuePair in args.DamageDelta.DamageDict)
        {
            if (keyValuePair.Key == "Slash")
            {
                TryApplySurgeryDamage(uid, WoundTypeEnum.Piercing);
                continue;
            }

            if (!Enum.TryParse<WoundTypeEnum>(keyValuePair.Key, out var dmg))
                continue;
            switch (dmg)
            {
                case WoundTypeEnum.Blunt:
                    TryApplySurgeryDamage(uid, WoundTypeEnum.Blunt);
                    break;
                case WoundTypeEnum.Heat:
                    TryApplySurgeryDamage(uid, WoundTypeEnum.Heat);
                    break;
                case WoundTypeEnum.Piercing:
                    TryApplySurgeryDamage(uid, WoundTypeEnum.Piercing);
                    break;
                default:
                    return;
            }
        }

        RaiseNetworkEvent(new SyncPartsEvent(GetNetEntity(uid)), uid);
    }

    private BodyPartSymmetry RandomizeSymmetry(SurgeryPartEnum part)
    {
        if (part is SurgeryPartEnum.Head or SurgeryPartEnum.Torso)
            return BodyPartSymmetry.None;
        return _random.Next(0, 2) switch
        {
            0 => BodyPartSymmetry.Left,
            1 => BodyPartSymmetry.Right,
            _ => BodyPartSymmetry.None
        };
    }

    private SurgeryPartEnum RandomizePart(EntityUid uid, WoundTypeEnum woundType)
    {
        var list = _bodySystem.GetBodyChildren(uid);
        var chanceModifier = 3;
        if (woundType == WoundTypeEnum.Piercing)
        {
            chanceModifier += 4;
        }

        foreach (var currentPart in list)
        {
            if (!TryComp<BodyPartComponent>(currentPart.Id, out var part))
                continue;
            chanceModifier += part.Wounds.Count;
        }

        if (_random.NextFloat(0f, 101f) <= Math.Clamp(90f - chanceModifier * 5, 0f, 100f))
            return SurgeryPartEnum.None;
        return _random.NextFloat(0f, 101f) switch
        {
            >= 85 and <= 100 => SurgeryPartEnum.Head,
            >= 55 and <= 85 => SurgeryPartEnum.Arm,
            >= 25 and <= 55 => SurgeryPartEnum.Torso,
            _ => SurgeryPartEnum.Leg
        };
    }
/*
    private bool ShouldDamageOrgan(EntityUid uid, WoundTypeEnum woundType)
    {
        var list = _bodySystem.GetBodyChildren(uid);
        var chanceModifier = 0;
        if (woundType == WoundTypeEnum.Piercing)
        {
            chanceModifier += 10;
        }

        foreach (var currentPart in list)
        {
            if (!TryComp<BodyPartComponent>(currentPart.Id, out var part))
                continue;
            chanceModifier += part.Wounds.Count;
        }

        return !(_random.NextFloat(0f, 100f) <= Math.Clamp(95f - chanceModifier * 5, 0f, 100f));
    }
    */

    private void TryApplySurgeryDamage(EntityUid uid, WoundTypeEnum woundType)
    {
        var part = RandomizePart(uid, woundType);
        var symmetry = RandomizeSymmetry(part);
        if (part == SurgeryPartEnum.None)
        {
            return;
        }

        var list = _bodySystem.GetBodyChildren(uid).ToList();
        var partComponentRaw =
            list.FirstOrNull(p =>
                p.Component.PartType == Enum.Parse<BodyPartType>(part.ToString()) &&
                p.Component.Symmetry == Enum.Parse<BodyPartSymmetry>(symmetry.ToString()));

        if (partComponentRaw == null)
        {
            return;
        }

        var partComponentId = partComponentRaw.Value.Id;
        if (!TryComp<BodyPartComponent>(partComponentId, out var partComponent))
            return;
        TryComp<MetaDataComponent>(partComponentId, out var metadata);
        switch (woundType)
        {
            case WoundTypeEnum.Blunt:
                partComponent.Wounds.Add(new PartWound(WoundTypeEnum.Blunt));
                break;
            case WoundTypeEnum.Piercing:
                partComponent.Wounds.Add(new PartWound(WoundTypeEnum.Piercing));
                break;
            case WoundTypeEnum.Heat:
                partComponent.Wounds.Add(new PartWound(WoundTypeEnum.Heat));
                break;
            default:
                return;
        }

        Dirty(partComponentId, partComponent, metadata);

        if (part is not (SurgeryPartEnum.Arm or SurgeryPartEnum.Leg) || partComponent.Wounds.Count < 7)
            return;

        var additionalPart = part switch
        {
            SurgeryPartEnum.Arm => "Hand",
            SurgeryPartEnum.Leg => "Foot"
        };
        var arm = list.First(p =>
            p.Component.PartType == Enum.Parse<BodyPartType>(part.ToString()) &&
            p.Component.Symmetry == symmetry);
        var hand = list.First(p =>
            p.Component.PartType == Enum.Parse<BodyPartType>(additionalPart) &&
            p.Component.Symmetry == symmetry);
        _xformSystem.AttachToGridOrMap(hand.Id);
        _xformSystem.AttachToGridOrMap(arm.Id);
    }

    private void OnSurgeryDoAfter(EntityUid uid, SurgeryInProgressComponent component, SurgeryDoAfterEvent args)
    {
        if (args.Cancelled || component.SurgeryPrototypeId == null ||
            !_prototypeManager.TryIndex<SurgeryOperationPrototype>(component.SurgeryPrototypeId,
                out var surgeryOperationPrototype) || surgeryOperationPrototype.Steps == null ||
            !TryComp<SurgeryInProgressComponent>(args.Target, out var surgery))
            return;
        _prototypeManager.TryIndex<SurgeryOperationPrototype>(component.SurgeryPrototypeId,
            out var test);
        var nextStep = surgery.CurrentStep;
        if (surgery.CurrentStep is { Repeatable: true })
        {
            if (!_prototypeManager.TryIndex<SurgeryOperationPrototype>(surgery.SurgeryPrototypeId ?? string.Empty,
                    out var operation))
                return;
            if (operation.Steps == null)
            {
                return;
            }

            var type = _reflectionManager.LooseGetType(surgeryOperationPrototype.EventKey);
            var ev = _dynamicTypeFactory.CreateInstance(type,
                new object[] { uid, surgeryOperationPrototype.ID, component.Symmetry });
            RaiseLocalEvent(ev);
            if (!TryComp<SurgeryInProgressComponent>(uid, out var newComp))
                return;
            if (newComp.CurrentStep is { Repeatable: false })
            {
                goto Check;
            }

            var repeatIndex = operation.Steps[surgery.CurrentStep.StepIndex].RepeatIndex;
            nextStep = operation.Steps[repeatIndex];

            nextStep.StepIndex = surgery.CurrentStep.RepeatIndex;
            nextStep.Repeatable = true;
            surgery.CurrentStep = nextStep;
            return;
        }

        Check:
        if (surgery.CurrentStep != null && surgeryOperationPrototype.Steps.Count == surgery.CurrentStep.StepIndex + 1)
        {
            if (args.Used != null && HasComp<StackComponent>(args.Used))
            {
                _stackSystem.Use(args.Used.Value, 1);
            }


            var type = _reflectionManager.LooseGetType(surgeryOperationPrototype.EventKey);
            object? ev = null;
            if (component.CurrentStep != null)
            {
                var stepAction = Enum.Parse<SurgeryTypeEnum>(surgeryOperationPrototype
                    .Steps[component.CurrentStep.StepIndex - 1].Key.ToString());
                if (stepAction is not
                    (SurgeryTypeEnum.AddPart or SurgeryTypeEnum.AddAdditionalPart))
                {
                    ev = _dynamicTypeFactory.CreateInstance(type,
                        new object[] { uid, surgeryOperationPrototype.ID, component.Symmetry });
                }
            }

            if (ev != null)
                RaiseLocalEvent(ev);
            RemComp<SurgeryInProgressComponent>(uid);
            _bloodstreamSystem.TryModifyBleedAmount(args.Target.Value, -100);
            _drunkSystem.TryRemoveDrunkenness(args.Target.Value);
            return;
        }

        if (surgery.CurrentStep != null)
        {
            var action = Enum.Parse<SurgeryTypeEnum>(surgery.CurrentStep.Key.ToString());
            var index = surgery.CurrentStep.StepIndex + 1;
            if (action == SurgeryTypeEnum.Repair)
            {
                if (args.Used != null && !_stackSystem.Use(args.Used.Value, 1))
                    return;
            }

            nextStep = surgeryOperationPrototype.Steps[index];
            nextStep.StepIndex = index - 1;
            nextStep.StepIndex++;
            if (action is not (SurgeryTypeEnum.Clamp or SurgeryTypeEnum.Burn))
            {
                _bloodstreamSystem.TryModifyBleedAmount(args.Target.Value, 10f);
                _drunkSystem.TryApplyDrunkenness(args.Target.Value, 60);
            }
            else
            {
                _bloodstreamSystem.TryModifyBleedAmount(args.Target.Value, -100);
                _drunkSystem.TryRemoveDrunkenness(args.Target.Value);
            }

            if (action is SurgeryTypeEnum.AddPart or SurgeryTypeEnum.AddAdditionalPart)
            {
                if (args.Used != null)
                {
                    var type = _reflectionManager.LooseGetType(surgeryOperationPrototype.EventKey);
                    var ev = _dynamicTypeFactory.CreateInstance(type,
                        new object[] { uid, surgeryOperationPrototype.ID, component.Symmetry, args.Used });
                    RaiseLocalEvent(ev);
                }
            }
        }

        if (HasComp<SurgeryInProgressComponent>(uid))
        {
            surgery.CurrentStep = nextStep;
        }

        UpdateStepIcon(ref surgery);
    }


    private void OnSurgeryInteract(EntityUid uid, SurgeryInProgressComponent component, InteractUsingEvent args)
    {
        float time;
        if (!_prototypeManager.TryIndex<SurgeryOperationPrototype>(component.SurgeryPrototypeId!, out var operation))
            return;
        if (operation.BodyPart == "Eyes")
        {
            operation.BodyPart = "Head";
        }
        var origin = Enum.Parse<BodyPartType>(operation.BodyPart);
        var additionalPart = origin switch
        {
            BodyPartType.Arm => BodyPartType.Hand,
            BodyPartType.Leg => BodyPartType.Foot,
            _ => BodyPartType.Other
        };
        if (component.CurrentStep == null)
            return;

        var key = Enum.Parse<SurgeryTypeEnum>(component.CurrentStep.Key.ToString());
        if (TryComp<BodyPartComponent>(args.Used, out var partUsed) &&
            (
                key == SurgeryTypeEnum.AddPart &&
                partUsed.PartType == origin &&
                partUsed.Symmetry == Enum.Parse<BodyPartSymmetry>(component.Symmetry.ToString())
                || key == SurgeryTypeEnum.AddAdditionalPart &&
                partUsed.PartType == additionalPart &&
                partUsed.Symmetry == Enum.Parse<BodyPartSymmetry>(component.Symmetry.ToString())
            ))
        {
            time = 15f;
            goto G;
        }

        if (
            component.CurrentStep == null ||
            !TryComp<SurgeryToolComponent>(args.Used, out var tool) ||
            !Equals(tool.Key, component.CurrentStep?.Key))
        {
            return;
        }

        time = 10 / tool.Modifier;
        G:
        var chance = 0;
        if (time > 20)
        {
            chance = int.Parse(((time / 20 - 1) * 10).ToString(CultureInfo.InvariantCulture));
        }

        if (!_buckleSystem.IsBuckled(uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("surgery-target-shouldBuckled"), uid, PopupType.Medium);
            return;
        }

        var doArgs =
            new DoAfterArgs(EntityManager, args.User, time, new SurgeryDoAfterEvent(chance), args.Target, args.Target,
                args.Used)
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnTargetMove = true,
                BreakOnHandChange = true,
                RequireCanInteract = true,
                BreakOnWeightlessMove = true
            };
        _doAfterSystem.TryStartDoAfter(doArgs);
    }


    private void OnSurgeryStarted(BeginSurgeryEvent ev)
    {
        if (ev.PrototypeId == null)
        {
            return;
        }

        var entity = _entityManager.GetEntity(ev.Uid);

        if (!_prototypeManager.TryIndex<SurgeryOperationPrototype>(ev.PrototypeId, out var operationPrototype))
        {
            return;
        }

        _entityManager.EnsureComponent<SurgeryInProgressComponent>(entity, out var surgeryComponent);

        surgeryComponent.CurrentStep = operationPrototype.Steps![0];
        surgeryComponent.Symmetry = ev.Symmetry;
        UpdateStepIcon(ref surgeryComponent);
        surgeryComponent.SurgeryPrototypeId = ev.PrototypeId;
        _popupSystem.PopupEntity(Loc.GetString("surgery-target-begin"), entity, PopupType.LargeCaution);
    }

    private static void UpdateStepIcon(ref SurgeryInProgressComponent component)
    {
        if (component.CurrentStep != null)
        {
            component.CurrentStep.Icon = component.CurrentStep.Key switch
            {
                //Refer to RSI instead... TODO
                SurgeryTypeEnum.Bandage => "/Texture/Objects/Specific/Medical/medical.rsi/gauze.png",
                SurgeryTypeEnum.Burn => "/Textures/Objects/Specific/Medical/Surgery/cautery.rsi/cautery.png",
                SurgeryTypeEnum.Clamp => "/Textures/Objects/Specific/Medical/Surgery/scissors.rsi/hemostat.png",
                SurgeryTypeEnum.Cut => "/Textures/Objects/Specific/Medical/Surgery/saw.rsi/advanced.png",
                SurgeryTypeEnum.Filter => "/Textures/Objects/Specific/Medical/medical.rsi/bloodpack.png",
                SurgeryTypeEnum.Incise => "/Textures/Objects/Specific/Medical/Surgery/scalpel.rsi/advanced.png",
                SurgeryTypeEnum.Repair => "/Textures/Radium/Objects/Specific/Medical/healing.rsi/bone_mesh.png",
                SurgeryTypeEnum.Retract => "/Textures/Objects/Specific/Medical/Surgery/scissors.rsi/retractor.png",
                SurgeryTypeEnum.Revive => "/Textures/Objects/Specific/Medical/defib.rsi/icon.png",
                SurgeryTypeEnum.AddPart => "/Textures/Objects/Specific/Medical/handheldcrewmonitor.rsi/icon.png",
                SurgeryTypeEnum.AddAdditionalPart => "/Textures/Objects/Specific/Medical/handheldcrewmonitor.rsi/icon.png",
                _ => component.CurrentStep.Icon
            };
        }
    }
}
