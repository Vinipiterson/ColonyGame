using Godot;

[GlobalClass]
public partial class DragColor : Resource
{
    [Export]
    public PlayerMode Mode { get; set; }

    [Export]
    public Color FillColor { get; set; } = new Color(1, 1, 1, 0.15f);

    [Export]
    public Color BorderColor { get; set; } = Colors.White;
}