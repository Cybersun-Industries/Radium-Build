using Content.Shared.Body.Part;
using Content.Shared.Radium.Medical.Surgery.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Radium.Medical.Surgery.Prototypes;

[Prototype("surgeryOperation")]
public sealed partial class SurgeryOperationPrototype : IPrototype
{
    [IdDataField]
    [ViewVariables]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    [DataField("desc", required: true)]
    public LocId Description { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedDescription => Loc.GetString(Description);

    [DataField("bodyPart")]
    public string BodyPart { get; set; } = "Head";

    [DataField]
    public SpriteSpecifier? Icon { get; private set; }

    [DataField("steps")]
    public List<SurgeryStepComponent>? Steps;

    [DataField("key")]
    public string EventKey = string.Empty;

    [DataField("unique")]
    public bool Unique;

    [DataField("hidden")]
    public bool IsHidden;
}
