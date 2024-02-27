namespace Content.Corvax.Interfaces.Shared;

public interface ISharedDiscordAuthManager
{
    public void Initialize();

    public bool IsSkipped { get; set; }
}
