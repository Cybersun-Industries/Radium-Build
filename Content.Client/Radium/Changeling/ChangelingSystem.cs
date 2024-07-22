using Content.Client.Alerts;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Shared.Radium.Changeling.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Client.Radium.Changeling;

public sealed class ClientChangelingSystem: EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
        SubscribeLocalEvent<ChangelingComponent, GetStatusIconsEvent>(GetChangelingIcon);
    }

    private void GetChangelingIcon(Entity<ChangelingComponent> ent, ref GetStatusIconsEvent args)
    {
        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }

    private static void OnUpdateAlert(Entity<ChangelingComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.ChemicalsAlert)
            return;

        var sprite = args.SpriteViewEnt.Comp;
        var chemicals = Math.Floor(Math.Clamp(ent.Comp.Chemicals, 0, 999));
        sprite.LayerSetState(AlertVisualLayers.Base, $"{Math.Floor(chemicals / 6)}");
    }
}
