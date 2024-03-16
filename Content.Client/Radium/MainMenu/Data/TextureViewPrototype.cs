using Robust.Shared.Prototypes;

namespace Content.Client.Radium.MainMenu.Data;

/// <summary>
/// Prototype data for a textureView.
/// </summary>
[Prototype("textureView")]
public sealed class TextureViewPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Layers.
    /// </summary>
    [DataField("layers")]
    public List<TextureViewConfig> Layers { get; private set; } = new();
}
