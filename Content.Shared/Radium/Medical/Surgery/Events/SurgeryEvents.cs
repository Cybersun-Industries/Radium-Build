using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Radium.Medical.Surgery.Events;

public interface ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }
}

[Serializable, NetSerializable]
public sealed class BeginSurgeryEvent : EntityEventArgs
{
    public string? PrototypeId;
    public NetEntity Uid;
    public Enum Symmetry = BodyPartSymmetry.None;
}

[Serializable, NetSerializable]
public sealed class SyncPartsEvent(NetEntity uid) : EntityEventArgs
{
    public NetEntity Uid = uid;
}

[Serializable, NetSerializable]
public sealed partial class SurgeryDoAfterEvent : SimpleDoAfterEvent
{
    public int FailureChange;

    public SurgeryDoAfterEvent(int chance)
    {
        FailureChange = chance;
    }
}

//BURN ALL OF THIS WITH FIRE!

#region LumaEvent

public sealed class LumaSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public Enum Symmetry { get; set; }

    public LumaSurgeryEvent(EntityUid uid, string prototypeId, Enum symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region OrganManipSurgeryEvent

public sealed class OrganManipSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public Enum Symmetry { get; set; }

    public OrganManipSurgeryEvent(EntityUid uid, string prototypeId, Enum symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region FeatureManipSurgeryEvent

public sealed class FeatureManipSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public Enum Symmetry { get; set; }

    public FeatureManipSurgeryEvent(EntityUid uid, string prototypeId, Enum symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region LobectomySurgeryEvent

public sealed class LobectomySurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public Enum Symmetry { get; set; }

    public LobectomySurgeryEvent(EntityUid uid, string prototypeId, Enum symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region CoronaryBypassSurgeryEvent

public sealed class CoronaryBypassSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public Enum Symmetry;

    public CoronaryBypassSurgeryEvent(EntityUid uid, string prototypeId, Enum symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region HepatectomySurgeryEvent

public sealed class HepatectomySurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public Enum Symmetry { get; set; }

    public HepatectomySurgeryEvent(EntityUid uid, string prototypeId, Enum symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region GastrectomySurgeryEvent

public sealed class GastrectomySurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public Enum Symmetry { get; set; }

    public GastrectomySurgeryEvent(EntityUid uid, string prototypeId, Enum symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region AmputationSurgeryEvent

public sealed class AmputationSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public Enum Symmetry { get; set; }

    public AmputationSurgeryEvent(EntityUid uid, string prototypeId, Enum symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region ImplantSurgeryEvent

public sealed class ImplantSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public Enum Symmetry { get; set; }

    public ImplantSurgeryEvent(EntityUid uid, string prototypeId, Enum symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region ImplantRemovalSurgeryEvent

public sealed class ImplantRemovalSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public Enum Symmetry { get; set; }

    public ImplantRemovalSurgeryEvent(EntityUid uid, string prototypeId, Enum symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region EyeSurgeryEvent

public sealed class EyeSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public Enum Symmetry { get; set; }

    public EyeSurgeryEvent(EntityUid uid, string prototypeId, Enum symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region RevivalSurgeryEvent

public sealed class RevivalSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public Enum Symmetry { get; set; }

    public RevivalSurgeryEvent(EntityUid uid, string prototypeId, Enum symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region FilterSurgeryEvent

public sealed class FilterSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public Enum Symmetry { get; set; }

    public FilterSurgeryEvent(EntityUid uid, string prototypeId, Enum symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region StomachPumpSurgeryEvent

public sealed class StomachPumpSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public Enum Symmetry { get; set; }

    public StomachPumpSurgeryEvent(EntityUid uid, string prototypeId, Enum symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region RepairBoneFSurgeryEvent

public sealed class RepairBoneFSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public BodyPartSymmetry Symmetry { get; set; }

    public RepairBoneFSurgeryEvent(EntityUid uid, string prototypeId, BodyPartSymmetry symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region RepairCompFSurgeryEvent

public sealed class RepairCompFSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public BodyPartSymmetry Symmetry { get; set; }

    public RepairCompFSurgeryEvent(EntityUid uid, string prototypeId, BodyPartSymmetry symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region BurnSurgeryEvent

public sealed class BurnSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public BodyPartSymmetry Symmetry { get; set; }

    public BurnSurgeryEvent(EntityUid uid, string prototypeId, BodyPartSymmetry symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region PierceSurgeryEvent

public sealed class PierceSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public BodyPartSymmetry Symmetry { get; set; }

    public PierceSurgeryEvent(EntityUid uid, string prototypeId, BodyPartSymmetry symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region EyeBlindSurgeryEvent

public sealed class EyeBlindSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }

    public BodyPartSymmetry Symmetry { get; set; }

    public EyeBlindSurgeryEvent(EntityUid uid, string prototypeId, BodyPartSymmetry symmetry)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
    }
}

#endregion

#region AddSurgeryEvent

public sealed class AddSurgeryEvent : EntityEventArgs, ISurgeryEvent
{
    public EntityUid Uid { get; set; }
    public string PrototypeId { get; set; }
    public EntityUid PartUid { get; set; }

    public BodyPartSymmetry Symmetry { get; set; }

    public AddSurgeryEvent(EntityUid uid, string prototypeId, BodyPartSymmetry symmetry, EntityUid partUid)
    {
        Uid = uid;
        PrototypeId = prototypeId;
        Symmetry = symmetry;
        PartUid = partUid;
    }
}

#endregion
