using Robust.Shared.Audio;

namespace Content.Server.Radium.Medical.Surgery.Components;

[RegisterComponent]
public sealed partial class DrapesComponent : Component
{
    /// <summary>
    ///     Sound played on healing begin
    /// </summary>
    [DataField("beginSound")]
    public SoundSpecifier? BeginSound = null;

    /// <summary>
    ///     Sound played on healing end
    /// </summary>
    [DataField("endSound")]
    public SoundSpecifier? EndSound = null;
}
