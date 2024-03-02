using Robust.Shared.Configuration;

namespace Content.Shared.Radium;

[CVarDefs]
public sealed class CCVars
{
    /// <summary>
    ///     URL of the Proxy detection server API
    /// </summary>
    public static readonly CVarDef<string> DiscordProxyApiUrl =
        CVarDef.Create("discord_auth.proxy_url", "", CVar.SERVERONLY);

    public static readonly CVarDef<bool> DiscordProxyEnabled =
        CVarDef.Create("discord_auth.proxy_enabled", false, CVar.SERVERONLY);
}
