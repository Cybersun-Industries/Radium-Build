using Content.Shared.Body.Part;
using Content.Shared.Radium.Medical.Surgery.Components;

namespace Content.Server.Radium.Medical.Surgery.Components;

[RegisterComponent]
public sealed partial class SurgeryInProgressComponent : Component
{
    [DataField("currentStep")]
    public SurgeryStepComponent? CurrentStep { get; set; }

    [DataField("surgeryPrototypeId", readOnly: true)]
    public string? SurgeryPrototypeId { get; set; }

    public Enum Symmetry { get; set; } = BodyPartSymmetry.None;
}
