using Content.Shared.Radium.Changeling;
using Content.Shared.Radium.Changeling.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Radium.Changeling;

public sealed class Changeling: EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, ChangelingComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        /*
        if (_appearance.TryGetData<bool>(uid, ChangelingVisuals.Harvesting, out var harvesting, args.Component) && harvesting)
        {
            args.Sprite.LayerSetState(0, component.HarvestingState);
        }
        else if (_appearance.TryGetData<bool>(uid, ChangelingVisuals.Slowed, out var stunned, args.Component) && stunned)
        {
            args.Sprite.LayerSetState(0, component.StunnedState);
        }
        else if (_appearance.TryGetData<bool>(uid, ChangelingVisuals.Idle, out var idle, args.Component))
        {
            args.Sprite.LayerSetState(0, component.State);
        }
        */
    }
}
