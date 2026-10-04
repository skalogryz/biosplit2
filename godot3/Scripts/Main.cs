using Godot;
using System;

public class Main : Node2D
{
	[Export] public int RagePerPunch = 10;
	[Export] public int ShotCost = 30;
	[Export] public int PunchDamage = 12;
	[Export] public int ShotDamage = 40;
	[Export] public float ScrollPerPunch = 32;
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
		[Export] public NodePath HealthBarPath = new NodePath("");
	[Export] public NodePath RageBarPath = new NodePath("");
	[Export] public NodePath EnemyBarPath = new NodePath("");
	[Export] public NodePath StatusLabelPath = new NodePath("");
	[Export] public NodePath EnemyStatusLabelPath = new NodePath("");
	[Export] public NodePath InventoryLabelPath = new NodePath("");
	private ParallaxBackground background;
	private ParallaxLayer panorama;
	private AnimatedSprite player, enemy;
	private Line2D shield, shotTrail;
	private Vector2 playerOrigin, enemyOrigin, backgroundOrigin;
	private Color playerColor;
	private string playerAction = "attack";
	private Label healthLabel, rageLabel, status, enemyStatus, inventoryText;
	private ProgressBar healthBar, rageBar, enemyBar;
	private TouchScreenButton[] actions = new TouchScreenButton[5];
	private Panel inventory;
	private TouchActionButton heal;
	private int hp = 100, rage, enemyHp = 80, enemyMax = 80, wave = 1, coins, kits = 2;
	private float scroll, targetScroll, cooldown, pose, defense, dodgeCooldown;
	private float enemyClock, enemyPose, respawn, flash;
	private bool blocking, dodging, gameOver;
	private string message = "Ударьте врага, чтобы накопить ярость.";

	public override void _Ready()
	{
		background = GetNode<ParallaxBackground>("Background");
		panorama = GetNode<ParallaxLayer>("Background/Panorama");
		backgroundOrigin = background.ScrollOffset;
		player = GetNode<AnimatedSprite>("Combatants/Player");
		enemy = GetNode<AnimatedSprite>("Combatants/Enemy");
		shield = player.GetNode<Line2D>("BlockShield");
		shotTrail = GetNode<Line2D>("Combatants/ShotTrail");
		playerOrigin = player.Position;
		enemyOrigin = enemy.Position;
		playerColor = player.Modulate;
		healthLabel = GetOptionalNode<Label>(HealthLabelPath);
		rageLabel = GetOptionalNode<Label>(RageLabelPath);
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
			cooldown = Mathf.Max(0,cooldown-delta); dodgeCooldown = Mathf.Max(0,dodgeCooldown-delta);
			pose = Mathf.Max(0,pose-delta); enemyPose = Mathf.Max(0,enemyPose-delta);
			defense = Mathf.Max(0,defense-delta); flash = Mathf.Max(0,flash-delta);
			if (defense <= 0) { blocking = false; dodging = false; }
			if (enemyHp <= 0)
			{
				respawn -= delta;
				if (respawn <= 0) { wave++; enemyMax = 80+(wave-1)*15; enemyHp = enemyMax; enemyClock=0; message="Новый противник!"; }
			}
			else
			{
				enemyClock += delta;
				if (enemyClock >= Mathf.Max(1.2f,EnemyAttackInterval))
				{
					enemyClock = 0; enemy.Frame=0; enemy.Play("attack"); enemyPose=AnimationDuration(enemy, "attack", 0.25f);
					if (dodging) message="Уворот: атака прошла мимо!";
					else { int damage = blocking ? 3 : 15; hp=Math.Max(0,hp-damage); flash=0.18f; message=blocking ? "Блок: получено только 3 урона." : "Враг нанёс 15 урона."; }
					if(hp==0) { gameOver=true; message="Вы проиграли. Нажмите R для новой игры."; }
				}
			}
		}
		if (!inventory.Visible && !gameOver)
			scroll = Mathf.Lerp(scroll, targetScroll, Mathf.Min(1, delta * 12));
		float width = panorama.MotionMirroring.x;
		if (width > 0 && scroll >= width) { scroll -= width; targetScroll -= width; }
		background.ScrollOffset = backgroundOrigin + new Vector2(-scroll, 0);
		player.Position = playerOrigin + new Vector2((dodging ? -85 : 0) + (pose > 0 ? 22 : 0), 0);
		enemy.Position = enemyOrigin + new Vector2(enemyPose > 0 ? -25 : 0, 0);
		player.Modulate = blocking ? new Color("77bbff") : flash > 0 ? new Color("ff7777") : playerColor;
		SetAnimation(player, gameOver ? "defeat" : dodging ? "dodge" : blocking ? "block" : pose > 0 ? playerAction : flash > 0 ? "hurt" : "idle");
		SetAnimation(enemy, enemyHp <= 0 ? "defeat" : enemyPose > 0 ? "attack" : "idle");
		player.Playing = !inventory.Visible && !gameOver;
		enemy.Playing = !inventory.Visible && !gameOver;
		shield.Visible = blocking;
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

	private bool CanAct() { return !gameOver && !inventory.Visible && enemyHp>0 && cooldown<=0; }
	public void Punch()
	{
		GD.Print("punch!");
		if(!CanAct()) return;
		rage=Math.Min(100,rage+Math.Max(1,RagePerPunch)); targetScroll+=Mathf.Max(0,ScrollPerPunch);
		StartPlayerAnimation("attack"); pose=AnimationDuration(player, "attack", 0.20f); cooldown=0.26f; HurtEnemy(PunchDamage); message="Удар! +"+RagePerPunch+" ярости.";
	}
	public void Shoot()
	{
		if(!CanAct() || rage<Math.Max(1,ShotCost)) return;
		rage-=Math.Max(1,ShotCost); StartPlayerAnimation("shoot"); pose=AnimationDuration(player, "shoot", 0.23f); cooldown=0.55f; HurtEnemy(ShotDamage); message="Выстрел! −"+ShotCost+" ярости.";
	}
	public void Dodge()
	{
		if(!CanAct() || dodgeCooldown>0) return;
		blocking=false; dodging=true; defense=0.65f; dodgeCooldown=1.1f; cooldown=0.2f; message="Уворот: 0,65 секунды неуязвимости.";
	}
	public void Block()
	{
		if(!CanAct()) return;
		dodging=false; blocking=true; defense=1.0f; cooldown=0.2f; message="Защита на 1 секунду.";
	}
	private void HurtEnemy(int damage)
	{
		enemyHp=Math.Max(0,enemyHp-Math.Max(1,damage));
		if(enemyHp==0) { coins+=10; if(wave%3==0) kits++; respawn=0.9f; enemyClock=0; }
	}
	public void ToggleInventory() { if(!gameOver) inventory.Visible=!inventory.Visible; }
	public void UseKit()
	{
		if(!inventory.Visible || kits<=0 || hp>=100 || gameOver) return;
		kits--; hp=Math.Min(100,hp+40); Refresh();
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
	private void Refresh()
	{
		if (Godot.Object.IsInstanceValid(healthLabel)) healthLabel.Text = "ЖИЗНЬ: " + hp + "/100";
		if (Godot.Object.IsInstanceValid(rageLabel)) rageLabel.Text = "ЯРОСТЬ: " + rage + "/100";
		bool warning=enemyHp>0 && enemyClock>=Mathf.Max(1.2f,EnemyAttackInterval)-Mathf.Max(0.1f,EnemyWarningTime);
		if (Godot.Object.IsInstanceValid(enemyStatus)) enemyStatus.Text=enemyHp<=0 ? "ПОБЕДА! +10 монет" : "ВРАГ "+wave+"  •  "+enemyHp+"/"+enemyMax+(warning ? "   ⚠ АТАКУЕТ!" : "");
		if (Godot.Object.IsInstanceValid(enemyStatus)) enemyStatus.Modulate=warning ? new Color("ff8a5b") : Colors.White;
		if (Godot.Object.IsInstanceValid(status)) status.Text = message;
		if (Godot.Object.IsInstanceValid(healthBar)) healthBar.Value = hp;
		if (Godot.Object.IsInstanceValid(rageBar)) rageBar.Value = rage;
		if (Godot.Object.IsInstanceValid(enemyBar)) { enemyBar.MaxValue = enemyMax; enemyBar.Value = enemyHp; }
		SetButtonEnabled(actions[0], CanAct()); SetButtonEnabled(actions[1], CanAct() && rage >= Math.Max(1, ShotCost));
		if (actions[1] is TouchActionButton shootButton) shootButton.Text="ВЫСТРЕЛ [2]  "+ShotCost+" ЯР";
		SetButtonEnabled(actions[2], CanAct() && dodgeCooldown <= 0); SetButtonEnabled(actions[3], CanAct()); SetButtonEnabled(actions[4], !gameOver);
		if (Godot.Object.IsInstanceValid(inventoryText)) inventoryText.Text="ИНВЕНТАРЬ\n\nМонеты: "+coins+"\nАптечки: "+kits+"\nБой приостановлен";
		heal.Disabled=kits<=0 || hp>=100;
	}
	public override void _UnhandledKeyInput(InputEventKey key)
	{
		if(!key.Pressed || key.Echo) return;
		switch((KeyList)key.Scancode)
		{
			case KeyList.Key1: Punch(); break;
			case KeyList.Key2: Shoot(); break;
			case KeyList.Key3: Dodge(); break;
			case KeyList.Key4: Block(); break;
			case KeyList.I: case KeyList.Escape: ToggleInventory(); break;
			case KeyList.R: GetTree().ReloadCurrentScene(); break;
		}
	}
}








