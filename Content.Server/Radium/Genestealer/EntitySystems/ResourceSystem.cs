using Content.Server.Radium.Components;
using Content.Server.Revenant.Components;
using Content.Shared.Examine;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Random;

namespace Content.Server.Radium;

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

        switch (mob.CurrentState)
        {
            case MobState.Alive:
                if (TryComp<MindContainerComponent>(uid, out var mind) && mind.Mind != null)
                    component.ResourceAmount = _random.NextFloat(50f, 60f);
                else
                    component.ResourceAmount = _random.NextFloat(35f, 45f);
                break;
            case MobState.Critical:
                component.ResourceAmount = _random.NextFloat(30f, 40f);
                break;
            case MobState.Dead:
                component.ResourceAmount = _random.NextFloat(20f, 30f);
                break;
            case MobState.Invalid:
                break;
        }
    }
}
