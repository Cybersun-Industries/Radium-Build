using System.Collections.Frozen;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Polymorph;
using Content.Shared.StatusIcon;
using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Radium.Changeling.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingComponent : Component
{
    #region Base Stats

    [DataField("soundMeatPool")]

    // Sandbox violation on CollectionExpression
    // ReSharper disable once UseCollectionExpression
    public List<SoundSpecifier?> SoundPool = new()
    {
        new SoundPathSpecifier("/Audio/Effects/gib1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/gib2.ogg"),
        new SoundPathSpecifier("/Audio/Effects/gib3.ogg"),
    };

    [DataField]
    public ProtoId<JobIconPrototype> StatusIcon= "HivemindFaction";

    [DataField("soundShriek")]
    public SoundSpecifier ShriekSound =
        new SoundPathSpecifier("/Audio/Radium/Changeling/Effects/changeling_shriek.ogg");

    [DataField]
    public float ShriekPower = 2.5f;

    public bool StrainedMusclesActive = false;

    public bool IsInLesserForm = false;

    [DataField, AutoNetworkedField]
    public float Chemicals = 10f;

    [DataField, AutoNetworkedField]
    public float MaxChemicals = 100f;

    [DataField]
    public string ChemicalsAlert = "Chemicals";

    public TimeSpan RegenTime = TimeSpan.Zero;

    public float RegenCooldown = 1f;

    [DataField]
    public float RegenChemicalsAmount = 0.5f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public FixedPoint2 Evolution = 0;

    [DataField] public bool IsInStasis;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string EvolutionCurrencyPrototype = "ChangelingEvolution";

    [DataField]
    public SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/Radium/Ambience/Antag/changeling_start.ogg");

    [DataField]
    public ProtoId<NpcFactionPrototype> NanotrasenFactionId = "NanoTrasen";

    #endregion

    [DataField, AutoNetworkedField]
    public Dictionary<int, string> ClientIdentitiesList = [];

    [DataField(serverOnly: true)]
    public Dictionary<int, (MetaDataComponent, HumanoidAppearanceComponent)> ServerIdentitiesList = [];

    [DataField] public EntityUid[] ActiveActions = [];

    [DataField] [ValidatePrototypeId<EntityPrototype>]
    public Dictionary<string, EntityUid> BaseActions = new()
    {
        { "ActionChangelingShop", EntityUid.Invalid },
        { "ActionChangelingAbsorbDNA", EntityUid.Invalid },
        { "ActionChangelingStasis", EntityUid.Invalid },
        { "ActionChangelingTransform", EntityUid.Invalid },
        { "ActionChangelingRegenerate", EntityUid.Invalid },
    };

    public Dictionary<ChangelingEquipment, (EntityUid, ProtoId<EntityPrototype>)> ChangelingEquipment = new()
    {
        { Components.ChangelingEquipment.Armblade, (EntityUid.Invalid, "ArmBladeChangeling") },
        { Components.ChangelingEquipment.Armor, (EntityUid.Invalid, "ChangelingClothingOuterArmor") },
        { Components.ChangelingEquipment.SpacesuitHelmet, (EntityUid.Invalid, "ChangelingClothingHeadHelmetHardsuit") },
        { Components.ChangelingEquipment.Spacesuit, (EntityUid.Invalid, "ChangelingClothingOuterHardsuit") },
        { Components.ChangelingEquipment.Shield, (EntityUid.Invalid, "ChangelingShield") },
        { Components.ChangelingEquipment.ArmorHelmet, (EntityUid.Invalid, "ChangelingClothingHeadHelmet") },
        { Components.ChangelingEquipment.FakeArmblade, (EntityUid.Invalid, "FakeArmBladeChangeling") },
    };

    [DataField] public FrozenSet<ProtoId<StoreCategoryPrototype>> StoreCategories =
        new HashSet<ProtoId<StoreCategoryPrototype>>
        {
            "ChangelingDefensive",
            "ChangelingOffensive",
        }.ToFrozenSet();

    [DataField]
    public MindComponent? Mind;

    [DataField]
    public ProtoId<PolymorphPrototype> ChangelingLesserFormPolymorphPrototype = "ChangelingMonkey";

    [DataField]
    public string ChangelingRole = "ChangelingRole";

    [DataField]
    public int TotalExtractedDna;

    [DataField]
    public int TotalAbsorbedEntities;

    [DataField]
    public EntityUid TransformationStingTarget;
}

public enum ChangelingEquipment
{
    Shield,
    Armblade,
    Armor,
    ArmorHelmet,
    Spacesuit,
    SpacesuitHelmet,
    FakeArmblade,
}
