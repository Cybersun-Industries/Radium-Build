using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Radium.Medical.Surgery.Systems;

namespace Content.Server.Radium.Medical.Surgery.Systems;

public sealed class ServerDamagePartsSystem : DamagePartsSystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;
    public override IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildren(EntityUid euid, BodyComponent bodyComponent)
    {
        return _bodySystem.GetBodyChildren(euid, bodyComponent);
    }
}
