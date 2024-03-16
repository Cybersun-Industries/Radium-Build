using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Client.Parallax;
using Content.Client.Radium.MainMenu.Data;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using static System.Threading.Tasks.Task;

namespace Content.Client.Radium.MainMenu.Managers;

public sealed class TextureViewManager : ITexureViewManager
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private ISawmill _sawmill = Logger.GetSawmill("textureView");
    public Vector2 ParallaxAnchor { get; set; }

    private readonly Dictionary<string, TextureViewPrepared[]> _textureViewList = new();

    private readonly Dictionary<string, CancellationTokenSource> _loadingView = new();

    public bool IsLoaded(string name)
    {
        return _textureViewList.ContainsKey(name);
    }

    public TextureViewPrepared[] GetLayers(string name)
    {
        return !_textureViewList.TryGetValue(name, out var tx) ? Array.Empty<TextureViewPrepared>() : tx;
    }

    public void UnloadView(string name)
    {
        if (_loadingView.TryGetValue(name, out var loading))
        {
            loading.Cancel();
            _loadingView.Remove(name, out _);
            return;
        }

        if (!_textureViewList.ContainsKey(name))
            return;

        _textureViewList.Remove(name);
    }

    public async Task LoadViewByName(string name)
    {
        if (_loadingView.ContainsKey(name))
            return;

        // Cancel any existing load and setup the new cancellation token
        var token = new CancellationTokenSource();
        _loadingView[name] = token;
        var cancel = token.Token;

        // Begin
        _sawmill.Debug($"Loading view {name}");

        try
        {
            var parallaxPrototype = _prototypeManager.Index<TextureViewPrototype>(name);

            TextureViewPrepared[] layers;

            layers = await LoadTextureViewLayers(parallaxPrototype.Layers, cancel);


            _textureViewList.Remove(name, out _);

            if (token.Token.IsCancellationRequested)
                return;

            _textureViewList[name] = layers;
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to load textureView {name}: {ex}");
        }
    }
    private async Task<TextureViewPrepared[]> LoadTextureViewLayers(List<TextureViewConfig> layersIn, CancellationToken cancel = default)
    {
        // Because this is async, make sure it doesn't change (prototype reloads could muck this up)
        // Since the tasks aren't awaited until the end, this should be fine
        var tasks = new Task<TextureViewPrepared>[layersIn.Count];
        for (var i = 0; i < layersIn.Count; i++)
        {
            tasks[i] = LoadTextureViewLayer(layersIn[i], cancel);
        }
        return await WhenAll(tasks);
    }

    private async Task<TextureViewPrepared> LoadTextureViewLayer(TextureViewConfig config, CancellationToken cancel = default)
    {
        return new TextureViewPrepared()
        {
            Texture = await config.Texture.GenerateTexture(cancel),
            Config = config
        };
    }
}
