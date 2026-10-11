using Godot;

// Layout belongs to the selection screen; appearance belongs to TargetArea.tscn.
public class TargetAreaControl : Control
{
    [Signal] public delegate void Activated();
    [Export] public NodePath HitButtonPath = new NodePath("HitButton");
    private TouchScreenButton hitButton;
    private RectangleShape2D hitShape;

    public override void _Ready()
    {
        hitButton = HitButtonPath == null || HitButtonPath.IsEmpty()
            ? null : GetNodeOrNull<Node>(HitButtonPath) as TouchScreenButton;
        if (hitButton == null) return;
        hitShape = new RectangleShape2D();
        hitButton.Shape = hitShape;
        hitButton.ShapeCentered = false;
        hitButton.Connect("pressed", this, nameof(OnPressed));
        Connect("resized", this, nameof(UpdateHitArea));
        UpdateHitArea();
    }

    public void UpdateHitArea()
    {
        if (!Godot.Object.IsInstanceValid(hitButton)) return;
        hitButton.Position = RectSize * 0.5f;
        hitShape.Extents = RectSize * 0.5f;
    }

    public void OnPressed() => EmitSignal(nameof(Activated));
}