using Content.Client.Alerts;
using Content.Shared.Radium.Changeling.Components;

namespace Content.Client.Radium.Changeling;

public sealed class ClientChangelingSystem: EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
    }

    private void OnUpdateAlert(Entity<ChangelingComponent> ent, ref UpdateAlertSpriteEvent args)
    {
        if (args.Alert.ID != ent.Comp.ChemicalsAlert)
            return;

        var sprite = args.SpriteViewEnt.Comp;
        var chemicals = Math.Floor(Math.Clamp(ent.Comp.Chemicals, 0, 999));
        sprite.LayerSetState(0, $"{Math.Floor(chemicals / 6)}");
    }
}
