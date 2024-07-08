using Content.Shared.Radium.Changeling.Components;
using Content.Shared.Radium.Changeling.Events;
using Content.Shared.Store;
using JetBrains.Annotations;

namespace Content.Client.Radium.Changeling.UI;

[UsedImplicitly]
public sealed class ChangelingStorageBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    //[Dependency] private readonly IEntityManager _entityManager = default!;

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
            ServerIdentityIndex = index
        };
        SendMessage(ev);
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
