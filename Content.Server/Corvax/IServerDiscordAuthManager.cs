using System.Threading;
using System.Threading.Tasks;
using Content.Corvax.Interfaces.Shared;
using Content.Server.Radium.DiscordAuth;
using Content.Shared.Corvax;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Corvax;

public interface IServerDiscordAuthManager : ISharedDiscordAuthManager
{
    public event EventHandler<ICommonSession>? PlayerVerified;
    public Task<DiscordAuthManager.DiscordGenerateLinkResponse> GenerateAuthLink(NetUserId userId, CancellationToken cancel);
    public Task<bool> IsVerified(NetUserId userId, CancellationToken cancel);

    public bool IsSkipped { get; set; }
}
