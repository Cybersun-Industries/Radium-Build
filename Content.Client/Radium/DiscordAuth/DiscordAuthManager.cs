using Content.Client.Corvax;
using Content.Shared.Backmen.DiscordAuth;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Shared.Network;

namespace Content.Client.Radium.DiscordAuth;

public sealed class DiscordAuthManager : IClientDiscordAuthManager
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    public string AuthUrl { get; private set; } = string.Empty;
    public Texture? Qrcode { get; }
    public bool IsSkipped { get; set; }

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgDiscordAuthCheck>();
        _netManager.RegisterNetMessage<MsgDiscordAuthRequired>(OnDiscordAuthRequired);
    }

    private void OnDiscordAuthRequired(MsgDiscordAuthRequired message)
    {
        if (_stateManager.CurrentState is DiscordAuthState)
            return;
        IsSkipped = false;
        AuthUrl = message.AuthUrl;
        _stateManager.RequestStateChange<DiscordAuthState>();
    }
}
