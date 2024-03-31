﻿using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Radium.Medical.Surgery.Events;
using Content.Shared.Radium.Medical.Surgery.Systems;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Radium.Medical.Surgery.UI.Widgets.Systems;

public sealed class ClientDamagePartsSystem : DamagePartsSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public event EventHandler<IReadOnlyDictionary<(BodyPartType, BodyPartSymmetry), (int, bool)>>?
        SyncParts;

    public event EventHandler? Dispose;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<BodyComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeNetworkEvent<SyncPartsEvent>(OnPartsSync);
    }

    private void OnPartsSync(SyncPartsEvent ev)
    {
        SyncParts?.Invoke(this, GetDamagedParts(GetEntity(ev.Uid)));
    }

    public IReadOnlyDictionary<(BodyPartType, BodyPartSymmetry), (int, bool)>? PartsCondition(EntityUid? uid)
    {
        return uid is not null
            ? GetDamagedParts(uid.Value)
            : null;
    }

    private void OnPlayerAttached(EntityUid uid, BodyComponent component, LocalPlayerAttachedEvent args)
    {
        SyncParts?.Invoke(this, PartsCondition(uid)!);
    }

    private void OnPlayerDetached(EntityUid uid, BodyComponent component, LocalPlayerDetachedEvent args)
    {
        Dispose?.Invoke(this, EventArgs.Empty);
    }
}
