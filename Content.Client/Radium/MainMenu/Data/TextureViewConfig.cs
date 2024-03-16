using System.Numerics;

namespace Content.Client.Radium.MainMenu.Data;

/// <summary>
/// The configuration for a textureView layer.
/// </summary>
[DataDefinition]
public sealed partial class TextureViewConfig
{
    /// <summary>
    /// The texture source for this layer.
    /// </summary>
    [DataField("texture", required: true)]
    public ITextureViewSource Texture { get; set; } = default!;

    /// <summary>
    /// Change rate per second.
    /// </summary>
    [DataField("speed")] public Vector2 Rate = Vector2.Zero;

    [DataField("scale")]
    public Vector2 Scale { get; set; } = Vector2.One;
}
