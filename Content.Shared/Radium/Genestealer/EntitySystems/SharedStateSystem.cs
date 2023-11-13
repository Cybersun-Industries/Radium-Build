using Content.Shared.Movement.Systems;
using Content.Shared.Radium.Genestealer.Components;

namespace Content.Shared.Radium.Genestealer.EntitySystems;

/// <summary>
/// Makes the revenant solid when the component is applied.
/// Additionally applies a few visual effects.
/// Used for status effect.
/// </summary>
public abstract class SharedGenstealerStateSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StateComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StateComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StateComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
    }

    private void OnRefresh(EntityUid uid, StateComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.MovementSpeedDebuff, component.MovementSpeedDebuff);
    }

    public virtual void OnStartup(EntityUid uid, StateComponent component, ComponentStartup args)
    {
        _appearance.SetData(uid, GenestealerVisuals.Idle, true);
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    public virtual void OnShutdown(EntityUid uid, StateComponent component, ComponentShutdown args)
    {
        _appearance.SetData(uid, GenestealerVisuals.Idle, false);

        component.MovementSpeedDebuff = 1; //just so we can avoid annoying code elsewhere
        _movement.RefreshMovementSpeedModifiers(uid);
    }
}
