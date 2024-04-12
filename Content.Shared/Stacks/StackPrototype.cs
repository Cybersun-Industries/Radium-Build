using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Stacks;

[Prototype("stack")]
public sealed partial class StackPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Human-readable name for this stack type e.g. "Steel"
    /// </summary>
    /// <remarks>This is a localization string ID.</remarks>
    [DataField("name")]
    public string PrName { get; private set; } = string.Empty;
    public string Name
    {
        get
        {
            //BURN ALL OF THIS WITH FIRE
            var name = Loc.TryGetString("materials-" + PrName.ToLower().Replace(' ', '-').Replace("-plank", ""), out var m1)? m1 :
                Loc.TryGetString("ent-" + PrName.ToLower().Replace(" ", ""), out var m2)? m2 : "";
            #if DEBUG
            if (name == "")
            {
                Logger.GetSawmill("Localization-g-menu").Error($"Can't find locale for materials-{PrName.ToLower().Replace(" ", "")}");
            }
            #endif

            return name;
        }
    }

    /// <summary>
    ///     An icon that will be used to represent this stack type.
    /// </summary>
    [DataField("icon")]
    public SpriteSpecifier? Icon { get; private set; }

    /// <summary>
    ///     The entity id that will be spawned by default from this stack.
    /// </summary>
    [DataField("spawn", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Spawn { get; private set; } = string.Empty;

    /// <summary>
    ///     The maximum amount of things that can be in a stack.
    ///     Can be overriden on <see cref="StackComponent"/>
    ///     if null, simply has unlimited max count.
    /// </summary>
    [DataField("maxCount")]
    public int? MaxCount { get; private set; }

    /// <summary>
    /// The size of an individual unit of this stack.
    /// </summary>
    [DataField("itemSize")]
    public int? ItemSize;
}

