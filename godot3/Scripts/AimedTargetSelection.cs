using System;
using System.Collections.Generic;
using Biosplit.Game;
using Godot;

public class AimedTargetSelection : Control
{
    public sealed class TargetArea
    {
        public Character Character;
        public AnimatedSprite Sprite;
    }
    public event Action<Character> Selected;
    public event Action Cancelled;
    private readonly List<TargetArea> targets = new List<TargetArea>();
    private readonly List<Rect2> regions = new List<Rect2>();
    public bool IsSelecting { get; private set; }

    public void Open(IEnumerable<TargetArea> choices)
    {
        targets.Clear();
        targets.AddRange(choices);
        IsSelecting = true;
        UpdateRegions();
        SetDeferred("visible", true);
    }
    public void Close()
    {
        IsSelecting = false;
        Visible = false;
        SetDeferred("visible", false);
        targets.Clear();
        regions.Clear();
        Update();
    }
    public override void _Process(float delta)
    {
        if (IsSelecting) UpdateRegions();
    }
    private void UpdateRegions()
    {
        regions.Clear();
        targets.RemoveAll(t => t.Character == null || t.Character.isDefeated
            || !Godot.Object.IsInstanceValid(t.Sprite) || !t.Sprite.IsVisibleInTree());
        var centers = new List<float>();
        foreach (var target in targets)
            centers.Add((GetGlobalTransformWithCanvas().AffineInverse() * target.Sprite.GetGlobalTransformWithCanvas().origin).x);
        for (int i = 0; i < targets.Count; i++)
        {
            float left = centers[i] - 70;
            float right = centers[i] + 70;
            if (i > 0) left = Mathf.Max(left, (centers[i - 1] + centers[i]) / 2);
            if (i + 1 < targets.Count) right = Mathf.Min(right, (centers[i] + centers[i + 1]) / 2);
            left = Mathf.Clamp(left, 0, RectSize.x);
            right = Mathf.Clamp(right, 0, RectSize.x);
            regions.Add(new Rect2(left, 0, Mathf.Max(0, right - left), RectSize.y));
        }
        Update();
    }
    public override void _Draw()
    {
        if (!IsSelecting) return;
        foreach (var region in regions)
        {
            if (region.Size.x <= 0) continue;
            DrawRect(region, new Color(1, 0.75f, 0.25f, 0.25f));
            DrawRect(region, new Color(1, 0.85f, 0.45f, 0.8f), false, 2);
        }
    }
    public override void _GuiInput(InputEvent input)
    {
        if (!IsSelecting) return;
        Vector2 point;
        if (input is InputEventMouseButton mouse && mouse.Pressed)
        {
            if (mouse.ButtonIndex != (int)ButtonList.Left)
            {
                AcceptEvent();
                Cancelled?.Invoke();
                return;
            }
            point = mouse.Position;
        }
        else if (input is InputEventScreenTouch touch && touch.Pressed) point = touch.Position;
        else return;
        AcceptEvent();
        UpdateRegions();
        for (int i = 0; i < regions.Count; i++)
            if (regions[i].Size.x > 0 && regions[i].HasPoint(point))
            {
                Selected?.Invoke(targets[i].Character);
                return;
            }
        Cancelled?.Invoke();
    }
}