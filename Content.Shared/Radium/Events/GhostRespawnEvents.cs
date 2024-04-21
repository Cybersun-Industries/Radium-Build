using System.ComponentModel;
using Robust.Shared.Serialization;

namespace Content.Shared.Radium.Events;

[NetSerializable, Serializable]
public sealed class SyncTimeEvent(int seconds, bool isAvailable) : EntityEventArgs
{
    public int TimeRemaining = seconds;
    public bool IsAvailable = isAvailable;
}

[NetSerializable, Serializable]
public sealed class RespawnRequestEvent : EntityEventArgs;

[NetSerializable, Serializable]
public sealed class RespawnResponseEvent : EntityEventArgs;
