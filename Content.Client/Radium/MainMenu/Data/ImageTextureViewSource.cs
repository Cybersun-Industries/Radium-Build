using System.Threading;
using System.Threading.Tasks;
using Content.Client.IoC;
using Content.Client.Parallax.Data;
using Content.Client.Resources;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.Radium.MainMenu.Data;

[UsedImplicitly]
[DataDefinition]
public sealed partial class ImageTextureViewSource:ITextureViewSource
{
    /// <summary>
    /// Texture path.
    /// </summary>
    [DataField("path", required: true)]
    public ResPath Path { get; private set; } = default!;

    Task<Texture> ITextureViewSource.GenerateTexture(CancellationToken cancel)
    {
        return Task.FromResult(StaticIoC.ResC.GetTexture(Path));
    }
}
