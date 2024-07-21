namespace Content.Server.Radium.Changeling.Components;

[RegisterComponent]
public sealed partial class ChangelingDnaComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsExtracted = false;
}
