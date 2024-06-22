using Content.Client.Radium.Medical.Surgery.UI;
using Content.Shared.Radium.Medical.Surgery;
using Content.Shared.Radium.Medical.Surgery.Events;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Radium.Medical.Surgery;

public sealed class SurgerySystem: EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeginSurgeryEvent>(OnSurgeryBegin);

    }

    private void OnSurgeryBegin(BeginSurgeryEvent ev)
    {
        //Don't ask me about that
        RaiseNetworkEvent(ev);
    }
}

