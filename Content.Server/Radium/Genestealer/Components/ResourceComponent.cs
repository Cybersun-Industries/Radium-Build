namespace Content.Server.Radium.Components;

[RegisterComponent]
public sealed partial class ResourceComponent : Component
{
    /// <summary>
    /// Были ли ресурсы собраны с цели.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Harvested = false;

    /// <summary>
    /// Общее количество ресурсов цели.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ResourceAmount = 0f;
}
