using Content.Client.Alerts;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Shared.Radium.Changeling.Components;

namespace Content.Client.Radium.Changeling;

public sealed class ClientChangelingSystem: EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
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
