using Robust.Shared.Serialization;

namespace Content.Shared.Radium.Medical.Surgery.Components;

[NetSerializable, Serializable]
public enum SurgeryUiKey : byte
{
    Key
}

[NetSerializable, Serializable]
public enum SurgeryTypeEnum : byte
{
    Incise,
    Retract,
    Burn,
    Cut,
    Clamp,
    Revive, //TODO
    Filter, //TODO
    AddPart,
    AddAdditionalPart,
    Repair, //Bonesetter/bone gel/surgical tape
    Bandage,

}

[NetSerializable, Serializable]
public enum SurgeryPartEnum : byte
{
    None,
    Head,
    Arm,
    Torso,
    Leg
}

[NetSerializable, Serializable]
public enum WoundTypeEnum : byte
{
    Blunt,
    Piercing,
    Heat
}

[NetSerializable, Serializable]
public enum WoundNegativeEffects: byte
{
    ActionModifier,
    ActiveBleeding,
    BleedingWeakness,
    WoundRateModifier
}

[NetSerializable, Serializable]
public enum WoundSeverity: byte
{
    Moderate,
    Heavy,
    Critical
}

[NetSerializable, Serializable]
public enum OrganCondition: byte
{
    Healthy,
    Unhealthy,
    Damaged,
    Critical,
    Dead
}

[NetSerializable, Serializable, DataRecord]
public partial struct PartWound
{
    public PartWound(WoundTypeEnum type) : this()
    {
        Type = type;
    }

    public WoundTypeEnum Type = WoundTypeEnum.Blunt;
    public List<Enum> NegativeEffects = new();
}
