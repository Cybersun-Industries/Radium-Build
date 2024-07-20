using Content.Shared.Body.Part;
using Robust.Shared.Serialization;

namespace Content.Shared.Radium.Changeling.Events;

[Serializable, NetSerializable]
public sealed class ConfirmTransformation : BoundUserInterfaceMessage
{
    public NetEntity Uid;
    public int ServerIdentityIndex;
}

[Serializable, NetSerializable]
public sealed class ConfirmTransformSting : BoundUserInterfaceMessage
{
    public NetEntity Uid;
    public int ServerIdentityIndex;
}
