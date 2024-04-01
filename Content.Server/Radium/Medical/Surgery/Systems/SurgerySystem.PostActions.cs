using System.Linq;
using Content.Server.Body.Systems;
using Content.Server.Radium.Medical.Surgery.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Popups;
using Content.Shared.Radium.Medical.Surgery.Components;
using Content.Shared.Radium.Medical.Surgery.Events;
using Content.Shared.Radium.Medical.Surgery.Prototypes;
using Content.Shared.StatusEffect;
using Content.Shared.Traits.Assorted;
using FastAccessors;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.Radium.Medical.Surgery.Systems;

public sealed partial class SurgerySystem
{
    [Dependency] private readonly TransformSystem _xformSystem = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly BlindableSystem _blindableSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    private void InitializePostActions()
    {
        //No comments...
        SubscribeLocalEvent<LumaSurgeryEvent>(OnLumaSurgeryPostAction); //DISABLED
        SubscribeLocalEvent<OrganManipSurgeryEvent>(OnOrganManipSurgeryPostAction); //DISABLED
        SubscribeLocalEvent<FeatureManipSurgeryEvent>(OnFeatureManipSurgeryPostAction); //DISABLED
        SubscribeLocalEvent<LobectomySurgeryEvent>(OnLobectomySurgeryAction); //DISABLED
        SubscribeLocalEvent<CoronaryBypassSurgeryEvent>(OnCoronaryBypassSurgeryPostAction); //DISABLED
        SubscribeLocalEvent<HepatectomySurgeryEvent>(OnHepatectomySurgeryPostAction); //DISABLED
        SubscribeLocalEvent<GastrectomySurgeryEvent>(OnGastrectomySurgeryPostAction); //DISABLED
        SubscribeLocalEvent<AmputationSurgeryEvent>(OnAmputationSurgeryPostAction); //DONE
        SubscribeLocalEvent<ImplantSurgeryEvent>(OnImplantSurgeryPostAction); //DISABLED
        SubscribeLocalEvent<ImplantRemovalSurgeryEvent>(OnImplantRemovalSurgeryPostAction); //DISABLED
        SubscribeLocalEvent<EyeBlindSurgeryEvent>(OnEyeBlindSurgeryPostAction);
        SubscribeLocalEvent<EyeSurgeryEvent>(OnEyeSurgeryPostAction); //DONE
        SubscribeLocalEvent<RevivalSurgeryEvent>(OnRevivalSurgeryPostAction); //DISABLED
        SubscribeLocalEvent<FilterSurgeryEvent>(OnFilterSurgeryPostAction); //DISABLED
        SubscribeLocalEvent<StomachPumpSurgeryEvent>(OnStomachPumpSurgeryPostAction); //DISABLED
        SubscribeLocalEvent<RepairBoneFSurgeryEvent>(OnRepairBoneFSurgeryPostAction); //DONE_TESTING_REQUIRED
        SubscribeLocalEvent<RepairCompFSurgeryEvent>(OnLRepairCompFSurgeryPostAction); //DONE_TESTING_REQUIRED
        SubscribeLocalEvent<BurnSurgeryEvent>(OnBurnSurgeryPostAction); //DONE_TESTING_REQUIRED
        SubscribeLocalEvent<PierceSurgeryEvent>(OnPierceSurgeryPostAction); //DONE
        SubscribeLocalEvent<AddSurgeryEvent>(OnAddSurgeryEventPostAction);
    }

    private void OnAddSurgeryEventPostAction(AddSurgeryEvent ev)
    {
        if (!TryGetOperationPrototype(ev.PrototypeId, out var operationPrototype))
            return;
        if (!TryComp<SurgeryInProgressComponent>(ev.Uid, out var surgeryInProgressComponent) ||
            surgeryInProgressComponent.CurrentStep == null)
            return;


        var t1 = Enum.Parse<SurgeryTypeEnum>(surgeryInProgressComponent.CurrentStep.Key.ToString());
        var partType = Enum.Parse<BodyPartType>(operationPrototype.BodyPart);
        if (t1 == SurgeryTypeEnum.AddPart)
        {
            var root = _bodySystem.GetRootPartOrNull(ev.Uid);
            if (root == null)
            {
                return;
            }

            _xformSystem.AttachToGridOrMap(ev.PartUid);
            var slotgId = _bodySystem.GetBodyAllSlots(ev.Uid).First(g => g.Type == partType).Id;
            slotgId = Enum.Parse<BodyPartSymmetry>(surgeryInProgressComponent.Symmetry.ToString()) ==
                      BodyPartSymmetry.Left
                ? slotgId.Replace("right", "left")
                : slotgId.Replace("left", "right");
            _bodySystem.AttachPart(root.Value.Entity, slotgId, ev.PartUid);
            return;
        }

        var list = _bodySystem.GetBodyChildren(ev.Uid).ToList();

        var additionalPart = partType switch
        {
            BodyPartType.Arm => BodyPartType.Hand,
            BodyPartType.Leg => BodyPartType.Foot
        };
        var part = list.FirstOrNull(p =>
            p.Component.PartType == partType &&
            p.Component.Symmetry == Enum.Parse<BodyPartSymmetry>(ev.Symmetry.ToString()));
        if (part == null)
        {
            //No arm detected
            return;
        }

        var slot = _bodySystem.GetAllBodyPartSlots(part.Value.Id).First(g => g.Type == additionalPart); //WRONG!
        _xformSystem.AttachToGridOrMap(ev.PartUid);
        _bodySystem.AttachPart(part.Value.Id, slot, ev.PartUid);

        RaiseNetworkEvent(new SyncPartsEvent(GetNetEntity(ev.Uid)), ev.Uid);
    }

    #region OnEyeBlindSurgeryPostAction

    private void OnEyeBlindSurgeryPostAction(EyeBlindSurgeryEvent ev)
    {
        AddComp<PermanentBlindnessComponent>(ev.Uid);
        _blindableSystem.UpdateIsBlind(ev.Uid);
    }

    #endregion

    #region OnPierceSurgeryPostAction

    private void OnPierceSurgeryPostAction(PierceSurgeryEvent ev)
    {
        if (!TryComp<SurgeryInProgressComponent>(ev.Uid, out var surgeryInProgressComponent))
            return;
        var operation = _prototypeManager.Index<SurgeryOperationPrototype>(ev.PrototypeId);
        var damagedParts = _bodySystem.GetBodyChildren(ev.Uid).Where(g =>
            g.Component.Wounds.Count > 0 &&
            g.Component.PartType == Enum.Parse<BodyPartType>(operation.BodyPart) &&
            g.Component.Symmetry == ev.Symmetry).ToList();
        if (damagedParts.ToList().Count == 0)
        {
            if (surgeryInProgressComponent.CurrentStep != null)
                surgeryInProgressComponent.CurrentStep.Repeatable = false;
            return;
        }

        foreach (var damagedPart in damagedParts)
        {
            var part = damagedPart.Component.Wounds.Where(e => e.Type == WoundTypeEnum.Piercing).ToList();
            if (part.ToList().Count != 0)
            {
                damagedPart.Component.Wounds.Remove(part.First());
            }
            else
            {
                if (surgeryInProgressComponent.CurrentStep != null)
                    surgeryInProgressComponent.CurrentStep.Repeatable = false;
            }

            Dirty(damagedPart.Id, damagedPart.Component);
            RaiseNetworkEvent(new SyncPartsEvent(GetNetEntity(ev.Uid)), ev.Uid);
        }
    }

    #endregion

    #region OnBurnSurgeryPostAction

    private void OnBurnSurgeryPostAction(BurnSurgeryEvent ev)
    {
        var operation = _prototypeManager.Index<SurgeryOperationPrototype>(ev.PrototypeId);
        var damagedParts = _bodySystem.GetBodyChildren(ev.Uid).Where(g =>
            g.Component.Wounds.Count is > 0 &&
            g.Component.PartType == Enum.Parse<BodyPartType>(operation.BodyPart) &&
            g.Component.Symmetry == ev.Symmetry).ToList();
        if (damagedParts.ToList().Count == 0)
        {
            if (!TryComp<SurgeryInProgressComponent>(ev.Uid, out var surgeryInProgressComponent))
                return;
            if (surgeryInProgressComponent.CurrentStep != null)
                surgeryInProgressComponent.CurrentStep.Repeatable = false;
            _popupSystem.PopupEntity("Операция провалена!", ev.Uid, PopupType.LargeCaution);
            _damageableSystem.TryChangeDamage(ev.Uid,
                new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"), 20));
            RemComp<SurgeryInProgressComponent>(ev.Uid);
            return;
        }

        foreach (var damagedPart in damagedParts)
        {
            var part = damagedPart.Component.Wounds.Where(e => e.Type == WoundTypeEnum.Heat).ToList();
            if (part.ToList().Count != 0)
            {
                damagedPart.Component.Wounds.RemoveAll(i => i.Type == WoundTypeEnum.Heat);
                _popupSystem.PopupEntity("Рана обработана.", ev.Uid, PopupType.LargeGreen);
            }

            Dirty(damagedPart.Id, damagedPart.Component);
            RaiseNetworkEvent(new SyncPartsEvent(GetNetEntity(ev.Uid)), ev.Uid);
        }
    }

    #endregion

    #region OnLRepairCompFSurgeryPostAction

    private void OnLRepairCompFSurgeryPostAction(RepairCompFSurgeryEvent ev)
    {
        var operation = _prototypeManager.Index<SurgeryOperationPrototype>(ev.PrototypeId);
        var damagedParts = _bodySystem.GetBodyChildren(ev.Uid).Where(g =>
            g.Component.Wounds.Count > 0 &&
            g.Component.PartType == Enum.Parse<BodyPartType>(operation.BodyPart) &&
            g.Component.Symmetry == ev.Symmetry).ToList();
        if (damagedParts.ToList().Count == 0)
        {
            if (!TryComp<SurgeryInProgressComponent>(ev.Uid, out var surgeryInProgressComponent))
                return;
            if (surgeryInProgressComponent.CurrentStep != null)
                surgeryInProgressComponent.CurrentStep.Repeatable = false;
            _popupSystem.PopupEntity("Операция провалена!", ev.Uid, PopupType.LargeCaution);
            _damageableSystem.TryChangeDamage(ev.Uid,
                new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"), 20));
            RemComp<SurgeryInProgressComponent>(ev.Uid);
            return;
        }

        foreach (var damagedPart in damagedParts)
        {
            var part = damagedPart.Component.Wounds.Where(e => e.Type == WoundTypeEnum.Blunt).ToList();
            if (part.ToList().Count != 0)
            {
                damagedPart.Component.Wounds.RemoveAll(i => i.Type == WoundTypeEnum.Blunt);
                _popupSystem.PopupEntity("Кость восстановлена.", ev.Uid, PopupType.LargeGreen);
            }

            Dirty(damagedPart.Id, damagedPart.Component);
            RaiseNetworkEvent(new SyncPartsEvent(GetNetEntity(ev.Uid)), ev.Uid);
        }
    }

    #endregion

    #region OnRepairBoneFSurgeryPostAction

    private void OnRepairBoneFSurgeryPostAction(RepairBoneFSurgeryEvent ev)
    {
        var operation = _prototypeManager.Index<SurgeryOperationPrototype>(ev.PrototypeId);
        var damagedParts = _bodySystem.GetBodyChildren(ev.Uid).Where(g =>
            g.Component.Wounds.Count is > 0 and < 5 &&
            g.Component.Wounds.Where(i => i.Type == WoundTypeEnum.Blunt).ToList().Count != 0 &&
            g.Component.PartType == Enum.Parse<BodyPartType>(operation.BodyPart) &&
            g.Component.Symmetry == ev.Symmetry).ToList();
        if (damagedParts.ToList().Count == 0)
        {
            if (!TryComp<SurgeryInProgressComponent>(ev.Uid, out var surgeryInProgressComponent))
                return;
            if (surgeryInProgressComponent.CurrentStep != null)
                surgeryInProgressComponent.CurrentStep.Repeatable = false;
            _popupSystem.PopupEntity("Операция провалена!", ev.Uid, PopupType.LargeCaution);
            _damageableSystem.TryChangeDamage(ev.Uid,
                new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 20));
            RemComp<SurgeryInProgressComponent>(ev.Uid);
            return;
        }

        foreach (var damagedPart in damagedParts)
        {
            var part = damagedPart.Component.Wounds.Where(e => e.Type == WoundTypeEnum.Blunt).ToList();
            if (part.ToList().Count != 0)
            {
                damagedPart.Component.Wounds.RemoveAll(i => i.Type == WoundTypeEnum.Blunt);
                _popupSystem.PopupEntity("Кость зафиксирована.", ev.Uid, PopupType.LargeGreen);
            }

            Dirty(damagedPart.Id, damagedPart.Component);
            RaiseNetworkEvent(new SyncPartsEvent(GetNetEntity(ev.Uid)), ev.Uid);
        }
    }

    #endregion

    #region OnStomachPumpSurgeryPostAction

    private void OnStomachPumpSurgeryPostAction(StomachPumpSurgeryEvent ev)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region OnFilterSurgeryPostAction

    private void OnFilterSurgeryPostAction(FilterSurgeryEvent ev)
    {
        //DISABLED
    }

    #endregion

    #region OnRevivalSurgeryPostAction

    private void OnRevivalSurgeryPostAction(RevivalSurgeryEvent ev)
    {
        //DISABLED
    }

    #endregion

    #region OnEyeSurgeryPostAction

    private void OnEyeSurgeryPostAction(EyeSurgeryEvent ev)
    {
        _statusEffectsSystem.TryRemoveStatusEffect(ev.Uid, TemporaryBlindnessSystem.BlindingStatusEffect);
        RemComp<TemporaryBlindnessComponent>(ev.Uid);
        RemComp<PermanentBlindnessComponent>(ev.Uid);
        _blindableSystem.AdjustEyeDamage(ev.Uid, -100);
    }

    #endregion

    #region OnImplantRemovalSurgeryPostAction

    private void OnImplantRemovalSurgeryPostAction(ImplantRemovalSurgeryEvent ev)
    {
        //DISABLED
    }

    #endregion

    #region OnImplantSurgeryPostAction

    private void OnImplantSurgeryPostAction(ImplantSurgeryEvent ev)
    {
        //DISABLED
    }

    #endregion

    #region OnAmputationSurgeryPostAction

    private void OnAmputationSurgeryPostAction(AmputationSurgeryEvent ev)
    {
        RemComp<SurgeryInProgressComponent>(ev.Uid);
        if (!TryGetOperationPrototype(ev.PrototypeId, out var operationPrototype))
            return;
        TryComp<BodyComponent>(ev.Uid, out var bodyComponent);
        var list = _bodySystem.GetBodyChildren(ev.Uid);

        var valueTuples = list as (EntityUid Id, BodyPartComponent Component)[] ?? list.ToArray();
        var partEnum = Enum.Parse<SurgeryPartEnum>(operationPrototype.BodyPart);
        if (partEnum is SurgeryPartEnum.Arm or SurgeryPartEnum.Leg)
        {
            var additionalPart = partEnum switch
            {
                SurgeryPartEnum.Arm => "Hand",
                SurgeryPartEnum.Leg => "Foot"
            };
            var arm = valueTuples.First(p =>
                p.Component.PartType == Enum.Parse<BodyPartType>(operationPrototype.BodyPart) &&
                p.Component.Symmetry == Enum.Parse<BodyPartSymmetry>(ev.Symmetry.ToString()));
            var hand = valueTuples.First(p =>
                p.Component.PartType == Enum.Parse<BodyPartType>(additionalPart) &&
                p.Component.Symmetry == Enum.Parse<BodyPartSymmetry>(ev.Symmetry.ToString()));
            _xformSystem.AttachToGridOrMap(hand.Id);
            _xformSystem.AttachToGridOrMap(arm.Id);
        }
        else
        {
            var part = valueTuples.First(p =>
                p.Component.PartType == Enum.Parse<BodyPartType>(operationPrototype.BodyPart) &&
                p.Component.Symmetry == Enum.Parse<BodyPartSymmetry>(ev.Symmetry.ToString()));

            _xformSystem.AttachToGridOrMap(part.Id);
        }

        RaiseNetworkEvent(new SyncPartsEvent(GetNetEntity(ev.Uid)), ev.Uid);
    }

    #endregion

    #region OnGastrectomySurgeryPostAction

    private void OnGastrectomySurgeryPostAction(GastrectomySurgeryEvent ev)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region OnHepatectomySurgeryPostAction

    private void OnHepatectomySurgeryPostAction(HepatectomySurgeryEvent ev)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region OnCoronaryBypassSurgeryPostAction

    private void OnCoronaryBypassSurgeryPostAction(CoronaryBypassSurgeryEvent ev)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region OnLobectomySurgeryAction

    private void OnLobectomySurgeryAction(LobectomySurgeryEvent ev)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region OnFeatureManipSurgeryPostAction

    private void OnFeatureManipSurgeryPostAction(FeatureManipSurgeryEvent ev)
    {
        //DISABLED
        throw new NotImplementedException();
    }

    #endregion

    #region OnOrganManipSurgeryPostAction

    private void OnOrganManipSurgeryPostAction(OrganManipSurgeryEvent ev)
    {
        //DISABLED
        throw new NotImplementedException();
    }

    #endregion

    #region OnLumaSurgeryPostAction

    private void OnLumaSurgeryPostAction(LumaSurgeryEvent ev)
    {
        Logger.GetSawmill("DEBUG").Error("Luma event raised");
        RemComp<SurgeryInProgressComponent>(ev.Uid);
    }

    #endregion
}
