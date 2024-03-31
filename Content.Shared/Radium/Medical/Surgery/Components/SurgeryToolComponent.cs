namespace Content.Shared.Radium.Medical.Surgery.Components;

[RegisterComponent]
public sealed partial class SurgeryToolComponent: Component
{
    [DataField("action", required: true)]
    public Enum Key = default!;

    [DataField("modifier")]
    public float Modifier = 1f;
}
