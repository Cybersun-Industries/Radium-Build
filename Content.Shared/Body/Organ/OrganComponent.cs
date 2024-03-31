using Content.Shared.Body.Systems;
using Content.Shared.Radium.Medical.Surgery.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Organ;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OrganComponent : Component
{
    /// <summary>
    /// Relevant body this organ is attached to.
    /// </summary>
    [DataField("body"), AutoNetworkedField]
    public EntityUid? Body;

    [DataField, AutoNetworkedField]
    public OrganCondition Condition = OrganCondition.Healthy;
}
