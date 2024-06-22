using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.Core.Tokens;

namespace Content.Shared.Radium.Medical.Surgery.Components;

[RegisterComponent]
public sealed partial class SurgeryStepComponent : Component
{
    [DataField("repeatable")]
    public bool Repeatable;

    [DataField("repeatIndex")]
    public int RepeatIndex;

    [DataField]
    private LocId Name { get; set; }

    [DataField("desc", required: true)]
    private LocId Description { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedDescription => Loc.GetString(Description);

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    [DataField]
    public string? Icon { get; set; }

    [DataField("action", required: true)]
    public Enum Key = default!;

    public int StepIndex;
}

[Serializable, NetSerializable]
public record struct SurgeryStepData
{
    public SurgeryStepData(SurgeryStepComponent? currentStep, string? operationName)
    {
        if (currentStep == null)
            return;
        LocalizedName = currentStep.LocalizedName;
        LocalizedDescription = currentStep.LocalizedDescription;
        Key = currentStep.Key;
        Icon = currentStep.Icon;
        if (operationName != null)
            OperationName = operationName;
    }

    public string OperationName = "";
    public string LocalizedName = "";
    public string LocalizedDescription = "";
    public string? Icon = null;
    public Enum Key = SurgeryTypeEnum.AddPart;
}
