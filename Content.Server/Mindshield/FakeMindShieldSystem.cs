using Content.Shared.Implants;
using Content.Shared.FakeSubdermalImplantComponent;
using Content.Shared.Tag;
using Content.Shared.FakeMindshield.Components;

namespace Content.Server.FakeMindshield;

public sealed class FakeMindShieldSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    [ValidatePrototypeId<TagPrototype>]
    public const string FakeMindShieldTag = "FakeMindShield";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FakeSubdermalImplantComponent, ImplantImplantedEvent>(ImplantCheck);
    }

    public void ImplantCheck(EntityUid uid, FakeSubdermalImplantComponent comp, ref ImplantImplantedEvent ev)
    {
        if (_tag.HasTag(ev.Implant, FakeMindShieldTag) && ev.Implanted != null)
        {
            EnsureComp<FakeMindShieldComponent>(ev.Implanted.Value);
        }
    }
}
