using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Gravity;
using Content.Shared.IdentityManagement;
using Content.Shared.Radium.Changeling.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Radium.Changeling.EntitySystems;

public sealed partial class ChangelingSystem
{
    public void PlayMeatySound(EntityUid uid, ChangelingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var rand = _random.Next(0, component.SoundPool.Count - 1);
        var sound = component.SoundPool.ToArray()[rand];
        _audio.PlayPvs(sound, uid, AudioParams.Default.WithVolume(-3f));
    }

    public void DoScreech(EntityUid uid, ChangelingComponent comp)
    {
        _audio.PlayPvs(comp.ShriekSound, uid);

        var center = _transform.GetMapCoordinates(uid);
        var gamers = Filter.Empty();
        gamers.AddInRange(center, comp.ShriekPower, _playerManager, EntityManager);

        foreach (var gamer in gamers.Recipients)
        {
            if (gamer.AttachedEntity == null)
                continue;

            var pos = _transform.GetMapCoordinates(gamer.AttachedEntity.Value).Position;
            var delta = center.Position - pos;

            if (delta.EqualsApprox(Vector2.Zero))
                delta = new Vector2(.01f, 0);

            _recoil.KickCamera(uid, -delta.Normalized());
        }
    }

    public void Cycle(ChangelingComponent comp)
    {
        if (comp.Mind == null)
            return;

        var session = _mindSystem.GetSession(comp.Mind);

        if (session?.AttachedEntity == null)
            return;

        var uid = session.AttachedEntity.Value;

        UpdateChemicals(uid);

        if (!comp.StrainedMusclesActive)
            return;

        var stamina = EnsureComp<StaminaComponent>(uid);

        _stamina.TakeStaminaDamage(uid, 7.5f, visual: false);

        if (_stamina.GetStaminaDamage(uid) >= stamina.CritThreshold
            || !HasComp<GravityComponent>(uid))
            ToggleStrainedMuscles(uid);
    }


    public bool TrySting(EntityUid uid,
        EntityTargetActionEvent action,
        bool overrideMessage = false)
    {
        var target = action.Target;

        if (HasComp<ChangelingComponent>(target))
            return false;

        if (!overrideMessage)
        {
            _popup.PopupEntity(Loc.GetString("changeling-sting", ("target", Identity.Entity(target, EntityManager))),
                uid,
                uid);
        }

        return true;
    }

    public bool TryInjectReagents(EntityUid uid, List<(string, FixedPoint2)> reagents)
    {
        var solution = new Solution();
        foreach (var reagent in reagents)
        {
            solution.AddReagent(reagent.Item1, reagent.Item2);
        }

        return _solutionSystem.TryGetInjectableSolution(uid, out var targetSolution, out _) &&
               _solutionSystem.TryAddSolution(targetSolution.Value, solution);
    }

    public bool TryReagentSting(EntityUid uid,
        ChangelingComponent comp,
        EntityTargetActionEvent action,
        List<(string, FixedPoint2)> reagents)
    {
        var target = action.Target;
        return TrySting(uid, action) && TryInjectReagents(target, reagents);
    }

    public bool TryToggleItem(EntityUid uid,
        ChangelingEquipment outEquipment,
        string? clothingSlot = null,
        ChangelingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        var outItem =
            component.ChangelingEquipment[outEquipment]; // There is some wierd access error, so idk how to do better.

        if (outItem.Item1.IsValid())
        {
            EntityManager.DeleteEntity(outItem.Item1);
            var tempEquipment = component.ChangelingEquipment[outEquipment];
            component.ChangelingEquipment[outEquipment] = (EntityUid.Invalid, tempEquipment.Item2);

            return true;
        }

        var item = EntityManager.SpawnEntity(outItem.Item2, Transform(uid).Coordinates);
        if (clothingSlot != null && !_inventorySystem.TryEquip(uid, item, clothingSlot, force: true))
        {
            EntityManager.DeleteEntity(item);
            return false;
        }

        if (!_handsSystem.TryForcePickupAnyHand(uid, item))
        {
            _popup.PopupEntity(Loc.GetString("changeling-fail-hands"), uid, uid);
            EntityManager.DeleteEntity(item);
            return false;
        }

        component.ChangelingEquipment[outEquipment] = (item, outItem.Item2);
        return true;
    }
}
