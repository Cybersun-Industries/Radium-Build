using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Radium.Genestealer;

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

public sealed partial class GenestealerShopActionEvent : InstantActionEvent
{
}

public sealed partial class GenestealerStasisActionEvent : InstantActionEvent
{
}

public sealed partial class GenestealerTransformActionEvent : InstantActionEvent
{
}

public sealed partial class GenestealerSpeedUpActionEvent : InstantActionEvent
{
}

[NetSerializable, Serializable]
public enum GenestealerVisuals : byte
{
    Idle,
    Slowed,
    Harvesting
}
