using Content.Shared.Body.Part;
using Content.Shared.Radium.Medical.Surgery;
using Content.Shared.Radium.Medical.Surgery.Events;
using JetBrains.Annotations;

namespace Content.Client.Radium.Medical.Surgery.UI;

[UsedImplicitly]
public sealed class SurgeryBoundUserInterface: BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    [ViewVariables]
    private readonly SurgeryMenu? _surgeryMenu;
    public SurgeryBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _surgeryMenu = new SurgeryMenu(this, owner);
        _surgeryMenu.OnClose += Close;
    }
    protected override void Open()
    {
        base.Open();
        _surgeryMenu?.OpenCenteredLeft();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _surgeryMenu?.Close();
    }

    public void BeginSurgery(NetEntity uid, string prototypeId, BodyPartSymmetry bodyPartSymmetry)
    {
        var ev = new BeginSurgeryEvent
        {
            PrototypeId = prototypeId,
            Uid = uid,
            Symmetry = bodyPartSymmetry
        };
        _entityManager.EventBus.RaiseEvent(EventSource.Local, ev);
    }
}
