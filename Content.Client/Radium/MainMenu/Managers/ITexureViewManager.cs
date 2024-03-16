using System.Numerics;
using System.Threading.Tasks;
using Content.Client.Parallax;

namespace Content.Client.Radium.MainMenu.Managers;

public interface ITexureViewManager
{
    /// <summary>
    /// All WorldHomePosition values are offset by this.
    /// </summary>
    Vector2 ParallaxAnchor { get; set; }

    bool IsLoaded(string name);

    /// <summary>
    /// The layers of the selected rsi.
    /// </summary>
    TextureViewPrepared[] GetLayers(string name);

    Task LoadViewByName(string name);

    void UnloadView(string name);
}
