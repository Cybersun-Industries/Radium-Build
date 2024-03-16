using System.Threading;
using System.Threading.Tasks;
using Robust.Client.Graphics;

namespace Content.Client.Radium.MainMenu.Data;

[ImplicitDataDefinitionForInheritors]
public partial interface ITextureViewSource
{
    /// <summary>
    /// Generates or loads the texture.
    /// Note that this should be cached, but not necessarily *here*.
    /// </summary>
    Task<Texture> GenerateTexture(CancellationToken cancel = default);
}
