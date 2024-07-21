using Content.Shared.Radium.Changeling.Components;
using Content.Shared.Radium.Changeling.Events;
using JetBrains.Annotations;

namespace Content.Client.Radium.Changeling.UI;

[UsedImplicitly]
public sealed class ChangelingDnaStorageBoundUserInterfaceTransform(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ChangelingStorage? _storageMenu;

    protected override void Open()
    {
        base.Open();
        _storageMenu = new ChangelingStorage(this, Owner);
        _storageMenu?.UpdateIdentities(Owner);
        _storageMenu?.OpenCentered();
        if (_storageMenu != null)
            _storageMenu.OnClose += Close;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _storageMenu?.Close();
        _storageMenu?.Dispose();
    }

    public void ConfirmTransformation(NetEntity uid, int index)
    {
        var ev = new ConfirmTransformation
        {
            Uid = uid,
            ServerIdentityIndex = index,
        };
        SendMessage(ev);
        Dispose();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case ChangelingStorageUiState msg:
                _storageMenu?.UpdateIdentities(Owner);
                break;
        }
    }
}

public sealed class ChangelingDnaStorageBoundUserInterfaceSting(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private ChangelingStorage? _storageMenu;

    protected override void Open()
    {
        base.Open();
        _storageMenu = new ChangelingStorage(this, Owner);
        _storageMenu?.UpdateIdentities(Owner);
        _storageMenu?.OpenCentered();
        if (_storageMenu != null)
            _storageMenu.OnClose += Close;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _storageMenu?.Close();
        _storageMenu?.Dispose();
    }

    public void ConfirmSting(NetEntity uid, int index)
    {
        var ev = new ConfirmTransformSting
        {
            Uid = uid,
            ServerIdentityIndex = index,
        };

        SendMessage(ev);
        Dispose();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case ChangelingStorageUiState:
                _storageMenu?.UpdateIdentities(Owner);
                break;
        }
    }
}
