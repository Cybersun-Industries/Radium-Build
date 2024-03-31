using Content.Client.Alerts;
using Content.Client.Gameplay;
using Content.Client.Ghost;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.Alert;
using Content.Shared.Body.Part;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Radium.Medical.Surgery.UI.Widgets.Systems;

public sealed class DamagePartsUiController : UIController, IOnStateEntered<GameplayState>,
    IOnSystemChanged<ClientDamagePartsSystem>
{
    [UISystemDependency] private readonly ClientDamagePartsSystem? _partsSystem = default;
    [UISystemDependency] private readonly GhostSystem? _ghost = default;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private DamagePartsUi? UI => UIManager.GetActiveUIWidgetOrNull<DamagePartsUi>();

    public void ClearAllControls(object? sender, EventArgs eventArgs)
    {
        UI?.Clear();
    }

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    private void OnScreenUnload()
    {
        //var widget = UI;
        //if (widget != null)
        //    widget.AlertPressed -= OnAlertPressed;
    }

    private void OnScreenLoad()
    {
        //var widget = UI;
        //if (widget != null)
        //    widget.AlertPressed += OnAlertPressed;

        SyncParts(_playerManager.LocalSession?.AttachedEntity);
    }
    private void SystemOnSyncParts(object? sender, IReadOnlyDictionary<(BodyPartType, BodyPartSymmetry), (int, bool)> e)
    {
        if (sender is not ClientDamagePartsSystem system)
            return;
        if (_ghost is { IsGhost: false })
        {
            UI?.SyncControls(system, e);
        }
    }

    public void OnStateEntered(GameplayState state)
    {
        // initially populate the frame if system is available
        SyncParts(_playerManager.LocalSession?.AttachedEntity);
    }

    public void SyncParts(EntityUid? entityUid)
    {
        var uid = entityUid ?? _playerManager.LocalEntity;
        var parts = _partsSystem?.PartsCondition(uid);
        if (parts != null)
        {
            SystemOnSyncParts(_partsSystem, parts);
        }
    }

    public void OnSystemLoaded(ClientDamagePartsSystem system)
    {
        system.SyncParts += SystemOnSyncParts;
        system.Dispose += ClearAllControls;
    }

    public void OnSystemUnloaded(ClientDamagePartsSystem system)
    {
        system.SyncParts -= SystemOnSyncParts;
        system.Dispose -= ClearAllControls;
    }
}
