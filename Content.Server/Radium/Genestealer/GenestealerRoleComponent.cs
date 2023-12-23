using Content.Shared.Mind;
using Content.Shared.Roles;

namespace Content.Server.Radium.Genestealer;

[RegisterComponent]
public sealed partial class GenestealerRoleComponent : AntagonistRoleComponent
{
    public EntityUid? Target { get; set; }
    public EntityUid? Extractions { get; set; }
    public MindComponent? TargetMind { get; set; }
}
