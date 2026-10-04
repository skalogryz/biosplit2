using Godot;

// Supports both standalone buttons and legacy Control + Caption wrappers.
[Tool]
public class TouchActionButton : TouchScreenButton
{
    private bool disabled;
    public bool Disabled
    {
        get => disabled;
        set
        {
            // Keep receiving releases while on cooldown; gameplay methods reject disabled actions.
            if (disabled == value) return;
            disabled = value;
            var wrapper = GetWrapper();
            CanvasItem visual = wrapper != null ? (CanvasItem)wrapper : this;
            visual.Modulate = disabled ? new Color(0.45f, 0.45f, 0.45f, 1) : Colors.White;
        }
    }

    public string Text
    {
        get => GetWrapper()?.GetNode<Label>("Caption").Text ?? "";
        set
        {
            var wrapper = GetWrapper();
            if (wrapper != null) wrapper.GetNode<Label>("Caption").Text = value;
        }
    }

    public override void _Ready()
    {
        SetProcessInput(true);
        if (GetWrapper() == null) return;
        // Only composite buttons derive their hit area from the wrapper.
        Shape = new RectangleShape2D();
        ShapeCentered = false;
        ShapeVisible = false;
        UpdateHitArea();
    }

    public override void _Process(float delta) => UpdateHitArea();

    private void UpdateHitArea()
    {
        var parent = GetWrapper();
        if (parent == null) return;
        Position = parent.RectSize * 0.5f;
        if (Shape is RectangleShape2D rectangle)
            rectangle.Extents = parent.RectSize * 0.5f;
    }

    private Control GetWrapper()
    {
        var parent = GetParent() as Control;
        return parent != null && parent.GetNodeOrNull<Label>("Caption") != null ? parent : null;
    }
}
