namespace Content.Server.Radium.Genestealer;

[RegisterComponent]
public sealed partial class GenestealerSpawnerComponent: Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("target")]
    public EntityUid TargetForce { get; set; } = EntityUid.Invalid;
}
