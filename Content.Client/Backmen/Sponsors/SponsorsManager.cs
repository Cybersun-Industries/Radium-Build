using System.Diagnostics.CodeAnalysis;
using Content.Corvax.Interfaces.Client;
using Content.Shared.Backmen.Sponsors;
using Robust.Shared.Network;

namespace Content.Client.Backmen.Sponsors;

public sealed class SponsorsManager : IClientSponsorsManager
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgSponsorInfo>(OnUpdate);
        _netMgr.RegisterNetMessage<Shared.Backmen.MsgWhitelist>(RxWhitelist); //backmen: whitelist
    }

    private void RxWhitelist(Shared.Backmen.MsgWhitelist message)
    {
        Whitelisted = message.Whitelisted;
    }

    private void OnUpdate(MsgSponsorInfo message)
    {
        Reset();

#if DEBUG
        Prototypes.Add("tier1");
        Prototypes.Add("tier2");
        Prototypes.Add("tier01");
        Prototypes.Add("tier02");
        Prototypes.Add("tier03");
        Prototypes.Add("tier04");
        Prototypes.Add("tier05");
        Prototypes.Add("tier6");
        Prototypes.Add("tier7");
        Prototypes.Add("tier8");
        Prototypes.Add("tier9");
        Prototypes.Add("tier10");
        Prototypes.Add("tier11");
        Prototypes.Add("tier12");
        Prototypes.Add("tier13");
        Prototypes.Add("tier14");
        Prototypes.Add("tier15");
        Prototypes.Add("tier16");
        Prototypes.Add("tier17");
        Prototypes.Add("tier18");
        Prototypes.Add("tier19");
        Prototypes.Add("tier20");
        Prototypes.Add("tier21");
        Prototypes.Add("tier22");
        Prototypes.Add("tier23");
#endif

        if (message.Info == null)
        {
            return;
        }

        foreach (var markings in message.Info.AllowedMarkings)
        {
            Prototypes.Add(markings);
        }

        Tier = message.Info.Tier ?? 0;
    }

    private void Reset()
    {
        Prototypes.Clear();
        Tier = 0;
    }

    public HashSet<string> Prototypes { get; } = new();
    public int Tier { get; private set; } = 0;
    public bool Whitelisted { get; private set; } = false;
}
