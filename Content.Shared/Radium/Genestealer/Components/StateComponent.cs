namespace Content.Shared.Radium.Genestealer.Components;

/// <summary>
/// Makes the target solid, visible, and applies a slowdown.
/// Meant to be used in conjunction with statusEffectSystem
/// </summary>
[RegisterComponent]
public sealed partial class StateComponent : Component
{
    /// <summary>
    /// The debuff applied when the component is present.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float MovementSpeedDebuff = 0.66f;
}
