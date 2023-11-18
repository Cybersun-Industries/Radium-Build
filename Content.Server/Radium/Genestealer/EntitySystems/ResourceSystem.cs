using Content.Server.Radium.Genestealer.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Random;

namespace Content.Server.Radium.Genestealer.EntitySystems;

public sealed class ResourceSystem: EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResourceComponent, ComponentStartup>(OnResourceEventReceived);
        SubscribeLocalEvent<ResourceComponent, MobStateChangedEvent>(OnMobstateChanged);
        SubscribeLocalEvent<ResourceComponent, MindAddedMessage>(OnResourceEventReceived);
        SubscribeLocalEvent<ResourceComponent, MindRemovedMessage>(OnResourceEventReceived);
    }

    private void OnMobstateChanged(EntityUid uid, ResourceComponent component, MobStateChangedEvent args)
    {
        UpdateResourceAmount(uid, component);
    }
    private void OnResourceEventReceived(EntityUid uid, ResourceComponent component, EntityEventArgs args)
    {
        UpdateResourceAmount(uid, component);
    }

    private void UpdateResourceAmount(EntityUid uid, ResourceComponent component)
    {
        if (!TryComp<MobStateComponent>(uid, out var mob))
            return;

        component.ResourceAmount = mob.CurrentState switch
        {
            MobState.Alive => _random.NextFloat(15f, 20f),
            MobState.Critical => _random.NextFloat(10f, 15f),
            _ => component.ResourceAmount
        };
    }
}
