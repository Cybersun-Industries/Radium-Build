namespace Content.Server.Radium.Changeling.Components;

[RegisterComponent]
public sealed partial class ResourceComponent : Component
{
    /// <summary>
    /// Были ли ресурсы собраны.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Harvested = false;
}
