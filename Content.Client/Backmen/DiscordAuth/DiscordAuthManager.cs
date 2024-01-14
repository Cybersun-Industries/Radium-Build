using System.IO;
using System.Threading;
using Content.Shared.Backmen.DiscordAuth;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Backmen.DiscordAuth;

public sealed class DiscordAuthManager : Content.Corvax.Interfaces.Client.IClientDiscordAuthManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    public string AuthUrl { get; private set; } = string.Empty;
    public Texture? Qrcode { get; }

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgDiscordAuthCheck>();
        _netManager.RegisterNetMessage<MsgDiscordAuthRequired>(OnDiscordAuthRequired);
    }

    private void OnDiscordAuthRequired(MsgDiscordAuthRequired message)
    {
        if (_stateManager.CurrentState is DiscordAuthState)
            return;

        AuthUrl = message.AuthUrl;
        _stateManager.RequestStateChange<DiscordAuthState>();
    }
}
