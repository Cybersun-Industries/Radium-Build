using Content.Shared.Radium.Genestealer;
using Content.Shared.Radium.Genestealer.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Radium.Genestealer;

public sealed class Genestealer: EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenestealerComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, GenestealerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        /*
        if (_appearance.TryGetData<bool>(uid, GenestealerVisuals.Harvesting, out var harvesting, args.Component) && harvesting)
        {
            args.Sprite.LayerSetState(0, component.HarvestingState);
        }
        else if (_appearance.TryGetData<bool>(uid, GenestealerVisuals.Slowed, out var stunned, args.Component) && stunned)
        {
            args.Sprite.LayerSetState(0, component.StunnedState);
        }
        else if (_appearance.TryGetData<bool>(uid, GenestealerVisuals.Idle, out var idle, args.Component))
        {
            args.Sprite.LayerSetState(0, component.State);
        }
        */
    }
}
