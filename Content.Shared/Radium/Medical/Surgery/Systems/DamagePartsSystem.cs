using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;

namespace Content.Shared.Radium.Medical.Surgery.Systems;

public abstract class DamagePartsSystem : EntitySystem
{
    //[Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    //[Dependency] private readonly IGameTiming _timing = default!;

    public IReadOnlyDictionary<(BodyPartType, BodyPartSymmetry), (int, bool)> GetDamagedParts(EntityUid euid)
    {
        Dictionary<(BodyPartType PartType, BodyPartSymmetry Symmetry), (int, bool)> partsWounds = new();
        //if (!HasComp<HumanoidAppearanceComponent>(euid))
        //{
        //    return partsWounds;
        //}
        if (!TryComp<BodyComponent>(euid, out var bodyComponent))
            return partsWounds;
        foreach (var (_, component) in GetBodyChildren(euid, bodyComponent))
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


    public abstract IEnumerable<(EntityUid Id, BodyPartComponent Component)> GetBodyChildren(EntityUid euid,
        BodyComponent bodyComponent);
}
