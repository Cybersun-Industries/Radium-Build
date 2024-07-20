namespace Content.Server.Radium.Changeling;

[RegisterComponent]
public sealed partial class ChangelingSpawnerComponent: Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("target")]
    public EntityUid TargetForce { get; set; } = EntityUid.Invalid;
}
