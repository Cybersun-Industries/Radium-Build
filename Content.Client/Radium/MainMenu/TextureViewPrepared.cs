using Content.Client.Radium.MainMenu.Data;
using Robust.Client.Graphics;

namespace Content.Client.Radium.MainMenu;

/// <summary>
/// A 'prepared' (i.e. texture loaded and ready to use) TextureView layer.
/// </summary>
public struct TextureViewPrepared
{
    /// <summary>
    /// The loaded texture for this layer.
    /// </summary>
    public Texture Texture { get; set; }

    /// <summary>
    /// The configuration for this layer.
    /// </summary>
    public TextureViewConfig Config { get; set; }
}
