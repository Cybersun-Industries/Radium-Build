using System.Numerics;
using Content.Client.Radium.MainMenu.Managers;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Radium.MainMenu;

/// <summary>
///     Renders the textureView background as a UI control.
/// </summary>
public sealed class TextureViewControl : Control
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ITexureViewManager _viewManager = default!;

    public TextureViewControl()
    {
        IoCManager.InjectDependencies(this);

        RectClipContent = true;
        _viewManager.LoadViewByName("DebugView");
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        foreach (var layer in _viewManager.GetLayers("DebugView"))
        {
            var tex = layer.Texture;
            var texSize = (tex.Size.X * (int) Size.X, tex.Size.Y * (int) Size.X) * layer.Config.Scale.Floored() / 1920;
            var ourSize = PixelSize;

            var currentTime = (float) _timing.RealTime.TotalSeconds;
            //var offset = new Vector2(currentTime * 100f, currentTime * 0f);
            var origin = (ourSize - texSize) / 2;

            handle.DrawTextureRect(tex, UIBox2.FromDimensions(origin, texSize));
        }
    }
}
