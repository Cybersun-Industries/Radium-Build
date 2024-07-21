using Content.Server.Radium.Changeling.EntitySystems;
using Content.Server.Radium.Changeling.StationEvents;
using Content.Shared.Mind;

namespace Content.Server.Radium.Changeling.Components;

[RegisterComponent][Access(typeof(ChangelingRule), typeof(ChangelingSystem))]
public sealed partial class ChangelingRuleComponent : Component
{
    public List<(EntityUid mindId, MindComponent mind)> Changelings = [];
}
