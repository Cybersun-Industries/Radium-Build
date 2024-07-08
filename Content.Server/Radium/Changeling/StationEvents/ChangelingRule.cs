using Content.Server.GameTicking.Rules.Components;
using Content.Server.Radium.Changeling.EntitySystems;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;

namespace Content.Server.Radium.Changeling.StationEvents;

public sealed class ChangelingRule: StationEventSystem<ChangelingRuleComponent>
{
    [Dependency] private readonly ChangelingSystem _changelingSystem = default!;
    protected override void Started(EntityUid uid, ChangelingRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if(!_changelingSystem.MakeChangeling(out _))
        {
            Sawmill.Warning("Map not have latejoin spawnpoints for creating changeling spawner");
        }
    }
}
