using Godot;

public partial class GameUI : Control
{
    [Export] private Label _fpsLabel;
    [Export] private Label _frameTimeLabel;
    [Export] private Label _colonistCountLabel;
    [Export] private Label _playerModeLabel;

    public override void _Process(double delta)
    {
        double frameTimeMs = delta * 1000.0;
        int fps = (int)Engine.GetFramesPerSecond();

        _fpsLabel.Text = $"FPS: {fps}";
        _frameTimeLabel.Text = $"Frame Time: {frameTimeMs:F2} ms";

        int colonistCount = GetTree().GetNodesInGroup("colonists").Count;
        _colonistCountLabel.Text = $"Colonists: {colonistCount}";

        _playerModeLabel.Text = $"Mode: {GameServices.GetPlayerController().CurrentMode}";
    }
}