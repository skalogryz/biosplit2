using Godot;
using System;
using System.Collections.Generic;

// Scene-based progress bar and Sprite pool; no custom drawing.
public class RageIndicator : Control
{
	[Export] public NodePath ProgressBarPath = new NodePath("Progress");
	[Export] public NodePath PointsPath = new NodePath("Points");
	[Export] public NodePath PointTemplatePath = new NodePath("Points/Point1");
	[Export] public float PointSpacing = 18f;

	private T Optional<T>(NodePath path) where T : Node
	{
		return path == null || path.IsEmpty() ? null : GetNodeOrNull<Node>(path) as T;
	}

	public void UpdateRage(int rage, int multiple)
	{
		int step = Math.Max(1, multiple);
		int value = Math.Max(0, rage);
		var bar = Optional<ProgressBar>(ProgressBarPath);
		if (Godot.Object.IsInstanceValid(bar))
		{
			bar.MinValue = 0;
			bar.MaxValue = step;
			bar.Step = 1;
			bar.Value = value % step;
		}

		var row = Optional<Node2D>(PointsPath);
		if (!Godot.Object.IsInstanceValid(row)) return;
		var points = new List<Sprite>();
		foreach (Node child in row.GetChildren())
			if (child is Sprite sprite && !sprite.IsQueuedForDeletion()) points.Add(sprite);

		int fullPoints = value / step;
		var template = Optional<Sprite>(PointTemplatePath);
		// Reuse the scene's sprites and duplicate its template only when more are needed.
		while (points.Count < fullPoints && Godot.Object.IsInstanceValid(template))
		{
			var point = (Sprite)template.Duplicate();
			row.AddChild(point);
			points.Add(point);
		}
		for (int i = 0; i < points.Count; i++)
		{
			points[i].Position = new Vector2(i * Math.Max(0, PointSpacing), 0);
			points[i].Visible = i < fullPoints;
		}
	}
}
