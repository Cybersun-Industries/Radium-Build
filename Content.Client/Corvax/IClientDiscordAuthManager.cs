using Content.Shared.Corvax;
using Robust.Client.Graphics;

namespace Content.Client.Corvax;

public interface IClientDiscordAuthManager : ISharedDiscordAuthManager
{
    public string AuthUrl { get; }
    public Texture? Qrcode { get; }
}
