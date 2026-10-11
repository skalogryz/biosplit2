using Godot;
using Biosplit.Game;
using System;

public class CharacterStatus : Node2D
{
	[Export] public Vector2 Offset = new Vector2(0, -100);
	private Character character;
	private Node2D anchor;
	private int initialMaxHealth = 1;
	private Label nameLabel;
	private ProgressBar healthBar;

	public Character Character
	{
		get => character;
		set
		{
			if (character != value)
			{
				character = value;
				initialMaxHealth = Math.Max(1, value?.health ?? 1);
			}
			UpdateView();
		}
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
		nameLabel = GetNode<Label>("NameLabel");
		healthBar = GetNode<ProgressBar>("HealthBar");
		UpdateView();
	}
	public override void _Process(float delta) => UpdateView();

	public void UpdateView()
	{
		bool hasAnchor = Godot.Object.IsInstanceValid(anchor);
		Visible = character != null && (anchor == null || hasAnchor && anchor.IsVisibleInTree());
		if (hasAnchor) GlobalPosition = anchor.GlobalPosition + Offset;
		if (nameLabel == null || healthBar == null) return;
		nameLabel.Text = character?.Name ?? "";
		healthBar.MaxValue = Math.Max(1, character?.EnemySettings?.Health ?? initialMaxHealth);
		healthBar.Value = Math.Max(0, character?.health ?? 0);
	}
}
