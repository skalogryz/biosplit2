using Godot;

// Control parent provides editable anchors; this node provides touch input.
[Tool]
public class TouchActionButton : TouchScreenButton
{
    private bool disabled;
    public bool Disabled
    {
        get => disabled;
        set
        {
            if (disabled == value) return;
            disabled = value;
            SetProcessInput(!disabled);
            GetParent<Control>().Modulate = disabled ? new Color(0.45f, 0.45f, 0.45f, 1) : Colors.White;
        }
    }

    public string Text
    {
        get => GetParent().GetNode<Label>("Caption").Text;
        set => GetParent().GetNode<Label>("Caption").Text = value;
    }

    public override void _Ready()
    {
        // Each button owns its hit area, including in the editor.
        Shape = new RectangleShape2D();
        ShapeCentered = false;
        ShapeVisible = false;
        UpdateHitArea();
    }

    public override void _Process(float delta) => UpdateHitArea();

    private void UpdateHitArea()
    {
        var parent = GetParent() as Control;
        if (parent == null) return;
        Position = parent.RectSize * 0.5f;
        if (Shape is RectangleShape2D rectangle)
            rectangle.Extents = parent.RectSize * 0.5f;
    }
}
