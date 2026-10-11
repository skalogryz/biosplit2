using Godot;
using System;
using Biosplit.Game;
using Biosplit2.Scripts;

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
	[Export] public float ScrollPerPunch = 32;
	[Export] public int EnemyDamage = 15;
	[Export] public int EnemyHealth = 80;
	[Export] public float EnemyAttackInterval = 2.4f;
	[Export] public float EnemyWarningTime = 0.8f;

		// Drag the desired TouchScreenButton from the scene tree into each field.
	[Export] public NodePath PunchButtonPath = new NodePath("");
	[Export] public NodePath ShootButtonPath = new NodePath("");
	[Export] public NodePath AimedShotButtonPath = new NodePath("");
	[Export] public NodePath DodgeButtonPath = new NodePath("");
	[Export] public NodePath BlockButtonPath = new NodePath("");
	[Export] public NodePath InventoryButtonPath = new NodePath("");
	[Export] public NodePath SettingsButtonPath = new NodePath("");
	[Export] public NodePath ConsoleScreenPath = new NodePath("");
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
	[Export] public NodePath[] Backgrounds;
	private ParallaxBackground[] backgrounds;
	[Export] public NodePath[] MainCharStatus;
	private CharacterStatus[] mainCharStats;

	private GameSession game;
	public GameSession Session => game;
	//private ParallaxBackground background;
	//private ParallaxLayer panorama;
	private AnimatedSprite player, enemy;
	private StaminaStatus playerStaminaStatus;
	private sealed class EnemySlot
	{
		public AnimatedSprite Sprite;
		public CharacterStatus Status;
		public Character Character;
		public Vector2 Origin;
		public Color Color;
		public int ZIndex;
		public float Hurt, Flash;
	}
	private readonly System.Collections.Generic.List<EnemySlot> enemySlots = new System.Collections.Generic.List<EnemySlot>();
	private Line2D shotTrail;
	private AimedTargetSelection aimedSelection;
	private Character shotTarget;
	private Vector2 playerOrigin, enemyOrigin, backgroundOrigin;
	private Color playerColor, enemyColor;
	private int combatBackZ, combatFrontZ;
	private string playerAction = "attack";
	private bool shootingPose;
	private Label healthLabel, rageLabel, staminaLabel, status, enemyStatus, inventoryText;
	private ProgressBar healthBar, rageBar, enemyBar;
	private RageIndicator rageIndicator;
	private TouchScreenButton[] actions = new TouchScreenButton[6];
	private InventoryScreen inventory;
	private ConsoleScreen console;
	private DeveloperSettingsScreen developerSettings;
	private TouchActionButton heal;
	// Presentation timers only. Combat timing belongs to GameSession.
	private float scroll, targetScroll, pose, enemyPose, flash, enemyFlash, enemyHurt;
	private bool blockTouchHeld, blockKeyHeld, punchTouchHeld, punchKeyHeld;
	private WeaponItem meleeAttackWeapon;
	private string enemyAction = "attack";
	private System.Collections.Generic.Dictionary<string, SpriteFrames> enemyAnimations;
	private SpriteFrames enemySceneFrames;
	private Character defeatedEnemy;
	private bool defeatAnimationFinished, rotatingEnemies;

	private sealed class EnemyRotation
	{
		public AnimatedSprite Sprite;
		public Vector2 Start, Target;
	}
	private readonly System.Collections.Generic.List<EnemyRotation> enemyRotations = new System.Collections.Generic.List<EnemyRotation>();
	private string message = "Ударьте врага, чтобы накопить ярость.";

	public override void _Ready()
	{
		var settings = CreateSettings();
		GameConfiguration.Load(System.IO.Path.GetDirectoryName(OS.GetExecutablePath()), settings, text => GD.PushWarning(text));
		settings = settings.NormalizedCopy();
		player = GetNode<AnimatedSprite>("Combatants/Player");
		enemy = GetNode<AnimatedSprite>("Combatants/Enemy");
		foreach (string name in new[] { "Enemy", "Enemy2", "Enemy3", "Enemy4" })
		{
			var sprite = GetNodeOrNull<AnimatedSprite>("Combatants/" + name);
			if (sprite != null) sprite.Connect("animation_finished", this, nameof(OnEnemyAnimationFinished), new Godot.Collections.Array { sprite });
			if (sprite != null) enemySlots.Add(new EnemySlot { Sprite = sprite, Status = sprite.GetNodeOrNull<CharacterStatus>("CharacterStatus"), Origin = sprite.Position, Color = sprite.Modulate, ZIndex = sprite.ZIndex });
		}
		WeaponAnimationNormalizer.Normalize(settings, player, warning: text => GD.PushWarning(text));
		enemySceneFrames = enemy.Frames;
		enemyAnimations = EnemyAnimationLibrary.Prepare(settings, enemy, warning: text => GD.PushWarning(text));
		ApplySettings(settings);
		game = new GameSession(settings);
		playerStaminaStatus = player.GetNodeOrNull<StaminaStatus>("StaminaStatus");
		playerStaminaStatus?.Bind(game.Player, player);
		SyncEnemySprites();
		MaxHealth = game.MaxHealth;
		MaxRage = game.MaxRage;
		RagePointStep = game.RagePointStep;

		//background = GetNode<ParallaxBackground>("Background");
		//panorama = GetNode<ParallaxLayer>("Background/Panorama");
		//backgroundOrigin = background.ScrollOffset;
		backgrounds = this.ResolvePaths<ParallaxBackground>(Backgrounds);
		if (backgrounds.Length > 0)
			backgroundOrigin = backgrounds[0].ScrollOffset;
		mainCharStats = this.ResolvePaths<CharacterStatus>(MainCharStatus);
		foreach(var st in mainCharStats)
		{
			st.Character = game.Player;
		}

		enemy = GetNode<AnimatedSprite>("Combatants/Enemy");
		shotTrail = GetNode<Line2D>("Combatants/ShotTrail");
		playerOrigin = player.Position;
		enemyOrigin = enemy.Position;
		playerColor = player.Modulate;
		enemyColor = enemy.Modulate;
		combatBackZ = Math.Min(player.ZIndex, enemy.ZIndex);
		combatFrontZ = Math.Min(4096, Math.Max(player.ZIndex, enemy.ZIndex) + 1);
		combatBackZ = Math.Min(combatBackZ, combatFrontZ - 1);
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
			BindActionButton(InventoryButtonPath, nameof(ToggleInventory), nameof(InventoryButtonPath)),
			BindActionButton(AimedShotButtonPath, nameof(AimedShot), nameof(AimedShotButtonPath))
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
		console = Optional<ConsoleScreen>(ConsoleScreenPath);
		if (Godot.Object.IsInstanceValid(console)) console.Hide();
		developerSettings = GetNode<DeveloperSettingsScreen>("UI/HUD/DeveloperSettings");
		developerSettings.Connect("visibility_changed", this, nameof(OnDeveloperSettingsVisibilityChanged));
		OnDeveloperSettingsVisibilityChanged();
		var settingsButton = Optional<TouchScreenButton>(SettingsButtonPath);
		if (Godot.Object.IsInstanceValid(settingsButton) && !settingsButton.IsConnected("pressed", this, nameof(ToggleDeveloperSettings)))
			settingsButton.Connect("pressed", this, nameof(ToggleDeveloperSettings));
		aimedSelection = GetNode<AimedTargetSelection>("UI/HUD/AimedTargetSelection");
		aimedSelection.Selected += ShootSelectedTarget;
		aimedSelection.Cancelled += CancelAimedShot;
		game.Changed += OnGameChanged;
		Render();
	}

	public override void _ExitTree()
	{
		if (game != null) game.Changed -= OnGameChanged;
	}

	public override void _Process(float delta)
	{
		UpdateMappedInput();
		if (!game.Paused)
		{
			foreach (var slot in enemySlots)
			{
				slot.Hurt = Mathf.Max(0, slot.Hurt - delta);
				slot.Flash = Mathf.Max(0, slot.Flash - delta);
			}
			pose = Mathf.Max(0, pose - delta);
			enemyPose = Mathf.Max(0, enemyPose - delta);
			enemyHurt = Mathf.Max(0, enemyHurt - delta);
			flash = Mathf.Max(0, flash - delta);
			enemyFlash = Mathf.Max(0, enemyFlash - delta);
		}
		if (aimedSelection.IsSelecting && (game.Paused || game.EnemyTransitionPending || game.State == GameState.EnemyRotation)) CancelAimedShot();
		game.Tick((decimal)delta);
		if (!game.Paused) AdvanceEnemyRotation(delta);
		if (punchTouchHeld || punchKeyHeld) TryMeleeAttack();
		if (developerSettings != null && !developerSettings.ScrollOnHitEnabled) targetScroll = scroll;
		if (!game.Paused) 
			scroll = Mathf.Lerp(scroll, targetScroll, Mathf.Min(1, delta * 12));
		//float width = panorama.MotionMirroring.x;
		//if (width > 0 && scroll >= width) { scroll -= width; targetScroll -= width; }
		Render();
	}

	private void Render()
	{
		SyncEnemySprites();
		//background.ScrollOffset = backgroundOrigin + new Vector2(-scroll, 0);
		foreach (var b in backgrounds)
		{
			b.ScrollOffset = backgroundOrigin + new Vector2(-scroll, 0);
		}
		player.Position = new Vector2((float)game.HeroPos + (game.Dodging ? -90 : 0), playerOrigin.y);
		if (!rotatingEnemies) enemy.Position = new Vector2((float)game.EnemyMeleePos, enemyOrigin.y);
		player.Modulate = DamageFlashEnabled && flash > 0 ? new Color("ff7777") : playerColor;
		SetAnimation(player, game.GameOver ? "defeat" : game.Blocking ? "block" : pose > 0 ? playerAction : game.Dodging ? "dodge" : flash > 0 ? "hurt" : "idle");
		enemy.Modulate = DamageFlashEnabled && enemyFlash > 0 ? new Color("ff7777") : enemyColor;
		SetAnimation(enemy, game.EnemyHealth <= 0 ? "defeat" : enemyHurt > 0 ? "hurt" : enemyPose > 0 ? enemyAction : "idle");
		player.Playing = !game.Paused;
		enemy.Playing = !game.Paused;
		shotTrail.Visible = pose > 0 && shootingPose;
		if (shotTrail.Visible)
		{
			shotTrail.SetPointPosition(0, player.Position + new Vector2(45, -25));
			var targetSprite = enemy;
			foreach (var slot in enemySlots)
				if (slot.Character == shotTarget) { targetSprite = slot.Sprite; break; }
			shotTrail.SetPointPosition(1, targetSprite.Position + new Vector2(-40, -25));
		}
		playerStaminaStatus?.UpdateView();
		foreach (var slot in enemySlots) slot.Status?.UpdateView();
		inventory.Visible = game.InventoryOpen;
		Refresh();
	}

	private void SyncEnemySprites()
	{
		if (rotatingEnemies) return;
		var enemies = new System.Collections.Generic.List<Character>();
		foreach (var character in game.Characters)
			if (character.EnemySettings != null && (!character.isDefeated || character == defeatedEnemy)) enemies.Add(character);
		for (int i = 0; i < enemySlots.Count; i++)
		{
			var slot = enemySlots[i];
			var character = i < enemies.Count ? enemies[i] : null;
			slot.Sprite.Visible = character != null;
			slot.Status?.Bind(character, slot.Sprite);
			if (character == null) { slot.Character = null; continue; }
			if (slot.Character != character)
			{
				slot.Character = character;
				slot.Hurt = slot.Flash = 0;
				ApplyEnemyAnimations(slot.Sprite, character);
				slot.Sprite.Frame = 0;
			}
			if (character == game.Enemy)
			{
				enemy = slot.Sprite;
				enemyOrigin = slot.Origin;
				enemyColor = slot.Color;
			}
			else
			{
				slot.Sprite.Position = slot.Origin;
				slot.Sprite.Modulate = DamageFlashEnabled && slot.Flash > 0 ? new Color("ff7777") : slot.Color;
				slot.Sprite.ZIndex = slot.ZIndex;
				SetAnimation(slot.Sprite, character.isDefeated ? "defeat" : slot.Hurt > 0 ? "hurt" : "idle");
				slot.Sprite.Playing = !game.Paused;
			}
		}
	}

	public void OnEnemyAnimationFinished(AnimatedSprite actor)
	{
		if (defeatedEnemy != null && IsDefeatedSprite(actor) && actor.Animation == "defeat")
			defeatAnimationFinished = true;
	}

	private void AdvanceEnemyRotation(float delta)
	{
		if (defeatedEnemy == null) return;
		if (!rotatingEnemies)
		{
			if (!defeatAnimationFinished || game.IsAttacking || game.IsSeparating) return;
			enemyRotations.Clear();
			int destination = 0;
			foreach (var slot in enemySlots)
			{
				if (slot.Character == null || slot.Character.isDefeated) continue;
				var destinationSlot = enemySlots[destination++];
				var target = destinationSlot == slot ? slot.Sprite.Position : destinationSlot.Origin;
				if (destination == 1 && destinationSlot != slot) target.x = (float)game.EnemyInitialPos;
				enemyRotations.Add(new EnemyRotation { Sprite = slot.Sprite, Start = slot.Sprite.Position, Target = target });
				SetAnimation(slot.Sprite, "idle");
			}
			if (!game.BeginEnemyRotation()) return;
			foreach (var slot in enemySlots) if (slot.Character == defeatedEnemy) slot.Sprite.Visible = false;
			rotatingEnemies = true;
			return;
		}
		float progress = (float)game.EnemyRotationProgress;
		foreach (var movement in enemyRotations)
			movement.Sprite.Position = movement.Start.LinearInterpolate(movement.Target, progress);
		if (game.State == GameState.EnemyRotation) return;
		rotatingEnemies = false;
		defeatedEnemy = null;
		defeatAnimationFinished = false;
		enemyRotations.Clear();

		SyncEnemySprites();
	}
	private void ApplyEnemyAnimations(AnimatedSprite actor, Character character)
	{
		if (!enemyAnimations.TryGetValue(character.Name, out var frames))
		{
			actor.Frames = Godot.Object.IsInstanceValid(enemySceneFrames) ? (SpriteFrames)enemySceneFrames.Duplicate() : new SpriteFrames();
			WeaponAnimationNormalizer.NormalizeEnemyAnimations(character.EnemySettings, actor, warning: text => GD.PushWarning(text));
			foreach (var weapon in new[] { character.Inventory.Weapon1, character.Inventory.Weapon2 })
				if (weapon != null) WeaponAnimationNormalizer.Normalize(new[] { weapon.Definition }, actor, warning: text => GD.PushWarning(text));
			frames = actor.Frames;
			enemyAnimations[character.Name] = frames;
		}
		actor.Frames = frames;
	}
	private void OnGameChanged(object sender, GameEvent action)
	{
		if (action.Kind == GameEventKind.AttackStarted)
		{
			bool enemyAttacks = action.Weapon != null &&
				(action.Weapon == game.Enemy.Inventory.Weapon1 || action.Weapon == game.Enemy.Inventory.Weapon2);
			enemy.ZIndex = enemyAttacks ? combatFrontZ : combatBackZ;
			player.ZIndex = enemyAttacks ? combatBackZ : combatFrontZ;
		}
		if (action.Kind == GameEventKind.AttackCompleted && action.Weapon == meleeAttackWeapon)
			meleeAttackWeapon = null;
		switch (action.Kind)
		{
			case GameEventKind.ApproachStarted:
				message = (action.Attacker == game.Enemy ? "Враг" : "Герой") + ": началось сближение.";
				break;
			case GameEventKind.SeparationStarted:
				message = (action.Attacker == game.Enemy ? "Враг" : "Герой") + ": начался отход.";
				break;
			case GameEventKind.SeparationCompleted:
				message = (action.Attacker == game.Enemy ? "Враг" : "Герой") + ": закончился отход.";
				break;
			case GameEventKind.StrikeStarted:
				if (action.Attacker == game.Enemy)
				{
					enemyAction = !string.IsNullOrEmpty(action.StrikeName) && enemy.Frames.HasAnimation(action.StrikeName)
						? action.StrikeName : "attack";
					enemyHurt = 0;
					enemy.Play(enemyAction);
					enemy.Frame = 0;
					enemyPose = Mathf.Max(AnimationDuration(enemy, enemyAction, 0.25f), (float)game.AttackRemaining);
				}
				message = (action.Attacker == game.Enemy ? "Враг" : "Герой") + ": начался страйк "
					+ (string.IsNullOrEmpty(action.StrikeName) ? "#" + action.StrikeIndex : action.StrikeName)
					+ " (" + action.Weapon.Definition.Name + ", индекс " + action.StrikeIndex + ").";
				break;
			case GameEventKind.Punch:
				if (developerSettings == null || developerSettings.ScrollOnHitEnabled) targetScroll += Mathf.Max(0, ScrollPerPunch);
				shootingPose = false;
				StartPlayerAnimation(StrikeAnimation(action, "attack"));
				pose = Mathf.Max(AnimationDuration(player, playerAction, 0.20f), (float)game.AttackRemaining);
				message = "Удар! +" + Math.Max(0, action.Weapon?.Definition.RageBonus ?? 0) + " ярости.";
				break;
			case GameEventKind.Shot:
				shootingPose = true;
				shotTarget = action.Target;
				StartPlayerAnimation(StrikeAnimation(action, "shoot"));
				pose = Mathf.Max(AnimationDuration(player, playerAction, 0.23f), (float)game.AttackRemaining);
				message = "Выстрел! −" + Math.Max(0, action.Weapon?.Definition.RageCost ?? 0) + " ярости.";
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
			case GameEventKind.EnemyDefeated:
				CancelAimedShot();
				defeatedEnemy = action.Target ?? game.Enemy;
				var defeatedSlot = FindEnemySlot(defeatedEnemy);
				var defeatedSprite = defeatedSlot?.Sprite;
				defeatAnimationFinished = defeatedSprite == null || !defeatedSprite.Frames.HasAnimation("defeat")
					|| defeatedSprite.Frames.GetFrameCount("defeat") == 0
					|| defeatedSprite.Frames.GetAnimationSpeed("defeat") <= 0 || defeatedSprite.SpeedScale <= 0;
				if (defeatedSlot != null) defeatedSlot.Hurt = defeatedSlot.Flash = 0;
				if (defeatedEnemy == game.Enemy) enemyHurt = enemyPose = 0;
				if (!defeatAnimationFinished) { defeatedSprite.Play("defeat"); defeatedSprite.Frame = 0; }
				return;
			case GameEventKind.EnemyHurt:
				var hurtCharacter = action.Target ?? game.Enemy;
				var hurtSlot = FindEnemySlot(hurtCharacter);
				if (hurtSlot != null)
				{
					hurtSlot.Flash = 0.18f;
					hurtSlot.Hurt = AnimationDuration(hurtSlot.Sprite, "hurt", 0.18f);
					if (!hurtCharacter.isDefeated && hurtSlot.Sprite.Frames.HasAnimation("hurt"))
					{
						hurtSlot.Sprite.Play("hurt");
						hurtSlot.Sprite.Frame = 0;
					}
					if (hurtCharacter == game.Enemy)
					{
						enemyFlash = hurtSlot.Flash;
						enemyHurt = hurtSlot.Hurt;
						enemyPose = 0;
					}
				}
				return;
			case GameEventKind.EnemyAttacked:
				if (action.Dodged) message = "Уворот: атака прошла мимо!";
				else if (action.Blocked) message = "Блок: атака отражена!";
				else { flash = 0.18f; message = "Враг нанёс " + action.Damage + " урона."; }
				break;
			case GameEventKind.EnemyTargetChanged:
			case GameEventKind.EnemySpawned:
				SyncEnemySprites();
				enemyAction = "attack";
				enemyHurt = enemyPose = enemyFlash = 0;
				enemy.Modulate = enemyColor;
				enemy.Play("idle");
				enemy.Frame = 0;
				message = "Новый противник!";
				break;
			case GameEventKind.AttackCancelled:
				if (!game.IsAttacking) pose = 0;
				message = game.IsAttacking ? "Атака отменена: текущий удар будет завершён." : "Атака прервана.";
				break;
			case GameEventKind.BattleCompleted:
				message = "Бой завершён: все противники побеждены.";
				break;
			case GameEventKind.GameOver:
				message = "Вы проиграли. Нажмите R для новой игры.";
				break;
			default: return;
		}
		if (Godot.Object.IsInstanceValid(console)) console.AppendMessage(message);
	}

	private WeaponItem FindReadyWeapon(WeaponType type)
	{
		var inventory = game.Player.Inventory;
		var first = inventory.Weapon1;
		if (first?.Definition.Type == type && game.CanStartAttack(first)) return first;
		var second = inventory.Weapon2;
		if (second?.Definition.Type == type && game.CanStartAttack(second)) return second;
		return null;
	}

	private void TryMeleeAttack()
	{
		if (game.IsAttacking || game.Paused || game.GameOver || aimedSelection.IsSelecting) return;
		var weapon = FindReadyWeapon(WeaponType.Melee);
		if (weapon == null) return;
		meleeAttackWeapon = weapon;
		if (game.StartAttack(weapon) != AttackResult.Success) meleeAttackWeapon = null;
	}

	public void Punch() { TryMeleeAttack(); Render(); }
	public void PressPunch() { punchTouchHeld = true; Punch(); }
	public void ReleasePunch()
	{
		punchTouchHeld = false;
		CancelReleasedMeleeAttack();
	}

	private void CancelReleasedMeleeAttack()
	{
		if (punchTouchHeld || punchKeyHeld) return;
		if (meleeAttackWeapon != null && game.ActiveWeapon == meleeAttackWeapon) game.CancelAttack(isInterrupt: false);
		Render();
	}
	public void Shoot()
	{
		var weapon = FindReadyWeapon(WeaponType.Firearm);
		if (weapon != null) game.StartAttack(weapon);
		Render();
	}
	public void Dodge() { game.Dodge(); Render(); }
	public void Block() { blockTouchHeld = true; UpdateBlock(); }
	public void ReleaseBlock() { blockTouchHeld = false; UpdateBlock(); }
	private void UpdateBlock() { game.SetBlocking(blockTouchHeld || blockKeyHeld); Render(); }
	public void ToggleInventory() { game.SetInventoryOpen(!game.InventoryOpen); Render(); }
	public void OnInventoryClosed() { game.SetInventoryOpen(false); Render(); }
	public void UseKit() { game.UseKit(); Render(); }
	public void OnDeveloperSettingsVisibilityChanged() => game.SetPaused(developerSettings.Visible);
	public void ToggleDeveloperSettings() => developerSettings?.Toggle();
	public void ToggleConsole()
	{
		if (Godot.Object.IsInstanceValid(console)) console.Visible = !console.Visible;
	}

	private void UpdateMappedInput()
	{
		if (Input.IsActionJustPressed("toggle_console")) ToggleConsole();
		var focus = GetNode<Control>("UI/HUD").GetFocusOwner();
		bool textFocused = focus is TextEdit || focus is LineEdit;
		bool settingsOpen = developerSettings != null && developerSettings.Visible;
		if (aimedSelection.IsSelecting)
		{
			if (Input.IsActionJustPressed("aimed_shot") || Input.IsActionJustPressed("ui_cancel")) CancelAimedShot();
			return;
		}
		bool gameplayInput = !textFocused && !settingsOpen && !game.Paused;

		bool meleeHeld = gameplayInput && Input.IsActionPressed("melee_attack");
		if (punchKeyHeld && !meleeHeld)
		{
			punchKeyHeld = false;
			CancelReleasedMeleeAttack();
		}
		else punchKeyHeld = meleeHeld;

		bool blockHeld = gameplayInput && Input.IsActionPressed("block");
		if (blockKeyHeld != blockHeld) { blockKeyHeld = blockHeld; UpdateBlock(); }
		if (!textFocused && !settingsOpen)
		{
			if (Input.IsActionJustPressed("toggle_inventory")) ToggleInventory();
			if (Input.IsActionJustPressed("restart_game")) { GetTree().ReloadCurrentScene(); return; }
		}
		if (!gameplayInput || game.Paused) return;
		if (Input.IsActionJustPressed("melee_attack")) Punch();
		if (Input.IsActionJustPressed("shoot")) Shoot();
		if (Input.IsActionJustPressed("aimed_shot")) AimedShot();
		if (Input.IsActionJustPressed("dodge")) Dodge();
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

	private string StrikeAnimation(GameEvent action, string fallback)
	{
		string name = game.CurrentStrike?.Name;
		return !string.IsNullOrEmpty(name) && player.Frames.HasAnimation(name) ? name : fallback;
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

	private EnemySlot FindEnemySlot(Character character)
	{
		foreach (var slot in enemySlots) if (slot.Character == character) return slot;
		return null;
	}
	private bool IsDefeatedSprite(AnimatedSprite actor) => FindEnemySlot(defeatedEnemy)?.Sprite == actor;

	public void AimedShot()
	{
		if (aimedSelection.IsSelecting) { CancelAimedShot(); return; }
		int living = 0;
		foreach (var character in game.Characters)
			if (character.EnemySettings != null && !character.isDefeated) living++;
		if (living == 1) { Shoot(); return; }
		if (living == 0 || FindReadyWeapon(WeaponType.Firearm) == null) return;
		var choices = new System.Collections.Generic.List<AimedTargetSelection.TargetArea>();
		foreach (var slot in enemySlots)
			if (slot.Character != null && !slot.Character.isDefeated && slot.Sprite.Visible)
				choices.Add(new AimedTargetSelection.TargetArea { Character = slot.Character, Sprite = slot.Sprite });
		aimedSelection.Open(choices);
		punchTouchHeld = punchKeyHeld = false;
		blockTouchHeld = blockKeyHeld = false;
		game.SetBlocking(false);
		Refresh();
	}
	private void CancelAimedShot()
	{
		if (aimedSelection == null) return;
		aimedSelection.Close();
		foreach (var button in actions)
			if (Godot.Object.IsInstanceValid(button)) button.SetProcessInput(true);
	}
	private void ShootSelectedTarget(Character target)
	{
		CancelAimedShot();
		var weapon = FindReadyWeapon(WeaponType.Firearm);
		if (weapon != null) game.StartAttack(weapon, target);
		Render();
	}

	private void Refresh()
	{
		if (Godot.Object.IsInstanceValid(healthLabel)) healthLabel.Text = $"{game.Health}/{game.MaxHealth}";
		if (Godot.Object.IsInstanceValid(rageIndicator)) rageIndicator.UpdateRage(game.Rage, RagePointStep);
		if (Godot.Object.IsInstanceValid(rageLabel)) rageLabel.Text = $"{game.Rage}/{game.MaxRage}";
		if (Godot.Object.IsInstanceValid(staminaLabel)) staminaLabel.Text = game.Stamina.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "/" + game.MaxStamina.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
		if (Godot.Object.IsInstanceValid(enemyStatus))
		{
			enemyStatus.Text = game.BattleFinished ? "ПОБЕДА! +10 монет" : "ВРАГ " + game.Wave + "  •  " + game.EnemyHealth + "/" + game.EnemyMaxHealth + (game.EnemyWarning ? "   ⚠ АТАКУЕТ!" : "");
			enemyStatus.Modulate = game.EnemyWarning ? new Color("ff8a5b") : Colors.White;
		}
		if (Godot.Object.IsInstanceValid(status)) status.Text = message;
		if (Godot.Object.IsInstanceValid(healthBar)) { healthBar.MaxValue = game.MaxHealth; healthBar.Value = game.Health; }
		if (Godot.Object.IsInstanceValid(rageBar)) { rageBar.MaxValue = game.MaxRage; rageBar.Value = game.Rage; }
		if (Godot.Object.IsInstanceValid(enemyBar)) { enemyBar.MaxValue = game.EnemyMaxHealth; enemyBar.Value = game.EnemyHealth; }
		SetButtonEnabled(actions[0], FindReadyWeapon(WeaponType.Melee) != null);
		var firearm = FindReadyWeapon(WeaponType.Firearm);
		SetButtonEnabled(actions[1], firearm != null);
		SetButtonEnabled(actions[5], firearm != null);
		if (actions[1] is TouchActionButton shootButton) shootButton.Text = "ВЫСТРЕЛ [']" + (firearm != null ? "  " + firearm.Definition.RageCost + " ЯР" : "");
		SetButtonEnabled(actions[2], game.CanDodge);
		SetButtonEnabled(actions[3], !game.GameOver);
		SetButtonEnabled(actions[4], !game.GameOver);
		if (Godot.Object.IsInstanceValid(inventoryText)) inventoryText.Text = "ИНВЕНТАРЬ\n\nМонеты: " + game.Coins + "\nАптечки: " + game.Kits + "\nБой приостановлен";
		heal.Disabled = !game.CanHeal;
		foreach (var button in actions)
			if (Godot.Object.IsInstanceValid(button)) button.SetProcessInput(!aimedSelection.IsSelecting);
	}

	private GameSettings CreateSettings() => new GameSettings
	{
		MaxHealth = MaxHealth,
		MaxRage = MaxRage,
		MaxStamina = MaxStamina,
		StaminaGrow = StaminaGrow,
		DodgeStamina = DodgeStamina,
		DodgeTimeMs = DodgeTimeMs,
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
		EnemyDamage = settings.EnemyDamage.Min;
		EnemyHealth = settings.EnemyHealth;
		EnemyAttackInterval = (float)settings.EnemyAttackInterval;
		EnemyWarningTime = (float)settings.EnemyWarningTime;
	}
}
