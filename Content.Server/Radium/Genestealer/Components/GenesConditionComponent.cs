using Content.Server.Radium.Genestealer.EntitySystems;

namespace Content.Server.Radium.Genestealer.Components;

[RegisterComponent, Access(typeof(GenestealerConditionsSystem))]
public sealed partial class GenesConditionComponent : Component
{
    [DataField("genesExtracted"), ViewVariables(VVAccess.ReadWrite)]
    public int GenesExtracted;
}
