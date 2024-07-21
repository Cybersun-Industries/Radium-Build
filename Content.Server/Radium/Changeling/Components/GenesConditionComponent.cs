using Content.Server.Radium.Changeling.EntitySystems;

namespace Content.Server.Radium.Changeling.Components;

[RegisterComponent, Access(typeof(ChangelingConditionsSystem))]
public sealed partial class GenesConditionComponent : Component
{
    [DataField("genesExtracted"), ViewVariables(VVAccess.ReadWrite)]
    public int GenesExtracted;
}
