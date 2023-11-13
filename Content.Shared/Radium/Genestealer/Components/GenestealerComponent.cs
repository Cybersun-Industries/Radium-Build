using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Radium.Genestealer.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GenestealerComponent : Component
{
    #region Base Stats
    /// <summary>
    /// The total amount of Essence the genestealer has.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Resource = 10;

    [DataField("stolenResourceCurrencyPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string StolenResourceCurrencyPrototype = "StolenResource";

    /// <summary>
    /// The entity's current max amount of essence. Can be increased
    /// through harvesting player souls.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxResource")]
    public FixedPoint2 ResourceRegenCap = 30;

    /// <summary>
    /// The amount of essence passively generated per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("resoucePerSecond")]
    public FixedPoint2 ResourcePerSecond = 0.1f;

    [ViewVariables]
    public float Accumulator = 0;

    #endregion

    #region Absorb DNA Ability
    // Here's the gist of the harvest ability:
    // Step 1: The revenant clicks on an entity to "search" for it's soul, which creates a doafter.
    // Step 2: After the doafter is completed, the soul is "found" and can be harvested.
    // Step 3: Clicking the entity again begins to harvest the soul, which causes the revenant to become vulnerable
    // Step 4: The second doafter for the harvest completes, killing the target and granting the revenant essence.

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("harvestDebuffs")]
    public Vector2 HarvestDebuffs = new(5, 0);

    /// <summary>
    /// The amount that is given to the genestealer each time it's max essence is upgraded.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxEssenceUpgradeAmount")]
    public float MaxEssenceUpgradeAmount = 5;
    #endregion

    #region Regenerative Statis Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("stasisCost")]
    public FixedPoint2 StasisCost = -15;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("stasisDebuffs")]
    public Vector2 StasisDebuffs = new(1, 0);

    [DataField("stasisDuration")]
    public FixedPoint2 StatisDuration = 120; //seconds
    #endregion

    #region Transform Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("transformCost")]
    public FixedPoint2 TransformCost = -5;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("transformDebuffs")]
    public Vector2 TransformDebuffs = new(1, 0);

    #endregion

    #region Visualizer
    [DataField("state")]
    public string State = "idle";
    [DataField("stunnedState")]
    public string StunnedState = "stunned";
    [DataField("harvestingState")]
    public string HarvestingState = "harvesting";
    #endregion

    [DataField] public EntityUid? Action;

    [DataField] public bool IsInStasis;
}
