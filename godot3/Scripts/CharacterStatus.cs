using Godot;
using Biosplit.Game;
using System;

public class CharacterStatus : Node2D
{
	[Export] public NodePath NameLabelPath;
	[Export] public NodePath LevelLabelPath;
	[Export] public NodePath HealthBarPath;
	[Export] public NodePath HealthLabelPath;
	// Duration in seconds; zero updates health immediately.
	[Export] public float HealthAnimationDuration = 0f;
	[Export] public Vector2 Offset = new Vector2(0, -100);
	private Character character;
	private Node2D anchor;
	private int initialMaxHealth = 1;
	private Label nameLabel;
	private Label levelLabel;
	private ProgressBar healthBar;
	private Label healthLabel;
	private double displayedHealth;
	private double healthAnimationStart;
	private int targetHealth;
	private double healthAnimationElapsed;

	public Character Character
	{
		get => character;
		set
		{
			if (character != value)
			{
				character = value;
				initialMaxHealth = Math.Max(1, value?.health ?? 1);
				targetHealth = Math.Max(0, value?.health ?? 0);
				displayedHealth = healthAnimationStart = targetHealth;
				healthAnimationElapsed = 0;
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
		nameLabel = Resolve<Label>(NameLabelPath);
		levelLabel = Resolve<Label>(LevelLabelPath);
		healthBar = Resolve<ProgressBar>(HealthBarPath);
		healthLabel = Resolve<Label>(HealthLabelPath);
		UpdateView();
	}
	public override void _Process(float delta)
	{
		UpdateHealthAnimation(delta);
		UpdateView();
	}

	public void UpdateView()
	{
		UpdateHealthAnimation(0);
		bool hasAnchor = Godot.Object.IsInstanceValid(anchor);
		Visible = character != null && (anchor == null || hasAnchor && anchor.IsVisibleInTree());
		if (hasAnchor) GlobalPosition = anchor.GlobalPosition + Offset;
		if (Godot.Object.IsInstanceValid(nameLabel))
			nameLabel.Text = character?.Name ?? "";
		if (Godot.Object.IsInstanceValid(levelLabel))
			levelLabel.Text = character?.level.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
		if (Godot.Object.IsInstanceValid(healthLabel))
			healthLabel.Text = character == null ? "" : Math.Round(displayedHealth, MidpointRounding.AwayFromZero).ToString(System.Globalization.CultureInfo.InvariantCulture);
		if (Godot.Object.IsInstanceValid(healthBar))
		{
			healthBar.MaxValue = Math.Max(1, character?.EnemySettings?.Health ?? initialMaxHealth);
			healthBar.Value = displayedHealth;
		}
	}

	private void UpdateHealthAnimation(float delta)
	{
		int health = Math.Max(0, character?.health ?? 0);
		if (health != targetHealth)
		{
			healthAnimationStart = displayedHealth;
			targetHealth = health;
			healthAnimationElapsed = 0;
		}
		if (HealthAnimationDuration <= 0 || character == null)
		{
			displayedHealth = targetHealth;
			return;
		}
		healthAnimationElapsed = Math.Min(HealthAnimationDuration, healthAnimationElapsed + Math.Max(0, delta));
		double progress = healthAnimationElapsed / HealthAnimationDuration;
		displayedHealth = healthAnimationStart + (targetHealth - healthAnimationStart) * progress;
	}
	private T Resolve<T>(NodePath path) where T : Node
		=> path == null || path.IsEmpty() ? null : GetNodeOrNull<Node>(path) as T;
}
