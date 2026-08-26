using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AutoClickerIA.Config;
using AutoClickerIA.Core;
using AutoClickerIA.Services;

namespace AutoClickerIA;

public partial class MainWindow : Window
{
    private readonly ClickEngine _clickEngine = new();
    private readonly HotkeyManager _activationManager;
    private readonly HotkeyManager _pauseManager;
    private readonly OverlayWindow _overlay;
    private readonly DispatcherTimer _overlayTimer;

    private AppSettings _settings;

    private bool _initializing = true;
    private bool _capturingActivation;
    private bool _capturingPause;
    private bool _isPaused;

    public MainWindow()
    {
        InitializeComponent();

        _settings = ConfigManager.Load();
        NormalizeSettings();

        _clickEngine.Settings = _settings;

        GetActivationBinding(out int activationKey, out bool activationIsMouse);

        _activationManager = new HotkeyManager(
            activationKey,
            activationIsMouse,
            _settings.HoldMode);

        _pauseManager = new HotkeyManager(
            _settings.PauseHotkey,
            false,
            false);

        _overlay = new OverlayWindow();
        if (_settings.OverlayEnabled)
            _overlay.Show();

        _overlayTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _overlayTimer.Tick += (_, _) => RefreshOverlay();

        LoadInterface();
        ConnectEvents();

        _activationManager.ToggleRequested += ToggleClicker;
        _activationManager.HoldStateChanged += SetHoldState;
        _pauseManager.ToggleRequested += TogglePause;

        _activationManager.Start();
        _pauseManager.Start();
        _overlayTimer.Start();

        _initializing = false;
        UpdateStatus();
    }

    private void NormalizeSettings()
    {
        _settings.MinCps = Math.Clamp(_settings.MinCps, 1, 30);
        _settings.MaxCps = Math.Clamp(_settings.MaxCps, 1, 30);

        if (_settings.MinCps > _settings.MaxCps)
            _settings.MaxCps = _settings.MinCps;

        if (_settings.ActivationHotkey <= 0)
            _settings.ActivationHotkey = 0x77;

        if (_settings.PauseHotkey <= 0)
            _settings.PauseHotkey = 0x76;

        if (_settings.ActivationHotkey == _settings.PauseHotkey)
            _settings.PauseHotkey = 0x76;

        if (_settings.ActivationInput is not ("Keyboard" or "Left" or "Right"))
            _settings.ActivationInput = "Keyboard";

        if (_settings.ActivationInput != "Keyboard")
            _settings.HoldMode = true;
    }

    private void LoadInterface()
    {
        SliderMin.Value = _settings.MinCps;
        SliderMax.Value = _settings.MaxCps;

        RbLeft.IsChecked = _settings.IsLeftClick;
        RbRight.IsChecked = !_settings.IsLeftClick;

        RbActivationKeyboard.IsChecked = _settings.ActivationInput == "Keyboard";
        RbActivationLeft.IsChecked = _settings.ActivationInput == "Left";
        RbActivationRight.IsChecked = _settings.ActivationInput == "Right";

        RbToggle.IsChecked = !_settings.HoldMode;
        RbHold.IsChecked = _settings.HoldMode;

        TxtActivationHotkey.Text = InputListener.GetInputName(_settings.ActivationHotkey);
        TxtPauseHotkey.Text = InputListener.GetInputName(_settings.PauseHotkey);

        UpdateCpsTexts();
        UpdateActivationUi();
    }

    private void ConnectEvents()
    {
        SliderMin.ValueChanged += SettingsChanged;
        SliderMax.ValueChanged += SettingsChanged;

        RbLeft.Checked += SettingsChanged;
        RbRight.Checked += SettingsChanged;

        RbActivationKeyboard.Checked += ActivationInputChanged;
        RbActivationLeft.Checked += ActivationInputChanged;
        RbActivationRight.Checked += ActivationInputChanged;

        RbToggle.Checked += ModeChanged;
        RbHold.Checked += ModeChanged;

        BtnToggle.Click += (_, _) => ToggleClicker();
        BtnChangeActivation.Click += (_, _) => BeginCapture(true);
        BtnChangePause.Click += (_, _) => BeginCapture(false);

        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private void SettingsChanged(object sender, RoutedEventArgs e)
    {
        SaveInterfaceSettings();
    }

    private void ActivationInputChanged(object sender, RoutedEventArgs e)
    {
        if (_initializing)
            return;

        _clickEngine.Stop();

        _settings.ActivationInput = GetSelectedActivationInput();

        if (_settings.ActivationInput != "Keyboard")
        {
            _settings.HoldMode = true;
            _initializing = true;
            RbHold.IsChecked = true;
            RbToggle.IsChecked = false;
            _initializing = false;
        }

        UpdateActivationUi();
        UpdateActivationManager();
        ConfigManager.Save(_settings);
        UpdateStatus();
    }

    private void ModeChanged(object sender, RoutedEventArgs e)
    {
        if (_initializing)
            return;

        if (_settings.ActivationInput != "Keyboard")
        {
            _initializing = true;
            RbHold.IsChecked = true;
            RbToggle.IsChecked = false;
            _initializing = false;
            _settings.HoldMode = true;
            return;
        }

        _clickEngine.Stop();
        SaveInterfaceSettings();
        UpdateStatus();
    }

    private string GetSelectedActivationInput()
    {
        if (RbActivationLeft.IsChecked == true)
            return "Left";

        if (RbActivationRight.IsChecked == true)
            return "Right";

        return "Keyboard";
    }

    private void SaveInterfaceSettings()
    {
        if (_initializing)
            return;

        int minCps = (int)SliderMin.Value;
        int maxCps = (int)SliderMax.Value;

        if (minCps > maxCps)
        {
            maxCps = minCps;
            SliderMax.Value = maxCps;
        }

        _settings.MinCps = minCps;
        _settings.MaxCps = maxCps;
        _settings.IsLeftClick = RbLeft.IsChecked == true;
        _settings.ActivationInput = GetSelectedActivationInput();
        _settings.HoldMode = _settings.ActivationInput != "Keyboard" || RbHold.IsChecked == true;

        _clickEngine.Settings = _settings;

        UpdateCpsTexts();
        UpdateActivationUi();
        UpdateActivationManager();

        ConfigManager.Save(_settings);
    }

    private void UpdateCpsTexts()
    {
        TxtMin.Text = ((int)SliderMin.Value).ToString();
        TxtMax.Text = ((int)SliderMax.Value).ToString();
    }

    private void UpdateActivationUi()
    {
        bool keyboard = _settings.ActivationInput == "Keyboard";

        KeyboardBindPanel.Visibility = keyboard
            ? Visibility.Visible
            : Visibility.Collapsed;

        MouseActivationHint.Visibility = keyboard
            ? Visibility.Collapsed
            : Visibility.Visible;

        RbToggle.IsEnabled = keyboard;
        RbHold.IsEnabled = true;
    }

    private void GetActivationBinding(out int key, out bool isMouse)
    {
        switch (_settings.ActivationInput)
        {
            case "Left":
                key = InputListener.VkLeftMouse;
                isMouse = true;
                break;
            case "Right":
                key = InputListener.VkRightMouse;
                isMouse = true;
                break;
            default:
                key = _settings.ActivationHotkey;
                isMouse = false;
                break;
        }
    }

    private void UpdateActivationManager()
    {
        GetActivationBinding(out int key, out bool isMouse);
        _activationManager.UpdateBinding(
            key,
            isMouse,
            _settings.ActivationInput != "Keyboard" || _settings.HoldMode);
    }

    private void ToggleClicker()
    {
        if (IsCapturing() || _isPaused)
            return;

        if (_clickEngine.IsRunning)
            _clickEngine.Stop();
        else
            StartClicker();

        UpdateStatus();
    }

    private void SetHoldState(bool pressed)
    {
        if (IsCapturing() || _isPaused)
            return;

        bool effectiveHold = _settings.ActivationInput != "Keyboard" || _settings.HoldMode;
        if (!effectiveHold)
            return;

        if (pressed)
            StartClicker();
        else
            _clickEngine.Stop();

        UpdateStatus();
    }

    private void StartClicker()
    {
        if (_isPaused)
            return;

        SaveInterfaceSettings();
        _clickEngine.Start();
    }

    private void TogglePause()
    {
        if (IsCapturing())
            return;

        _isPaused = !_isPaused;
        _clickEngine.Stop();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (_isPaused)
        {
            TxtStatus.Text = "PAUSADO";
            TxtStatus.Foreground = Brushes.Gold;
            StatusDot.Fill = Brushes.Gold;
            BtnToggle.Content = "MÓDULO PAUSADO";
            BtnToggle.IsEnabled = false;
        }
        else if (_clickEngine.IsRunning)
        {
            TxtStatus.Text = "RODANDO";
            TxtStatus.Foreground = Brushes.LimeGreen;
            StatusDot.Fill = Brushes.LimeGreen;
            BtnToggle.Content = "PARAR MÓDULO";
            BtnToggle.IsEnabled = true;
        }
        else
        {
            TxtStatus.Text = "PARADO";
            TxtStatus.Foreground = Brushes.White;
            StatusDot.Fill = new SolidColorBrush(Color.FromRgb(213, 0, 0));
            BtnToggle.Content = "INICIAR MÓDULO";
            BtnToggle.IsEnabled = true;
        }

        RefreshOverlay();
    }

    private void RefreshOverlay()
    {
        if (!_settings.OverlayEnabled || !_overlay.IsVisible)
            return;

        string status = _isPaused
            ? "PAUSADO"
            : _clickEngine.IsRunning
                ? "RODANDO"
                : "PARADO";

        string activation = _settings.ActivationInput switch
        {
            "Left" => "MOUSE LEFT",
            "Right" => "MOUSE RIGHT",
            _ => InputListener.GetInputName(_settings.ActivationHotkey)
        };

        _overlay.UpdateState(status, _clickEngine.CurrentCps, activation);
    }

    private void BeginCapture(bool activation)
    {
        _capturingActivation = activation;
        _capturingPause = !activation;

        _clickEngine.Stop();
        _activationManager.Stop();
        _pauseManager.Stop();

        if (activation)
        {
            TxtActivationHotkey.Text = "AGUARDANDO...";
            BtnChangeActivation.Content = "...";
        }
        else
        {
            TxtPauseHotkey.Text = "AGUARDANDO...";
            BtnChangePause.Content = "...";
        }

        BtnChangeActivation.IsEnabled = activation;
        BtnChangePause.IsEnabled = !activation;

        Focus();
        Keyboard.Focus(this);
        UpdateStatus();
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsCapturing())
            return;

        Key selectedKey = e.Key == Key.System ? e.SystemKey : e.Key;

        if (selectedKey == Key.Escape)
        {
            CancelCapture();
            e.Handled = true;
            return;
        }

        int virtualKey = KeyInterop.VirtualKeyFromKey(selectedKey);
        if (virtualKey <= 0)
            return;

        if (_capturingActivation)
        {
            if (virtualKey == _settings.PauseHotkey)
            {
                ShowConflict();
                e.Handled = true;
                return;
            }

            _settings.ActivationHotkey = virtualKey;
            TxtActivationHotkey.Text = InputListener.GetInputName(virtualKey);
        }
        else
        {
            if (virtualKey == _settings.ActivationHotkey && _settings.ActivationInput == "Keyboard")
            {
                ShowConflict();
                e.Handled = true;
                return;
            }

            _settings.PauseHotkey = virtualKey;
            TxtPauseHotkey.Text = InputListener.GetInputName(virtualKey);
        }

        FinishCapture();
        e.Handled = true;
    }

    private static void ShowConflict()
    {
        MessageBox.Show(
            "A ativação e a pausa não podem usar a mesma tecla.",
            "BIND EM CONFLITO",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void FinishCapture()
    {
        _capturingActivation = false;
        _capturingPause = false;

        BtnChangeActivation.Content = "ALTERAR";
        BtnChangePause.Content = "ALTERAR";
        BtnChangeActivation.IsEnabled = true;
        BtnChangePause.IsEnabled = true;

        UpdateActivationManager();
        _pauseManager.UpdateBinding(_settings.PauseHotkey, false, false);

        _activationManager.Start();
        _pauseManager.Start();

        ConfigManager.Save(_settings);
        RefreshOverlay();
    }

    private void CancelCapture()
    {
        _capturingActivation = false;
        _capturingPause = false;

        TxtActivationHotkey.Text = InputListener.GetInputName(_settings.ActivationHotkey);
        TxtPauseHotkey.Text = InputListener.GetInputName(_settings.PauseHotkey);

        BtnChangeActivation.Content = "ALTERAR";
        BtnChangePause.Content = "ALTERAR";
        BtnChangeActivation.IsEnabled = true;
        BtnChangePause.IsEnabled = true;

        _activationManager.Start();
        _pauseManager.Start();
    }

    private bool IsCapturing() => _capturingActivation || _capturingPause;

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _clickEngine.Stop();
        _overlayTimer.Stop();

        _activationManager.ToggleRequested -= ToggleClicker;
        _activationManager.HoldStateChanged -= SetHoldState;
        _pauseManager.ToggleRequested -= TogglePause;

        _activationManager.Dispose();
        _pauseManager.Dispose();

        _overlay.Close();
        ConfigManager.Save(_settings);

        base.OnClosed(e);
    }
}
