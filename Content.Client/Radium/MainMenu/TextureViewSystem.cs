using System.Numerics;
using Content.Client.Parallax;
using Content.Client.Radium.MainMenu.Data;
using Content.Client.Radium.MainMenu.Managers;
using Content.Shared.Parallax;
using Content.Shared.Radium.MainMenu;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Radium.MainMenu;

public sealed class TextureViewSystem : SharedTextureViewSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly ITexureViewManager _viewManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new ParallaxOverlay());
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnReload);
    }

    private void OnReload(PrototypesReloadedEventArgs obj)
    {
        if (!obj.WasModified<TextureViewPrototype>())
            return;

        _viewManager.UnloadView("DebugView");
        _viewManager.LoadViewByName("DebugView");

        foreach (var comp in EntityQuery<ParallaxComponent>(true))
        {
            _viewManager.UnloadView(comp.Parallax);
            _viewManager.LoadViewByName(comp.Parallax);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<TextureViewOverlay>();
    }

}
