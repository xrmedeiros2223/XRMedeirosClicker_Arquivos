using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace AutoClickerIA;

public partial class OverlayWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;

    public OverlayWindow()
    {
        InitializeComponent();
        Loaded += OverlayWindow_Loaded;
        SourceInitialized += OverlayWindow_SourceInitialized;
    }

    private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Rect area = SystemParameters.WorkArea;
        Left = area.Right - Width - 18;
        Top = area.Top + 18;
    }

    private void OverlayWindow_SourceInitialized(object? sender, EventArgs e)
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        int style = GetWindowLong(handle, GwlExStyle);
        SetWindowLong(
            handle,
            GwlExStyle,
            style | WsExTransparent | WsExToolWindow | WsExNoActivate);
    }

    public void UpdateState(string status, int cps, string activation)
    {
        OverlayStatusText.Text = status;
        OverlayDetailText.Text = $"{cps} CPS  •  {activation}";

        switch (status)
        {
            case "RODANDO":
                OverlayStatusText.Foreground = Brushes.LimeGreen;
                OverlayStatusDot.Fill = Brushes.LimeGreen;
                break;
            case "PAUSADO":
                OverlayStatusText.Foreground = Brushes.Gold;
                OverlayStatusDot.Fill = Brushes.Gold;
                break;
            default:
                OverlayStatusText.Foreground = Brushes.White;
                OverlayStatusDot.Fill = new SolidColorBrush(Color.FromRgb(213, 0, 0));
                break;
        }
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr windowHandle, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr windowHandle, int index, int newStyle);
}
