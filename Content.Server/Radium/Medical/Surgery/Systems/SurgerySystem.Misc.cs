using System.Diagnostics.CodeAnalysis;
using Content.Server.Body.Systems;
using Content.Shared.Body.Organ;
using Content.Shared.Radium.Medical.Surgery.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Radium.Medical.Surgery.Systems;

public sealed partial class SurgerySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;
    public bool TryGetOperationPrototype(string id, [NotNullWhen(true)] out SurgeryOperationPrototype? prototype)
    {
        return _prototypeManager.TryIndex(id, out prototype);
    }

}
