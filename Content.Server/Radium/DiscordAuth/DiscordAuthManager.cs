using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Content.Corvax.Interfaces.Server;
using Content.Server.Corvax;
using Content.Shared.Backmen.CCVar;
using Content.Shared.Backmen.DiscordAuth;
using Content.Shared.Radium;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using YamlDotNet.Serialization.NamingConventions;

namespace Content.Server.Radium.DiscordAuth;

public sealed class DiscordAuthManager : IServerDiscordAuthManager
{
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IPlayerManager _playerMgr = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDiscordAuthManager _discordAuthManager = default!;

    private ISawmill _sawmill = default!;
    private readonly HttpClient _httpClient = new();
    private bool _isEnabled = false;
    private string _apiUrl = string.Empty;
    private string _apiKey = string.Empty;
    private string _proxyApiUrl = string.Empty;

    /// <summary>
    ///     Raised when player passed verification or if feature disabled
    /// </summary>
    public event EventHandler<ICommonSession>? PlayerVerified;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("discord_auth");

        _cfg.OnValueChanged(CCVars.DiscordAuthEnabled, v => _isEnabled = v, true);
        _cfg.OnValueChanged(CCVars.DiscordAuthApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(CCVars.DiscordAuthApiKey, v => _apiKey = v, true);
        _cfg.OnValueChanged(CCVars.DiscordProxyApiUrl, v => _proxyApiUrl = v, true);

        _netMgr.RegisterNetMessage<MsgDiscordAuthRequired>();
        _netMgr.RegisterNetMessage<MsgDiscordAuthSkip>(OnAuthSkipped);
        _netMgr.RegisterNetMessage<MsgDiscordAuthCheck>(OnAuthCheck);

        _playerMgr.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public bool IsSkipped { get; set; } = false;

    private async void OnAuthCheck(MsgDiscordAuthCheck message)
    {
        var isProxy = await IsProxy(message.MsgChannel);
        if (isProxy)
        {
            _netMgr.DisconnectChannel(message.MsgChannel, "Proxy detected. Interrupting connection.");
        }
        var isVerified = await IsVerified(message.MsgChannel.UserId);

        if (!isVerified)
            return;

        if (isProxy)
        {
            return;
        }

        var session = _playerMgr.GetSessionById(message.MsgChannel.UserId);

        PlayerVerified?.Invoke(this, session);
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Connected)
            return;

        if (!_isEnabled)
        {
            PlayerVerified?.Invoke(this, e.Session);
            return;
        }

        if (e.NewStatus != SessionStatus.Connected)
            return;

        var isVerified = await IsVerified(e.Session.UserId);
        if (isVerified)
        {
            PlayerVerified?.Invoke(this, e.Session);
            return;
        }

        var authUrl = await GenerateAuthLink(e.Session.UserId);
        var msg = new MsgDiscordAuthRequired { AuthUrl = authUrl.Url };
        e.Session.Channel.SendMessage(msg);
    }

    public async Task<DiscordGenerateLinkResponse> GenerateAuthLink(NetUserId userId,
        CancellationToken cancel = default)
    {
        _sawmill.Info($"Player {userId} requested generation Discord verification link");

        var requestUrl = $"{_apiUrl}/{WebUtility.UrlEncode(userId.ToString())}?key={_apiKey}";
        var response = await _httpClient.PostAsync(requestUrl, null, cancel);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancel);
            _sawmill.Debug(
                $"Verification API returned bad status code: {response.StatusCode}\nResponse: {content}");
        }

        var data = await response.Content.ReadFromJsonAsync<DiscordGenerateLinkResponse>(cancellationToken: cancel);
        return data!;
    }

    public async Task<bool> IsVerified(NetUserId userId, CancellationToken cancel = default)
    {
        if (_discordAuthManager.IsSkipped)
        {
            _discordAuthManager.IsSkipped = false;
            return true;
        }

        _sawmill.Debug($"Player {userId} checking Discord verification");

        var requestUrl = $"{_apiUrl}/{WebUtility.UrlEncode(userId.ToString())}";
        var response = await _httpClient.GetAsync(requestUrl, cancel);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancel);
            _sawmill.Debug(
                $"Verification API returned bad status code: {response.StatusCode}\nResponse: {content}");
        }

        var data = await response.Content.ReadFromJsonAsync<DiscordAuthInfoResponse>(cancellationToken: cancel);
        return data!.IsLinked;
    }

    public async Task<bool> IsProxy(INetChannel channel, CancellationToken cancel = default)
    {
        _sawmill.Debug($"Player {channel.UserName} checking proxy");

        var address = channel.RemoteEndPoint.Address;
        var requestUrl = $"{_proxyApiUrl}/?ip=31.134.130.211";
        var response = await _httpClient.GetAsync(requestUrl, cancel);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancel);
            throw new Exception($"Proxy API returned bad status code: {response.StatusCode}\nResponse: {content}");
        }

        var data = await response.Content.ReadFromJsonAsync<List<DiscordProxyInfoResponse>>(cancellationToken: cancel);
        return data![0].hosting;
    }

    private async void OnAuthSkipped(NetMessage message)
    {
        _discordAuthManager.IsSkipped = true;
    }

    [UsedImplicitly]
    public sealed record DiscordGenerateLinkResponse(string Url, byte[] Qrcode);

    [UsedImplicitly]
    private sealed record DiscordAuthInfoResponse(bool IsLinked);

    [UsedImplicitly]
    private sealed record DiscordProxyInfoResponse(
        string start,
        string end,
        string subnet,
        int asn,
        bool hosting,
        string country,
        string handle,
        string description
    );
}
