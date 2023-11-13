using Content.Server.GameTicking.Rules.Components;
using Content.Server.Radium.Genestealer.EntitySystems;
using Content.Server.StationEvents.Events;

namespace Content.Server.Radium.Genestealer.StationEvents;

public sealed class GenestealerRule: StationEventSystem<GenestealerRuleComponent>
{
    [Dependency] private readonly GenestealerSystem _genestealerSystem = default!;
    protected override void Started(EntityUid uid, GenestealerRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if(!_genestealerSystem.MakeGenestealer(out _))
        {
            Sawmill.Warning("Map not have latejoin spawnpoints for creating genestealer spawner");
        }
    }
}
