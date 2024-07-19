using System.Collections.Frozen;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
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
    public List<SoundSpecifier?> SoundPool = new()
    {
        new SoundPathSpecifier("/Audio/Effects/gib1.ogg"),
        new SoundPathSpecifier("/Audio/Effects/gib2.ogg"),
        new SoundPathSpecifier("/Audio/Effects/gib3.ogg"),
    };

    [DataField("soundShriek")]
    public SoundSpecifier ShriekSound =
        new SoundPathSpecifier("/Audio/Goobstation/Changeling/Effects/changeling_shriek.ogg");

    [DataField("shriekPower")]
    public float ShriekPower = 2.5f;

    public bool StrainedMusclesActive = false;

    public bool IsInLesserForm = false;

    [DataField, AutoNetworkedField]
    public float Chemicals = 100f;

    [DataField, AutoNetworkedField]
    public float MaxChemicals = 100f;

    public TimeSpan RegenTime = TimeSpan.Zero;

    public float RegenCooldown = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public FixedPoint2 Evolution = 0;

    [DataField] public bool IsInStasis;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string EvolutionCurrencyPrototype = "ChangelingEvolution";


    [ViewVariables]
    public float Accumulator = 0;

    #endregion

    [DataField, AutoNetworkedField]
    public Dictionary<int, string> ClientIdentitiesList = [];

    [DataField(serverOnly: true)]
    public Dictionary<int, (MetaDataComponent, HumanoidAppearanceComponent)> ServerIdentitiesList = [];

    [DataField] public HumanoidAppearanceComponent? SourceHumanoid;

    [DataField] public MetaDataComponent? Metadata;

    [DataField] public EntityUid[] ActiveActions = [];

    [DataField] [ValidatePrototypeId<EntityPrototype>]
    public Dictionary<string, EntityUid> BaseActions = new()
    {
        { "ActionChangelingShop", EntityUid.Invalid },
        { "ActionChangelingAbsorbDNA", EntityUid.Invalid },
        { "ActionChangelingStasis", EntityUid.Invalid },
        { "ActionChangelingTransform", EntityUid.Invalid },
    };

    public Dictionary<ChangelingEquipment, (EntityUid, ProtoId<EntityPrototype>)> ChangelingEquipment = new()
    {
        { Components.ChangelingEquipment.Armblade, (EntityUid.Invalid, "ChangelingArmBlade") },
        { Components.ChangelingEquipment.Armor, (EntityUid.Invalid, "ChangelingClothingOuterArmor") },
        { Components.ChangelingEquipment.SpacesuitHelmet, (EntityUid.Invalid, "ChangelingClothingHeadHelmetHardsuit") },
        { Components.ChangelingEquipment.Spacesuit, (EntityUid.Invalid, "ChangelingClothingOuterHardsuit") },
        { Components.ChangelingEquipment.Shield, (EntityUid.Invalid, "ChangelingShield") },
        { Components.ChangelingEquipment.ArmorHelmet, (EntityUid.Invalid, "ChangelingClothingHeadHelmet") },
        { Components.ChangelingEquipment.FakeArmbladePrototype, (EntityUid.Invalid, "FakeArmbladePrototype") },
    };

    [DataField] public FrozenSet<ProtoId<StoreCategoryPrototype>> StoreCategories =
        new HashSet<ProtoId<StoreCategoryPrototype>>
        {
            "ChangelingDefensive",
            "ChangelingOffensive",
        }.ToFrozenSet();

    [DataField] public MindComponent? Mind;
}

public enum ChangelingEquipment
{
    Shield,
    Armblade,
    Armor,
    ArmorHelmet,
    Spacesuit,
    SpacesuitHelmet,
    FakeArmbladePrototype,
}
