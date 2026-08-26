namespace AutoClickerIA.Config;

public class AppSettings
{
    public int MinCps { get; set; } = 10;
    public int MaxCps { get; set; } = 15;

    // true = clique automático esquerdo; false = direito
    public bool IsLeftClick { get; set; } = true;

    // Keyboard, Left ou Right
    public string ActivationInput { get; set; } = "Keyboard";

    // F8
    public int ActivationHotkey { get; set; } = 0x77;

    // false = alternar; true = ao pressionar
    public bool HoldMode { get; set; } = true;

    // F7
    public int PauseHotkey { get; set; } = 0x76;

    public bool OverlayEnabled { get; set; } = true;
}
