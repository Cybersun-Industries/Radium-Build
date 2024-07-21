using Content.Shared.Mind;
using Content.Shared.Roles;

namespace Content.Server.Radium.Changeling.Components;

[RegisterComponent, ExclusiveAntagonist]
public sealed partial class ChangelingRoleComponent : AntagonistRoleComponent
{
    public EntityUid? Target { get; set; }
    public EntityUid? Extractions { get; set; }
    public MindComponent? TargetMind { get; set; }
}
