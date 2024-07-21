using Content.Shared.Actions;
using Content.Shared.DoAfter;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Radium.Changeling;

[Serializable, NetSerializable]
public sealed partial class SoulEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class HarvestEvent : SimpleDoAfterEvent;


public sealed class HarvestDoAfterComplete(EntityUid target) : EntityEventArgs
{
    public readonly EntityUid Target = target;
}

public sealed class HarvestDoAfterCancelled : EntityEventArgs;

public sealed partial class ChangelingAbsorbDnaActionEvent : EntityTargetActionEvent;

public sealed partial class ChangelingShopActionEvent : InstantActionEvent;

public sealed partial class ChangelingStasisActionEvent : InstantActionEvent;

public sealed partial class ChangelingTransformActionEvent : InstantActionEvent;

public sealed partial class ActionChangelingRegenerateEvent : InstantActionEvent;

public sealed partial class ActionChangelingAdrenalineSacsEvent : InstantActionEvent;

public sealed partial class ActionChangelingStrainedMusclesEvent : InstantActionEvent;

public sealed partial class ActionChangelingAnatomicPanaceaEvent : InstantActionEvent;

public sealed partial class ActionChangelingChitinousArmorEvent : InstantActionEvent;

public sealed partial class ActionChangelingVoidAdaptationEvent : InstantActionEvent;

public sealed partial class ActionChangelingOrganicShieldEvent : InstantActionEvent;

public sealed partial class ActionChangelingFleshmendEvent : InstantActionEvent;

public sealed partial class ActionChangelingChameleonSkinEvent : InstantActionEvent;

public sealed partial class ActionChangelingBiodegradeEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class PassiveChangelingSpawnLesserFormActionEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class PassiveChangelingAugmentedEyesightEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class PassiveChangelingDefibrillatorGraspEvent : SimpleDoAfterEvent;

public sealed partial class ActionChangelingDnaStingEvent : EntityTargetActionEvent;

public sealed partial class ActionChangelingTransformationStingEvent : EntityTargetActionEvent;

public sealed partial class ActionChangelingMuteStingEvent : EntityTargetActionEvent;

public sealed partial class ActionChangelingBlindStingEvent : EntityTargetActionEvent;

public sealed partial class ActionChangelingHallutinationStingEvent : EntityTargetActionEvent;

public sealed partial class ActionChangelingCryogenicStingEvent : EntityTargetActionEvent;

public sealed partial class ActionChangelingFalseArmbladeStingEvent : EntityTargetActionEvent;

public sealed partial class ActionChangelingArmBladeEvent : InstantActionEvent;

public sealed partial class ActionChangelingResonantShriekEvent : InstantActionEvent;

public sealed partial class ActionChangelingDissonantShriekEvent : InstantActionEvent;



[NetSerializable, Serializable]
public enum ChangelingVisuals : byte
{
    Idle,
    Slowed,
    Harvesting
}
