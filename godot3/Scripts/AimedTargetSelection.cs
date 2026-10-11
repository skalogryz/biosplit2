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
		internal TargetAreaControl View;
	}
	[Export] public PackedScene TargetAreaScene;
	[Export] public float TargetAreaWidth = 140f;
	[Export] public NodePath AreasContainerPath = new NodePath("Areas");
	[Export] public NodePath CancelAreaPath = new NodePath("CancelArea");
	public event Action<Character> Selected;
	public event Action Cancelled;
	private readonly List<TargetArea> targets = new List<TargetArea>();
	private Control areasContainer;
	public bool IsSelecting { get; private set; }

	public override void _Ready()
	{
		areasContainer = GetNodeOrNull<Control>(AreasContainerPath);
		var cancelArea = GetNodeOrNull<TargetAreaControl>(CancelAreaPath);
		cancelArea?.Connect(nameof(TargetAreaControl.Activated), this, nameof(OnCancelPressed));
	}

	public void Open(IEnumerable<TargetArea> choices)
	{
		Close();
		if (TargetAreaScene == null || areasContainer == null) return;
		foreach (var choice in choices)
		{
			var instance = TargetAreaScene.Instance();
			var view = instance as TargetAreaControl;
			if (view == null)
			{
				instance.Free();
				GD.PushWarning("TargetAreaScene must have a TargetAreaControl root.");
				continue;
			}
			choice.View = view;
			targets.Add(choice);
			areasContainer.AddChild(view);
			view.Connect(nameof(TargetAreaControl.Activated), this, nameof(OnTargetPressed),
				new Godot.Collections.Array { view });
		}
		IsSelecting = true;
		UpdateRegions();
		SetDeferred("visible", true);
	}

	public void Close()
	{
		IsSelecting = false;
		Visible = false;
		SetDeferred("visible", false);
		foreach (var target in targets)
			if (Godot.Object.IsInstanceValid(target.View))
			{
				target.View.Hide();
				target.View.QueueFree();
			}
		targets.Clear();
	}

	public override void _Process(float delta)
	{
		if (IsSelecting) UpdateRegions();
	}

	private void UpdateRegions()
	{
		for (int i = targets.Count - 1; i >= 0; i--)
		{
			var target = targets[i];
			if (target.Character != null && !target.Character.isDefeated
				&& Godot.Object.IsInstanceValid(target.Sprite) && target.Sprite.IsVisibleInTree()) continue;
			if (Godot.Object.IsInstanceValid(target.View)) { target.View.Hide(); target.View.QueueFree(); }
			targets.RemoveAt(i);
		}
		var centers = new List<float>();
		foreach (var target in targets)
			centers.Add((areasContainer.GetGlobalTransformWithCanvas().AffineInverse()
				* target.Sprite.GetGlobalTransformWithCanvas().origin).x);
		for (int i = 0; i < targets.Count; i++)
		{
			float halfWidth = Mathf.Max(0, TargetAreaWidth) / 2;
			float left = centers[i] - halfWidth;
			float right = centers[i] + halfWidth;
			if (i > 0) left = Mathf.Max(left, (centers[i - 1] + centers[i]) / 2);
			if (i + 1 < targets.Count) right = Mathf.Min(right, (centers[i] + centers[i + 1]) / 2);
			left = Mathf.Clamp(left, 0, areasContainer.RectSize.x);
			right = Mathf.Clamp(right, 0, areasContainer.RectSize.x);
			var view = targets[i].View;
			view.RectPosition = new Vector2(left, 0);
			view.RectSize = new Vector2(Mathf.Max(0, right - left), areasContainer.RectSize.y);
			view.Visible = right > left;
		}
	}

	public void OnTargetPressed(TargetAreaControl view)
	{
		if (!IsSelecting) return;
		foreach (var target in targets)
			if (target.View == view && !target.Character.isDefeated)
			{
				Selected?.Invoke(target.Character);
				return;
			}
	}

	public void OnCancelPressed()
	{
		// The background also receives touches inside a target area. Let that area's
		// signal select the target before deciding whether this was an outside click.
		CallDeferred(nameof(CancelIfSelecting));
	}

	public void CancelIfSelecting()
	{
		if (IsSelecting) Cancelled?.Invoke();
	}
}
