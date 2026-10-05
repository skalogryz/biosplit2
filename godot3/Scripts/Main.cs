using Godot;
using System;

public class Main : Node2D
{
	[Export] public bool DamageFlashEnabled = true;
	[Export] public int MaxHealth = 100;
	[Export] public int MaxRage = 100;
	internal decimal MaxStamina { get; set; } = 100m;
	internal decimal StaminaGrow { get; set; } = 2m;
	internal decimal Stamina { get; private set; }
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
	[Export] public NodePath EnemyBarPath = new NodePath("");
	[Export] public NodePath StatusLabelPath = new NodePath("");
	[Export] public NodePath EnemyStatusLabelPath = new NodePath("");
	[Export] public NodePath InventoryLabelPath = new NodePath("");
	private ParallaxBackground background;
	private ParallaxLayer panorama;
	private AnimatedSprite player, enemy;
	private Line2D shotTrail;
	private Vector2 playerOrigin, enemyOrigin, backgroundOrigin;
	private Color playerColor, enemyColor;
	private string playerAction = "attack";
	private Label healthLabel, rageLabel, staminaLabel, status, enemyStatus, inventoryText;
	private ProgressBar healthBar, rageBar, enemyBar;
	private TouchScreenButton[] actions = new TouchScreenButton[5];
	private Panel inventory;
	private TouchActionButton heal;
	private int hp, rage, enemyHp, enemyMax, wave = 1, coins, kits = 2;
	private float scroll, targetScroll, cooldown, pose, dodgeRemaining;
	private float enemyClock, enemyPose, respawn, flash, enemyFlash, enemyHurt;
	private bool blocking, dodging, gameOver, blockTouchHeld, blockKeyHeld;
	private string message = "Ударьте врага, чтобы накопить ярость.";

	public override void _Ready()
	{
		string executableDirectory = System.IO.Path.GetDirectoryName(OS.GetExecutablePath());
		WeaponConfiguration.Load(executableDirectory, this);
		HeroConfiguration.Load(executableDirectory, this);
		EnemyConfiguration.Load(executableDirectory, this);
		EnemyDamage = Math.Max(0, EnemyDamage);
		EnemyHealth = Math.Max(1, EnemyHealth);
		EnemyAttackInterval = Mathf.Max(0.001f, EnemyAttackInterval);
		enemyMax = EnemyHealth;
		enemyHp = enemyMax;
		MaxHealth = Math.Max(1, MaxHealth);
		MaxRage = Math.Max(1, MaxRage);
		hp = MaxHealth;
		MaxStamina = Math.Max(0m, MaxStamina);
		StaminaGrow = Math.Max(0m, StaminaGrow);
		Stamina = 0m;
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
		healthLabel = GetOptionalNode<Label>(HealthLabelPath);
		rageLabel = GetOptionalNode<Label>(RageLabelPath);
		staminaLabel = GetOptionalNode<Label>(StaminaLabelPath);
		status = GetOptionalNode<Label>(StatusLabelPath);
		enemyStatus = GetOptionalNode<Label>(EnemyStatusLabelPath);
		healthBar = GetOptionalNode<ProgressBar>(HealthBarPath);
		rageBar = GetOptionalNode<ProgressBar>(RageBarPath);
		enemyBar = GetOptionalNode<ProgressBar>(EnemyBarPath);
		actions = new[]
		{
			BindActionButton(PunchButtonPath, nameof(Punch), nameof(PunchButtonPath)),
			BindActionButton(ShootButtonPath, nameof(Shoot), nameof(ShootButtonPath)),
			BindActionButton(DodgeButtonPath, nameof(Dodge), nameof(DodgeButtonPath)),
			BindActionButton(BlockButtonPath, nameof(Block), nameof(BlockButtonPath)),
			BindActionButton(InventoryButtonPath, nameof(ToggleInventory), nameof(InventoryButtonPath))
		};
		if (actions[3] != null && !actions[3].IsConnected("released", this, nameof(ReleaseBlock))) actions[3].Connect("released", this, nameof(ReleaseBlock));
		inventory = GetNode<Panel>("UI/HUD/Inventory");
		inventoryText = GetOptionalNode<Label>(InventoryLabelPath);
		heal = inventory.GetNode<TouchActionButton>("HealButton/TouchButton");
		inventory.Visible = false;
		Refresh();
	}

	public override void _Process(float delta)
	{
		if (!inventory.Visible && !gameOver)
		{
			AccumulateStamina(delta);
			cooldown = Mathf.Max(0,cooldown-delta);
			pose = Mathf.Max(0,pose-delta); enemyPose = Mathf.Max(0,enemyPose-delta); enemyHurt = Mathf.Max(0,enemyHurt-delta);
			dodgeRemaining = Mathf.Max(0, dodgeRemaining - delta); flash = Mathf.Max(0,flash-delta); enemyFlash = Mathf.Max(0,enemyFlash-delta);
			dodging = dodgeRemaining > 0;
			if (enemyHp <= 0)
			{
				respawn -= delta;
				if (respawn <= 0) { wave++; enemyMax = EnemyHealth; enemyHp = enemyMax; enemyClock=0; enemyHurt=0; enemyPose=0; enemyFlash=0; enemy.Modulate=enemyColor; enemy.Play("idle"); enemy.Frame=0; message="Новый противник!"; }
			}
			else
			{
				enemyClock += delta;
				if (enemyClock >= Mathf.Max(0.001f,EnemyAttackInterval))
				{
					enemyClock = 0; if (enemyHurt <= 0) { enemy.Play("attack"); enemy.Frame=0; } enemyPose=AnimationDuration(enemy, "attack", 0.25f);
					if (dodging) message="Уворот: атака прошла мимо!";
					else if (blocking) message="Блок: атака отражена!";
					else { int damage = EnemyDamage; hp=Math.Max(0,hp-damage); flash=0.18f; message="Враг нанёс " + damage + " урона."; }
					if(hp==0) { gameOver=true; message="Вы проиграли. Нажмите R для новой игры."; }
				}
			}
		}
		if (!inventory.Visible && !gameOver)
			scroll = Mathf.Lerp(scroll, targetScroll, Mathf.Min(1, delta * 12));
		float width = panorama.MotionMirroring.x;
		if (width > 0 && scroll >= width) { scroll -= width; targetScroll -= width; }
		background.ScrollOffset = backgroundOrigin + new Vector2(-scroll, 0);
		player.Position = playerOrigin + new Vector2(dodging ? -90 : pose > 0 ? 22 : 0, 0);
		enemy.Position = enemyOrigin + new Vector2(enemyPose > 0 ? -25 : 0, 0);
		player.Modulate = DamageFlashEnabled && flash > 0 ? new Color("ff7777") : playerColor;
		SetAnimation(player, gameOver ? "defeat" : blocking ? "block" : pose > 0 ? playerAction : dodging ? "dodge" : flash > 0 ? "hurt" : "idle");
		enemy.Modulate = DamageFlashEnabled && enemyFlash > 0 ? new Color("ff7777") : enemyColor;
		SetAnimation(enemy, enemyHp <= 0 ? "defeat" : enemyHurt > 0 ? "hurt" : enemyPose > 0 ? "attack" : "idle");
		player.Playing = !inventory.Visible && !gameOver;
		enemy.Playing = !inventory.Visible && !gameOver;
		shotTrail.Visible = pose > 0 && playerAction == "shoot";
		if (shotTrail.Visible)
		{
			shotTrail.SetPointPosition(0, player.Position + new Vector2(45, -25));
			shotTrail.SetPointPosition(1, enemy.Position + new Vector2(-40, -25));
		}
		Refresh();
	}
	private static void SetAnimation(AnimatedSprite actor, string animation)
	{
		if (actor.Frames.HasAnimation(animation) && actor.Animation != animation)
			actor.Play(animation);
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

	private bool CanAct() { return !blocking && !gameOver && !inventory.Visible && enemyHp>0 && cooldown<=0; }
	private bool CanPunch() { return CanAct() && !dodging && rage >= Math.Max(0, PunchCost) && Stamina >= Math.Max(0m, PunchStaminaCost); }
	private bool CanShoot() { return CanAct() && rage >= Math.Max(0, ShotCost) && Stamina >= Math.Max(0m, ShotStaminaCost); }
	public void Punch()
	{
		if (!CanPunch()) return;
		Stamina -= Math.Max(0m, PunchStaminaCost);
		rage -= Math.Max(0, PunchCost);
		rage=(int)Math.Min(MaxRage, (long)rage + Math.Max(1, RagePerPunch)); targetScroll+=Mathf.Max(0,ScrollPerPunch);
		StartPlayerAnimation("attack"); pose=AnimationDuration(player, "attack", 0.20f); cooldown=Math.Max(0, PunchCooldownMs) / 1000f; HurtEnemy(PunchDamage); message="Удар! +"+RagePerPunch+" ярости."; Refresh();
	}
	public void Shoot()
	{
		if (!CanShoot()) return;
		Stamina -= Math.Max(0m, ShotStaminaCost);
		rage-=Math.Max(0,ShotCost); StartPlayerAnimation("shoot"); pose=AnimationDuration(player, "shoot", 0.23f); cooldown=Math.Max(0, ShotCooldownMs) / 1000f; HurtEnemy(ShotDamage); message="Выстрел! −"+ShotCost+" ярости."; Refresh();
	}
	private bool CanDodge()
	{
		return !blocking && !dodging && !gameOver && !inventory.Visible && enemyHp > 0 && Stamina >= DodgeStamina;
	}
	public void Dodge()
	{
		if (!CanDodge()) return;
		Stamina -= DodgeStamina;
		dodging = true;
		dodgeRemaining = Math.Max(1, DodgeTimeMs) / 1000f;
		pose = 0;
		flash = 0;
		player.Position = playerOrigin + new Vector2(-90, 0);
		SetAnimation(player, "dodge");
		message = "Отскок: атаки врага не причиняют урона.";
		Refresh();
	}
	public void Block()
	{
		blockTouchHeld = true;
		UpdateBlock();
	}
	public void ReleaseBlock()
	{
		blockTouchHeld = false;
		UpdateBlock();
	}
	private void UpdateBlock()
	{
		blocking = !gameOver && (blockTouchHeld || blockKeyHeld);
		if (blocking)
		{
			pose = 0;
			flash = 0;
			SetAnimation(player, "block");
			message = "Защита: удерживайте кнопку блока.";
		}
	}
	public override void _Input(InputEvent inputEvent)
	{
		// Process release even when a Control consumes keyboard input.
		if (inputEvent is InputEventKey key && key.Scancode == (uint)KeyList.Key4 && !key.Pressed)
		{
			blockKeyHeld = false;
			UpdateBlock();
		}
	}
	private void HurtEnemy(int damage)
	{
		enemyHp=Math.Max(0,enemyHp-Math.Max(1,damage));
		enemyFlash = 0.18f;
		enemyPose = 0;
		if (enemyHp > 0 && enemy.Frames.HasAnimation("hurt"))
		{
			enemyHurt = AnimationDuration(enemy, "hurt", 0.18f);
			enemy.Play("hurt");
			enemy.Frame = 0;
		}
		enemy.Modulate = DamageFlashEnabled ? new Color("ff7777") : enemyColor;
		if(enemyHp==0) { coins+=10; if(wave%3==0) kits++; respawn=0.9f; enemyClock=0; }
	}
	public void ToggleInventory() { if(!gameOver) inventory.Visible=!inventory.Visible; }
	public void UseKit()
	{
		if(!inventory.Visible || kits<=0 || hp>=MaxHealth || gameOver) return;
		kits--; hp=(int)Math.Min(MaxHealth, (long)hp + 40); Refresh();
	}
	private TouchScreenButton BindActionButton(NodePath path, string method, string field)
	{
		var button = path == null || path.IsEmpty() ? null : GetNodeOrNull<TouchScreenButton>(path);
		if (button == null)
		{
			GD.PushWarning("Main: assign a TouchScreenButton to " + field + " in the inspector.");
			return null;
		}
		button.SetProcessInput(true);
		if (!button.IsConnected("pressed", this, method))
			button.Connect("pressed", this, method);
		return button;
	}

	private static void SetButtonEnabled(TouchScreenButton button, bool enabled)
	{
		if (button == null) return;
		if (button is TouchActionButton custom)
			custom.Disabled = !enabled;
		else
		{
			// Leave input active so a cooldown cannot discard the touch release.
			button.Modulate = enabled ? Colors.White : new Color(0.45f, 0.45f, 0.45f, 1);
		}
	}
		private T GetOptionalNode<T>(NodePath path) where T : Node
	{
		if (path == null || path.IsEmpty()) return null;
		return GetNodeOrNull<Node>(path) as T;
	}
		private void AccumulateStamina(float delta)
	{
		if (blocking || delta <= 0 || StaminaGrow <= 0 || Stamina >= MaxStamina) return;
		decimal seconds = (decimal)delta;
		decimal remaining = MaxStamina - Stamina;
		// Compare before multiplying to avoid overflow for large INI values.
		if (seconds >= 1m && StaminaGrow >= remaining / seconds)
			Stamina = MaxStamina;
		else if (seconds < 1m && StaminaGrow * seconds >= remaining)
			Stamina = MaxStamina;
		else
			Stamina += StaminaGrow * seconds;
	}
	private void Refresh()
	{
		if (Godot.Object.IsInstanceValid(healthLabel)) healthLabel.Text = $"{hp}/{MaxHealth}";
		if (Godot.Object.IsInstanceValid(rageLabel)) rageLabel.Text = $"{rage}/{MaxRage}";
		if (Godot.Object.IsInstanceValid(staminaLabel)) staminaLabel.Text = Stamina.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "/" + MaxStamina.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
		bool warning=enemyHp>0 && enemyClock>=Mathf.Max(0.001f,EnemyAttackInterval)-Mathf.Max(0.1f,EnemyWarningTime);
		if (Godot.Object.IsInstanceValid(enemyStatus)) enemyStatus.Text=enemyHp<=0 ? "ПОБЕДА! +10 монет" : "ВРАГ "+wave+"  •  "+enemyHp+"/"+enemyMax+(warning ? "   ⚠ АТАКУЕТ!" : "");
		if (Godot.Object.IsInstanceValid(enemyStatus)) enemyStatus.Modulate=warning ? new Color("ff8a5b") : Colors.White;
		if (Godot.Object.IsInstanceValid(status)) status.Text = message;
		if (Godot.Object.IsInstanceValid(healthBar)) { healthBar.MaxValue = MaxHealth; healthBar.Value = hp; }
		if (Godot.Object.IsInstanceValid(rageBar)) { rageBar.MaxValue = MaxRage; rageBar.Value = rage; }
		if (Godot.Object.IsInstanceValid(enemyBar)) { enemyBar.MaxValue = enemyMax; enemyBar.Value = enemyHp; }
		SetButtonEnabled(actions[0], CanPunch()); SetButtonEnabled(actions[1], CanShoot());
		if (actions[1] is TouchActionButton shootButton) shootButton.Text="ВЫСТРЕЛ [2]  "+ShotCost+" ЯР";
		SetButtonEnabled(actions[2], CanDodge()); SetButtonEnabled(actions[3], !gameOver); SetButtonEnabled(actions[4], !gameOver);
		if (Godot.Object.IsInstanceValid(inventoryText)) inventoryText.Text="ИНВЕНТАРЬ\n\nМонеты: "+coins+"\nАптечки: "+kits+"\nБой приостановлен";
		heal.Disabled=kits<=0 || hp>=MaxHealth;
	}
	public override void _UnhandledKeyInput(InputEventKey key)
	{
		if(!key.Pressed || key.Echo) return;
		switch((KeyList)key.Scancode)
		{
			case KeyList.Key1: Punch(); break;
			case KeyList.Key2: Shoot(); break;
			case KeyList.Key3: Dodge(); break;
			case KeyList.Key4: blockKeyHeld = true; UpdateBlock(); break;
			case KeyList.I: case KeyList.Escape: ToggleInventory(); break;
			case KeyList.R: GetTree().ReloadCurrentScene(); break;
		}
	}
}



















