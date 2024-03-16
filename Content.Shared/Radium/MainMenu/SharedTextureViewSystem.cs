using Robust.Shared.Serialization;

namespace Content.Shared.Radium.MainMenu;

/// <summary>
/// Handles main menu texture.
/// </summary>
public abstract class SharedTextureViewSystem: EntitySystem
{
    [Serializable, NetSerializable]
    protected sealed class TextureComponentState : ComponentState
    {
        public string TexturePath = string.Empty;
    }
}
