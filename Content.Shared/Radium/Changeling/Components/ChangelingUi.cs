using Robust.Shared.Serialization;

namespace Content.Shared.Radium.Changeling.Components;

[NetSerializable, Serializable]
public enum ChangelingDnaStorageUiKey : byte
{
    Transform,
    Sting,
}

[Serializable, NetSerializable]
public sealed class ChangelingStorageUiState : BoundUserInterfaceState;
