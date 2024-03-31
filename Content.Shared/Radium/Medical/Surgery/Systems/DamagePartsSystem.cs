using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Radium.Medical.Surgery.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Radium.Medical.Surgery.Systems;

public class DamagePartsSystem : EntitySystem
{
    //[Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    //[Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;

    public IReadOnlyDictionary<(BodyPartType, BodyPartSymmetry), (int, bool)> GetDamagedParts(EntityUid euid)
    {
        Dictionary<(BodyPartType PartType, BodyPartSymmetry Symmetry), (int, bool)> partsWounds = new();
        //if (!HasComp<HumanoidAppearanceComponent>(euid))
        //{
        //    return partsWounds;
        //}
        if (!TryComp<BodyComponent>(euid, out var bodyComponent))
            return partsWounds;
        foreach (var (_, component) in _bodySystem.GetBodyChildren(euid, bodyComponent))
        {
            //var organs = _bodySystem.GetPartOrgans(adjacentId).ToList();
            //var isDamaged = organs.Select(g => g.Component.Condition != OrganCondition.Healthy).ToList().Contains(true);
            //var damagedOrgans = organs.Where(g => g.Component.Condition != OrganCondition.Healthy);
            try
            {
                partsWounds.TryAdd((component.PartType, component.Symmetry),
                    (component.Wounds.Count, false)); //isDamaged
            }
            catch (Exception)
            {
                Logger.GetSawmill("Surgery").Error("Exception in adding parts!");
            }
        }

        return partsWounds;
    }
}
