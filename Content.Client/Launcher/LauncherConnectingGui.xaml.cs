using System.Linq;
using Content.Client.Stylesheets;
using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Localization;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.Launcher
{
    [GenerateTypedNameReferences]
    public sealed partial class LauncherConnectingGui : Control
    {
        public static readonly SpriteSpecifier Sprite =
            new SpriteSpecifier.Rsi(new ResPath("/Textures/Radium/Menu/maina.rsi"), "maina");

        private const float RedialWaitTimeSeconds = 15f;
        private readonly LauncherConnecting _state;
        private readonly IRobustRandom _random;
        private readonly IPrototypeManager _prototype;
        private readonly IConfigurationManager _cfg;

        private float _redialWaitTime = RedialWaitTimeSeconds;

        public LauncherConnectingGui(LauncherConnecting state, IRobustRandom random,
            IPrototypeManager prototype, IConfigurationManager config)
        {
            _state = state;
            _random = random;
            _prototype = prototype;
            _cfg = config;

            RobustXamlLoader.Load(this);

            LayoutContainer.SetAnchorPreset(this, LayoutContainer.LayoutPreset.Wide);

            Stylesheet = IoCManager.Resolve<IStylesheetManager>().SheetSpace;

            Background.SetFromSpriteSpecifier(Sprite);
            Background.HorizontalAlignment = HAlignment.Stretch;
            Background.VerticalAlignment = VAlignment.Stretch;
            Background.DisplayRect.Stretch = TextureRect.StretchMode.KeepAspectCentered;

            ChangeLoginTip();
            ReconnectButton.OnPressed += _ => _state.RetryConnect();
            // Redial shouldn't fail, but if it does, try a reconnect (maybe we're being run from debug)
            RedialButton.OnPressed += _ =>
            {
                if (!_state.Redial())
                    _state.RetryConnect();
            };
            RetryButton.OnPressed += _ => _state.RetryConnect();
            ExitButton.OnPressed += _ => _state.Exit();

            var addr = state.Address;
            if (addr != null)
                ConnectingAddress.Text = addr;

            state.PageChanged += OnPageChanged;
            state.ConnectFailReasonChanged += ConnectFailReasonChanged;
            state.ConnectionStateChanged += ConnectionStateChanged;

            ConnectionStateChanged(state.ConnectionState);

            // Redial flag setup
            var edim = IoCManager.Resolve<ExtendedDisconnectInformationManager>();
            edim.LastNetDisconnectedArgsChanged += LastNetDisconnectedArgsChanged;
            LastNetDisconnectedArgsChanged(edim.LastNetDisconnectedArgs);
        }

        private void ConnectFailReasonChanged(string? reason)
        {
            ConnectFailReason.SetMessage(reason == null
                ? ""
                : Loc.GetString("connecting-fail-reason", ("reason", reason)));
        }

        private void LastNetDisconnectedArgsChanged(NetDisconnectedArgs? args)
        {
            var redialFlag = args?.RedialFlag ?? false;
            RedialButton.Visible = redialFlag;
            ReconnectButton.Visible = !redialFlag;
        }

        private void ChangeLoginTip()
        {
            var tipsDataset = _cfg.GetCVar(CCVars.TipsDataset);
            var loginTipsEnabled = _prototype.TryIndex<DatasetPrototype>(tipsDataset, out var tips);

            LoginTips.Visible = loginTipsEnabled;
            if (!loginTipsEnabled)
            {
                return;
            }

            var tipList = tips!.Values;

            if (tipList.Count == 0)
                return;

            var randomIndex = _random.Next(tipList.Count);
            var tip = tipList[randomIndex];
            LoginTip.SetMessage(tip);

            LoginTipTitle.Text = Loc.GetString("connecting-window-tip", ("numberTip", randomIndex));
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            _redialWaitTime -= args.DeltaSeconds;
            if (_redialWaitTime <= 0)
            {
                RedialButton.Disabled = false;
                RedialButton.Text = Loc.GetString("connecting-redial");
            }
            else
            {
                RedialButton.Disabled = true;
                RedialButton.Text =
                    Loc.GetString("connecting-redial-wait", ("time", _redialWaitTime.ToString("00.000")));
            }
        }

        private void OnPageChanged(LauncherConnecting.Page page)
        {
            ConnectingStatus.Visible = page == LauncherConnecting.Page.Connecting;
            ConnectFail.Visible = page == LauncherConnecting.Page.ConnectFailed;
            Disconnected.Visible = page == LauncherConnecting.Page.Disconnected;

            if (page == LauncherConnecting.Page.Disconnected)
                DisconnectReason.Text = _state.LastDisconnectReason;
        }

        private void ConnectionStateChanged(ClientConnectionState state)
        {
            ConnectStatus.Text = Loc.GetString($"connecting-state-{state}");
        }
    }
}
