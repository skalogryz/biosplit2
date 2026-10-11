using Godot;
using System;
using System.Collections.Generic;

// Containers lay out visible TextureRects; gameplay code only selects their textures.
public class RageIndicator : Control
{
	[Export] public NodePath ProgressBarPath = new NodePath("Progress");
	[Export] public NodePath PointsPath = new NodePath("Points/Row");
	[Export] public NodePath PointTemplatePath = new NodePath("Points/Row/Point1");
	[Export] public Texture FullPointTexture;
	[Export] public Texture Quarter1Texture;
	[Export] public Texture Quarter2Texture;
	[Export] public Texture Quarter3Texture;
	[Export] public Texture Quarter4Texture;

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

		var row = Optional<HBoxContainer>(PointsPath);
		if (!Godot.Object.IsInstanceValid(row)) return;
		var points = new List<TextureRect>();
		foreach (Node child in row.GetChildren())
			if (child is TextureRect sprite && !sprite.IsQueuedForDeletion()) points.Add(sprite);

		int fullPoints = value / step;
		int remainder = value % step;
		int visiblePoints = fullPoints + (remainder > 0 ? 1 : 0);
		Texture partialTexture = null;
		if (remainder > 0)
		{
			int quarter = (int)((long)remainder * 4 / step);
			partialTexture = quarter == 0 ? Quarter1Texture : quarter == 1 ? Quarter2Texture : quarter == 2 ? Quarter3Texture : Quarter4Texture;
		}
		var template = Optional<TextureRect>(PointTemplatePath);
		// Reuse the scene's controls and duplicate its template only when more are needed.
		while (points.Count < visiblePoints && Godot.Object.IsInstanceValid(template))
		{
			var point = (TextureRect)template.Duplicate();
			row.AddChild(point);
			points.Add(point);
		}
		for (int i = 0; i < points.Count; i++)
		{
			points[i].Visible = i < visiblePoints;
			if (i < visiblePoints) points[i].Texture = i < fullPoints ? FullPointTexture : partialTexture;
		}
	}
}
