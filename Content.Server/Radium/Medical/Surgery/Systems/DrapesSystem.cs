using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Server.Radium.Medical.Surgery.Components;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Radium.Medical.Surgery;
using Content.Shared.Radium.Medical.Surgery.Components;
using Content.Shared.Radium.Medical.Surgery.Events;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Radium.Medical.Surgery.Systems;

public sealed class DrapesSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DrapesComponent, AfterInteractEvent>(OnDrapesAfterInteract);
    }


    private void OnDrapesAfterInteract(Entity<DrapesComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null || HasComp<SurgeryInProgressComponent>(entity))
            return;

        if (ApplyDrapes(entity, args, entity.Comp))
            args.Handled = true;
    }

    private bool ApplyDrapes(EntityUid uid, InteractEvent args, DrapesComponent component)
    {
        if (args.Target == null)
        {
            return false;
        }

        var target = args.Target.Value;
        var user = args.User;

        if (!TryComp<DrapesComponent>(uid, out var drapes))
        {
            return false;
        }

        if (!TryComp<DamageableComponent>(target, out _))
            return false;

        if (args.User != target && !_interactionSystem.InRangeUnobstructed(user, target, popup: true))
            return false;

        if (!_inventory.HasSlot(target, "jumpsuit"))
        {
            return false;
        }


        _audio.PlayPvs(component.BeginSound,
            uid,
            AudioHelpers.WithVariation(0.125f, _random).WithVolume(1f));

        if (!_players.TryGetSessionByEntity(args.User, out _))
        {
            _popupSystem.PopupEntity("Мозговая активность остутствует. Нет смысла что-либо делать.",
                args.User,
                args.User);
            return false;
        }

        if (args.Target == null)
            return false;

        OpenUserInterface(args.User, args.Target.Value);

        _adminLogger.Add(LogType.Healed,
            $"{EntityManager.ToPrettyString(args.User):user} used drapes on {target}");

        _audio.PlayPvs(drapes.EndSound,
            uid,
            AudioHelpers.WithVariation(0.125f, _random).WithVolume(1f));

        args.Handled = true;
        return true;
    }

    private void OpenUserInterface(EntityUid user, EntityUid target)
    {
        if (!_uiSystem.HasUi(target, SurgeryUiKey.Key))
            return;

        _uiSystem.OpenUi(target, SurgeryUiKey.Key, user);

        //if (!TryComp<ActorComponent>(user, out var actor) ||
        //    !_uiSystem.TryGetOpenUi(target, SurgeryUiKey.Key, out var ui))
        //    return;

        //_uiSystem.OpenUi(ui.Owner, ui.UiKey, actor.PlayerSession);
    }
}
