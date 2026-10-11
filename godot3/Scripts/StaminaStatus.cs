using System;
using Biosplit.Game;
using Godot;

public class StaminaStatus : Node2D
{
	[Export] public Vector2 Offset = new Vector2(0, -100);
	private Character character;
	private Node2D anchor;
	private ProgressBar staminaBar;

	public Character Character
	{
		get => character;
		set { character = value; UpdateView(); }
	}
	public Node2D Anchor
	{
		get => anchor;
		set { anchor = value; UpdateView(); }
	}
	public void Bind(Character value, Node2D target)
	{
		anchor = target;
		Character = value;
	}
	public override void _Ready()
	{
		SetAsToplevel(true);
		staminaBar = GetNode<ProgressBar>("StaminaBar");
		UpdateView();
	}
	public override void _Process(float delta) => UpdateView();
	public void UpdateView()
	{
		bool hasAnchor = Godot.Object.IsInstanceValid(anchor);
		Visible = character != null && (anchor == null || hasAnchor && anchor.IsVisibleInTree());
		if (hasAnchor) GlobalPosition = anchor.GlobalPosition + Offset;
		if (staminaBar == null) return;
		decimal maximum = Math.Max(0m, character?.maxStamina ?? 0m);
		staminaBar.MaxValue = (double)Math.Max(0.000001m, maximum);
		staminaBar.Value = (double)Math.Min(maximum, Math.Max(0m, character?.stamina ?? 0m));
	}
}
