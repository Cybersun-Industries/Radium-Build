using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Radium.Changeling;

[Serializable, NetSerializable]
public sealed partial class SoulEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class HarvestEvent : SimpleDoAfterEvent
{
}


public sealed class HarvestDoAfterComplete : EntityEventArgs
{
    public readonly EntityUid Target;

    public HarvestDoAfterComplete(EntityUid target)
    {
        Target = target;
    }
}

public sealed class HarvestDoAfterCancelled : EntityEventArgs
{
}

public sealed partial class ChangelingAbsorbDnaActionEvent : EntityTargetActionEvent
{
}

public sealed partial class ChangelingShopActionEvent : InstantActionEvent
{
}

public sealed partial class ChangelingStasisActionEvent : InstantActionEvent
{
}

public sealed partial class ChangelingTransformActionEvent : InstantActionEvent
{
}

public sealed partial class ChangelingSpeedUpActionEvent : InstantActionEvent
{
}

[NetSerializable, Serializable]
public enum ChangelingVisuals : byte
{
    Idle,
    Slowed,
    Harvesting
}
