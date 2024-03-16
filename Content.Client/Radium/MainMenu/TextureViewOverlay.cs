using Robust.Client.Graphics;

namespace Content.Client.Radium.MainMenu;

public sealed class TextureViewOverlay: Overlay
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    private readonly TextureViewSystem _view;
    public TextureViewOverlay()
    {
        IoCManager.InjectDependencies(this);
        _view = _entManager.System<TextureViewSystem>();
    }
    protected override void Draw(in OverlayDrawArgs args)
    {

    }
}
