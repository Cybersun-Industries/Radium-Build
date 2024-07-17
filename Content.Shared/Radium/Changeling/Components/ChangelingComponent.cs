using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
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

    public Dictionary<ChangelingEquipment, EntityUid?> ChangelingEquipment= new ();
    public EntityUid? ArmbladeEntity;
    public EntityUid? ShieldEntity;
    public EntityUid? ArmorEntity, ArmorHelmetEntity;
    public EntityUid? SpacesuitEntity, SpacesuitHelmetEntity;

    public bool StrainedMusclesActive = false;

    public bool IsInLesserForm = false;

    /// <summary>
    ///     Current amount of chemicals changeling currently has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Chemicals = 100f;

    /// <summary>
    ///     Maximum amount of chemicals changeling can have.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxChemicals = 100f;

    /// <summary>
    ///     Cooldown between chem regen events.
    /// </summary>
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

    [DataField] public ActionsComponent? Actions;

    [DataField] public EntityUid[] ActiveActions = [];

    [DataField] public EntityUid? AbsorbDnaAction;

    [DataField] public EntityUid? StasisAction;

    [DataField] public EntityUid? TransformAction;

    [DataField] public EntityUid? ShopAction;

    [DataField] public MindComponent? Mind;
}

public enum ChangelingEquipment
{
    Shield,
    Armblade,
    Armor,
    ArmorHelmet,
    Spacesuit,
    SpacesuitHelmet
}
