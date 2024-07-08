using Robust.Shared.Serialization;

namespace Content.Shared.Radium.Changeling.Components;

[NetSerializable, Serializable]
public enum ChangelingStorageUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class ChangelingStorageUiState : BoundUserInterfaceState;
