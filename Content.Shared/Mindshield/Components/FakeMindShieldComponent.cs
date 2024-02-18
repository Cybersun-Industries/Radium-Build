using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.FakeMindshield.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FakeMindShieldComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<StatusIconPrototype> FakeMindShieldStatusIcon = "MindShieldIcon";

}
