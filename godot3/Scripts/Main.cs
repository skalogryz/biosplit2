using Godot;
using System;
using Biosplit.Game;

public class Main : Node2D
{
	[Export] public bool DamageFlashEnabled = true;
	[Export] public int MaxHealth = 100;
	[Export] public int MaxRage = 100;
	internal decimal MaxStamina { get; set; } = 100m;
	internal decimal StaminaGrow { get; set; } = 2m;
	internal decimal Stamina => game?.Stamina ?? 0m;
	internal decimal DodgeStamina { get; set; } = 10m;
	[Export] public int DodgeTimeMs = 3000;
	[Export] public int RagePerPunch = 10;
	[Export] public int ShotCost = 30;
	[Export] public int PunchCost = 0;
	internal decimal PunchStaminaCost { get; set; } = 3m;
	internal decimal ShotStaminaCost { get; set; } = 0m;
	[Export] public int PunchDamage = 12;
	[Export] public int ShotDamage = 40;
	[Export] public int PunchCooldownMs = 260;
	[Export] public int ShotCooldownMs = 550;
	[Export] public float ScrollPerPunch = 32;
	[Export] public int EnemyDamage = 15;
	[Export] public int EnemyHealth = 80;
	[Export] public float EnemyAttackInterval = 2.4f;
	[Export] public float EnemyWarningTime = 0.8f;

		// Drag the desired TouchScreenButton from the scene tree into each field.
	[Export] public NodePath PunchButtonPath = new NodePath("");
	[Export] public NodePath ShootButtonPath = new NodePath("");
	[Export] public NodePath DodgeButtonPath = new NodePath("");
	[Export] public NodePath BlockButtonPath = new NodePath("");
	[Export] public NodePath InventoryButtonPath = new NodePath("");
	[Export] public NodePath HealthLabelPath = new NodePath("");
	[Export] public NodePath RageLabelPath = new NodePath("");
	[Export] public NodePath StaminaLabelPath = new NodePath("");
		[Export] public NodePath HealthBarPath = new NodePath("");
	[Export] public NodePath RageBarPath = new NodePath("");
	[Export] public NodePath RageIndicatorPath = new NodePath("");
	[Export] public int RagePointStep = 10;
	[Export] public NodePath EnemyBarPath = new NodePath("");
	[Export] public NodePath StatusLabelPath = new NodePath("");
	[Export] public NodePath EnemyStatusLabelPath = new NodePath("");
	[Export] public NodePath InventoryLabelPath = new NodePath("");
	private GameSession game;
	public GameSession Session => game;
	private ParallaxBackground background;
	private ParallaxLayer panorama;
	private AnimatedSprite player, enemy;
	private Line2D shotTrail;
	private Vector2 playerOrigin, enemyOrigin, backgroundOrigin;
	private Color playerColor, enemyColor;
	private string playerAction = "attack";
	private Label healthLabel, rageLabel, staminaLabel, status, enemyStatus, inventoryText;
	private ProgressBar healthBar, rageBar, enemyBar;
	private RageIndicator rageIndicator;
	private TouchScreenButton[] actions = new TouchScreenButton[5];
	private InventoryScreen inventory;
	private TouchActionButton heal;
	// Presentation timers only. Combat timing belongs to GameSession.
	private float scroll, targetScroll, pose, enemyPose, flash, enemyFlash, enemyHurt;
	private bool blockTouchHeld, blockKeyHeld, punchTouchHeld, punchKeyHeld;
	private string message = "Ударьте врага, чтобы накопить ярость.";

	public override void _Ready()
	{
		var settings = CreateSettings();
		GameConfiguration.Load(System.IO.Path.GetDirectoryName(OS.GetExecutablePath()), settings, text => GD.PushWarning(text));
		ApplySettings(settings);
		game = new GameSession(settings);
		MaxHealth = game.MaxHealth;
		MaxRage = game.MaxRage;
		RagePointStep = game.RagePointStep;

		background = GetNode<ParallaxBackground>("Background");
		panorama = GetNode<ParallaxLayer>("Background/Panorama");
		backgroundOrigin = background.ScrollOffset;
		player = GetNode<AnimatedSprite>("Combatants/Player");
		enemy = GetNode<AnimatedSprite>("Combatants/Enemy");
		shotTrail = GetNode<Line2D>("Combatants/ShotTrail");
		playerOrigin = player.Position;
		enemyOrigin = enemy.Position;
		playerColor = player.Modulate;
		enemyColor = enemy.Modulate;
		healthLabel = Optional<Label>(HealthLabelPath);
		rageLabel = Optional<Label>(RageLabelPath);
		staminaLabel = Optional<Label>(StaminaLabelPath);
		status = Optional<Label>(StatusLabelPath);
		enemyStatus = Optional<Label>(EnemyStatusLabelPath);
		healthBar = Optional<ProgressBar>(HealthBarPath);
		rageBar = Optional<ProgressBar>(RageBarPath);
		rageIndicator = Optional<RageIndicator>(RageIndicatorPath);
		enemyBar = Optional<ProgressBar>(EnemyBarPath);
		actions = new[]
		{
			BindActionButton(PunchButtonPath, nameof(PressPunch), nameof(PunchButtonPath)),
			BindActionButton(ShootButtonPath, nameof(Shoot), nameof(ShootButtonPath)),
			BindActionButton(DodgeButtonPath, nameof(Dodge), nameof(DodgeButtonPath)),
			BindActionButton(BlockButtonPath, nameof(Block), nameof(BlockButtonPath)),
			BindActionButton(InventoryButtonPath, nameof(ToggleInventory), nameof(InventoryButtonPath))
		};
		if (actions[0] != null && !actions[0].IsConnected("released", this, nameof(ReleasePunch)))
            actions[0].Connect("released", this, nameof(ReleasePunch));
        if (actions[3] != null && !actions[3].IsConnected("released", this, nameof(ReleaseBlock)))
			actions[3].Connect("released", this, nameof(ReleaseBlock));
		inventory = GetNode<InventoryScreen>("UI/HUD/Inventory");
		if (!inventory.IsConnected(nameof(InventoryScreen.HealRequested), this, nameof(UseKit)))
			inventory.Connect(nameof(InventoryScreen.HealRequested), this, nameof(UseKit));
		if (!inventory.IsConnected(nameof(InventoryScreen.Closed), this, nameof(OnInventoryClosed)))
			inventory.Connect(nameof(InventoryScreen.Closed), this, nameof(OnInventoryClosed));
		inventoryText = Optional<Label>(InventoryLabelPath);
		heal = inventory.GetNode<TouchActionButton>("HealButton/TouchButton");
		inventory.Visible = false;
		game.Changed += OnGameChanged;
		Render();
	}

	public override void _ExitTree()
	{
		if (game != null) game.Changed -= OnGameChanged;
	}

	public override void _Process(float delta)
	{
		if (!game.Paused)
		{
			pose = Mathf.Max(0, pose - delta);
			enemyPose = Mathf.Max(0, enemyPose - delta);
			enemyHurt = Mathf.Max(0, enemyHurt - delta);
			flash = Mathf.Max(0, flash - delta);
			enemyFlash = Mathf.Max(0, enemyFlash - delta);
		}
		game.Tick((decimal)delta);
        // Input intent only; GameSession checks cooldown, resources and combat state.
        if (punchTouchHeld || punchKeyHeld) game.Punch();
		if (!game.Paused) scroll = Mathf.Lerp(scroll, targetScroll, Mathf.Min(1, delta * 12));
		float width = panorama.MotionMirroring.x;
		if (width > 0 && scroll >= width) { scroll -= width; targetScroll -= width; }
		Render();
	}

	private void Render()
	{
		background.ScrollOffset = backgroundOrigin + new Vector2(-scroll, 0);
		player.Position = playerOrigin + new Vector2(game.Dodging ? -90 : pose > 0 ? 22 : 0, 0);
		enemy.Position = enemyOrigin + new Vector2(enemyPose > 0 ? -25 : 0, 0);
		player.Modulate = DamageFlashEnabled && flash > 0 ? new Color("ff7777") : playerColor;
		SetAnimation(player, game.GameOver ? "defeat" : game.Blocking ? "block" : pose > 0 ? playerAction : game.Dodging ? "dodge" : flash > 0 ? "hurt" : "idle");
		enemy.Modulate = DamageFlashEnabled && enemyFlash > 0 ? new Color("ff7777") : enemyColor;
		SetAnimation(enemy, game.EnemyHealth <= 0 ? "defeat" : enemyHurt > 0 ? "hurt" : enemyPose > 0 ? "attack" : "idle");
		player.Playing = !game.Paused;
		enemy.Playing = !game.Paused;
		shotTrail.Visible = pose > 0 && playerAction == "shoot";
		if (shotTrail.Visible)
		{
			shotTrail.SetPointPosition(0, player.Position + new Vector2(45, -25));
			shotTrail.SetPointPosition(1, enemy.Position + new Vector2(-40, -25));
		}
		inventory.Visible = game.InventoryOpen;
		Refresh();
	}

	private void OnGameChanged(object sender, GameEvent action)
	{
		switch (action.Kind)
		{
			case GameEventKind.Punch:
				targetScroll += Mathf.Max(0, ScrollPerPunch);
				StartPlayerAnimation("attack");
				pose = AnimationDuration(player, "attack", 0.20f);
				message = "Удар! +" + game.RagePerPunch + " ярости.";
				break;
			case GameEventKind.Shot:
				StartPlayerAnimation("shoot");
				pose = AnimationDuration(player, "shoot", 0.23f);
				message = "Выстрел! −" + game.ShotCost + " ярости.";
				break;
			case GameEventKind.Dodge:
				pose = flash = 0;
				SetAnimation(player, "dodge");
				message = "Отскок: атаки врага не причиняют урона.";
				break;
			case GameEventKind.BlockStarted:
				pose = flash = 0;
				SetAnimation(player, "block");
				message = "Защита: удерживайте кнопку блока.";
				break;
			case GameEventKind.EnemyHurt:
				enemyFlash = 0.18f;
				enemyPose = 0;
				if (game.EnemyHealth > 0 && enemy.Frames.HasAnimation("hurt"))
				{
					enemyHurt = AnimationDuration(enemy, "hurt", 0.18f);
					enemy.Play("hurt");
					enemy.Frame = 0;
				}
				break;
			case GameEventKind.EnemyAttacked:
				if (enemyHurt <= 0) { enemy.Play("attack"); enemy.Frame = 0; }
				enemyPose = AnimationDuration(enemy, "attack", 0.25f);
				if (action.Dodged) message = "Уворот: атака прошла мимо!";
				else if (action.Blocked) message = "Блок: атака отражена!";
				else { flash = 0.18f; message = "Враг нанёс " + action.Damage + " урона."; }
				break;
			case GameEventKind.EnemySpawned:
				enemyHurt = enemyPose = enemyFlash = 0;
				enemy.Modulate = enemyColor;
				enemy.Play("idle");
				enemy.Frame = 0;
				message = "Новый противник!";
				break;
			case GameEventKind.GameOver:
				message = "Вы проиграли. Нажмите R для новой игры.";
				break;
		}
	}

	public void Punch() { game.Punch(); Render(); }
    public void PressPunch() { punchTouchHeld = true; Punch(); }
    public void ReleasePunch() { punchTouchHeld = false; }
	public void Shoot() { game.Shoot(); Render(); }
	public void Dodge() { game.Dodge(); Render(); }
	public void Block() { blockTouchHeld = true; UpdateBlock(); }
	public void ReleaseBlock() { blockTouchHeld = false; UpdateBlock(); }
	private void UpdateBlock() { game.SetBlocking(blockTouchHeld || blockKeyHeld); Render(); }
	public void ToggleInventory() { game.SetInventoryOpen(!game.InventoryOpen); Render(); }
	public void OnInventoryClosed() { game.SetInventoryOpen(false); Render(); }
	public void UseKit() { game.UseKit(); Render(); }

	public override void _Input(InputEvent inputEvent)
	{
		// Receive release even if a Control consumes the key event.
        if (inputEvent is InputEventKey punchKey && punchKey.Scancode == (uint)KeyList.Slash && !punchKey.Pressed)
            punchKeyHeld = false;
		if (inputEvent is InputEventKey key && key.Scancode == (uint)KeyList.Z && !key.Pressed)
		{
			blockKeyHeld = false;
			UpdateBlock();
		}
	}

	public override void _UnhandledKeyInput(InputEventKey key)
	{
		if (!key.Pressed || key.Echo) return;
		switch ((KeyList)key.Scancode)
		{
			case KeyList.Slash: punchKeyHeld = true; Punch(); break;
			case KeyList.Apostrophe: Shoot(); break;
			case KeyList.A: Dodge(); break;
			case KeyList.Z: blockKeyHeld = true; UpdateBlock(); break;
			case KeyList.I: case KeyList.Escape: ToggleInventory(); break;
			case KeyList.R: GetTree().ReloadCurrentScene(); break;
		}
	}

	private static void SetAnimation(AnimatedSprite actor, string animation)
	{
		if (actor.Frames.HasAnimation(animation) && actor.Animation != animation) actor.Play(animation);
	}

	private static float AnimationDuration(AnimatedSprite actor, string animation, float minimum)
	{
		if (!actor.Frames.HasAnimation(animation)) return minimum;
		float speed = actor.Frames.GetAnimationSpeed(animation) * actor.SpeedScale;
		return speed > 0 ? Mathf.Max(minimum, actor.Frames.GetFrameCount(animation) / speed) : minimum;
	}

	private void StartPlayerAnimation(string animation)
	{
		playerAction = animation;
		player.Frame = 0;
		player.Play(animation);
	}

	private TouchScreenButton BindActionButton(NodePath path, string method, string field)
	{
		var button = Optional<TouchScreenButton>(path);
		if (button == null) { GD.PushWarning("Main: assign a TouchScreenButton to " + field + " in the inspector."); return null; }
		button.SetProcessInput(true);
		if (!button.IsConnected("pressed", this, method)) button.Connect("pressed", this, method);
		return button;
	}

	private static void SetButtonEnabled(TouchScreenButton button, bool enabled)
	{
		if (!Godot.Object.IsInstanceValid(button)) return;
		if (button is TouchActionButton custom) custom.Disabled = !enabled;
		else button.Modulate = enabled ? Colors.White : new Color(0.45f, 0.45f, 0.45f, 1);
	}

	private T Optional<T>(NodePath path) where T : Node
		=> path == null || path.IsEmpty() ? null : GetNodeOrNull<Node>(path) as T;

	private void Refresh()
	{
		if (Godot.Object.IsInstanceValid(healthLabel)) healthLabel.Text = $"{game.Health}/{game.MaxHealth}";
		if (Godot.Object.IsInstanceValid(rageIndicator)) rageIndicator.UpdateRage(game.Rage, RagePointStep);
		if (Godot.Object.IsInstanceValid(rageLabel)) rageLabel.Text = $"{game.Rage}/{game.MaxRage}";
		if (Godot.Object.IsInstanceValid(staminaLabel)) staminaLabel.Text = game.Stamina.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "/" + game.MaxStamina.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
		if (Godot.Object.IsInstanceValid(enemyStatus))
		{
			enemyStatus.Text = game.EnemyHealth <= 0 ? "ПОБЕДА! +10 монет" : "ВРАГ " + game.Wave + "  •  " + game.EnemyHealth + "/" + game.EnemyMaxHealth + (game.EnemyWarning ? "   ⚠ АТАКУЕТ!" : "");
			enemyStatus.Modulate = game.EnemyWarning ? new Color("ff8a5b") : Colors.White;
		}
		if (Godot.Object.IsInstanceValid(status)) status.Text = message;
		if (Godot.Object.IsInstanceValid(healthBar)) { healthBar.MaxValue = game.MaxHealth; healthBar.Value = game.Health; }
		if (Godot.Object.IsInstanceValid(rageBar)) { rageBar.MaxValue = game.MaxRage; rageBar.Value = game.Rage; }
		if (Godot.Object.IsInstanceValid(enemyBar)) { enemyBar.MaxValue = game.EnemyMaxHealth; enemyBar.Value = game.EnemyHealth; }
		SetButtonEnabled(actions[0], game.CanPunch);
		SetButtonEnabled(actions[1], game.CanShoot);
		if (actions[1] is TouchActionButton shootButton) shootButton.Text = "ВЫСТРЕЛ [']  " + game.ShotCost + " ЯР";
		SetButtonEnabled(actions[2], game.CanDodge);
		SetButtonEnabled(actions[3], !game.GameOver);
		SetButtonEnabled(actions[4], !game.GameOver);
		if (Godot.Object.IsInstanceValid(inventoryText)) inventoryText.Text = "ИНВЕНТАРЬ\n\nМонеты: " + game.Coins + "\nАптечки: " + game.Kits + "\nБой приостановлен";
		heal.Disabled = !game.CanHeal;
	}

	private GameSettings CreateSettings() => new GameSettings
	{
		MaxHealth = MaxHealth,
		MaxRage = MaxRage,
		MaxStamina = MaxStamina,
		StaminaGrow = StaminaGrow,
		DodgeStamina = DodgeStamina,
		DodgeTimeMs = DodgeTimeMs,
		RagePerPunch = RagePerPunch,
		PunchCost = PunchCost,
		ShotCost = ShotCost,
		PunchStaminaCost = PunchStaminaCost,
		ShotStaminaCost = ShotStaminaCost,
		PunchDamage = PunchDamage,
		ShotDamage = ShotDamage,
		PunchCooldownMs = PunchCooldownMs,
		ShotCooldownMs = ShotCooldownMs,
		EnemyDamage = EnemyDamage,
		EnemyHealth = EnemyHealth,
		EnemyAttackInterval = (decimal)EnemyAttackInterval,
		EnemyWarningTime = (decimal)EnemyWarningTime
	};

	private void ApplySettings(GameSettings settings)
	{
		MaxHealth = settings.MaxHealth;
		MaxRage = settings.MaxRage;
		MaxStamina = settings.MaxStamina;
		StaminaGrow = settings.StaminaGrow;
		DodgeStamina = settings.DodgeStamina;
		DodgeTimeMs = settings.DodgeTimeMs;
		RagePerPunch = settings.RagePerPunch;
		PunchCost = settings.PunchCost;
		ShotCost = settings.ShotCost;
		PunchStaminaCost = settings.PunchStaminaCost;
		ShotStaminaCost = settings.ShotStaminaCost;
		PunchDamage = settings.PunchDamage;
		ShotDamage = settings.ShotDamage;
		PunchCooldownMs = settings.PunchCooldownMs;
		ShotCooldownMs = settings.ShotCooldownMs;
		EnemyDamage = settings.EnemyDamage;
		EnemyHealth = settings.EnemyHealth;
		EnemyAttackInterval = (float)settings.EnemyAttackInterval;
		EnemyWarningTime = (float)settings.EnemyWarningTime;
	}
}
