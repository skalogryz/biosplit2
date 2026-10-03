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

	private Texture background, idle, attack, zombieIdle, zombieAttack;
	private Sprite player, enemy;
	private Label stats, status, enemyStatus, inventoryText;
	private ProgressBar healthBar, rageBar, enemyBar;
	private TouchActionButton[] actions = new TouchActionButton[5];
	private Panel inventory;
	private TouchActionButton heal;
	private int hp = 100, rage, enemyHp = 80, enemyMax = 80, wave = 1, coins, kits = 2;
	private float scroll, targetScroll, cooldown, pose, defense, dodgeCooldown;
	private float enemyClock, enemyPose, respawn, flash;
	private bool blocking, dodging, gameOver;
	private string message = "Ударьте врага, чтобы накопить ярость.";
	private Vector2 view = new Vector2(1280,720);
	private readonly Color accent = new Color("ef7c36");

	public override void _Ready()
	{
		background = GD.Load<Texture>("res://Assets/Background/battle_01.jpg");
		idle = GD.Load<Texture>("res://Assets/Sprites/Pers_idle_01.png");
		attack = GD.Load<Texture>("res://Assets/Sprites/Pers_atak_01.png");
		zombieIdle = GD.Load<Texture>("res://Assets/Sprites/Zombi_idle_01.png");
		zombieAttack = GD.Load<Texture>("res://Assets/Sprites/Zombi_atak_01.png");
		player = new Sprite { Texture = idle, Scale = Vector2.One * 2.1f };
		enemy = new Sprite { Texture = zombieIdle, Scale = Vector2.One * 2.1f, FlipH = true };
		AddChild(player); AddChild(enemy);
		stats = GetNode<Label>("UI/HUD/Stats");
		status = GetNode<Label>("UI/HUD/Status");
		enemyStatus = GetNode<Label>("UI/HUD/EnemyStatus");
		healthBar = GetNode<ProgressBar>("UI/HUD/HealthBar");
		rageBar = GetNode<ProgressBar>("UI/HUD/RageBar");
		enemyBar = GetNode<ProgressBar>("UI/HUD/EnemyBar");
		string[] buttonNames = { "PunchButton", "ShootButton", "DodgeButton", "BlockButton", "InventoryButton" };
		for (int i = 0; i < actions.Length; i++)
			actions[i] = GetNode<TouchActionButton>("UI/HUD/" + buttonNames[i] + "/TouchButton");
		inventory = GetNode<Panel>("UI/HUD/Inventory");
		inventoryText = inventory.GetNode<Label>("InventoryText");
		heal = inventory.GetNode<TouchActionButton>("HealButton/TouchButton");
		inventory.Visible = false;
		Refresh();
	}

	public override void _Process(float delta)
	{
		view = GetViewportRect().Size;
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
					enemyClock = 0; enemyPose=0.25f;
					if (dodging) message="Уворот: атака прошла мимо!";
					else { int damage = blocking ? 3 : 15; hp=Math.Max(0,hp-damage); flash=0.18f; message=blocking ? "Блок: получено только 3 урона." : "Враг нанёс 15 урона."; }
					if(hp==0) { gameOver=true; message="Вы проиграли. Нажмите R для новой игры."; }
				}
			}
		}
		scroll = Mathf.Lerp(scroll,targetScroll,Mathf.Min(1,delta*12));
		// Rebase after every complete tile to avoid precision loss during long sessions.
		float width = background.GetWidth() * (view.y/background.GetHeight());
		if (scroll >= width) { scroll-=width; targetScroll-=width; }
		player.Texture=pose>0 ? attack : idle;
		player.Position = new Vector2(view.x*0.39f-(dodging ? 85 : 0)+(pose>0 ? 22 : 0),view.y*0.53f);
		player.Modulate = blocking ? new Color("77bbff") : flash>0 ? new Color("ff7777") : Colors.White;
		enemy.Texture=enemyPose>0 ? zombieAttack : zombieIdle;
		enemy.Position=new Vector2(view.x*0.62f-(enemyPose>0 ? 25 : 0),view.y*0.53f);
		enemy.Visible=enemyHp>0;
		Refresh(); Update();
	}

	public override void _Draw()
	{
		float scale=view.y/background.GetHeight(), width=background.GetWidth()*scale;
		float offset=scroll % width;
		for(float x=-offset; x<view.x; x+=width)
			DrawTextureRect(background,new Rect2(x,0,width,view.y),false);
		if(pose>0 && cooldown>0.35f)
			DrawLine(player.Position+new Vector2(45,-25),enemy.Position+new Vector2(-40,-25),accent,5);
		if(blocking) DrawArc(player.Position,110,-1.5f,1.5f,32,new Color("77bbff"),4);
	}
	private bool CanAct() { return !gameOver && !inventory.Visible && enemyHp>0 && cooldown<=0; }
	public void Punch()
	{
		if(!CanAct()) return;
		rage=Math.Min(100,rage+Math.Max(1,RagePerPunch)); targetScroll+=Mathf.Max(0,ScrollPerPunch);
		pose=0.20f; cooldown=0.26f; HurtEnemy(PunchDamage); message="Удар! +"+RagePerPunch+" ярости.";
	}
	public void Shoot()
	{
		if(!CanAct() || rage<Math.Max(1,ShotCost)) return;
		rage-=Math.Max(1,ShotCost); pose=0.23f; cooldown=0.55f; HurtEnemy(ShotDamage); message="Выстрел! −"+ShotCost+" ярости.";
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
	private void Refresh()
	{
		stats.Text="BIOSPLIT 2   |   HP "+hp+"   |   ЯРОСТЬ "+rage+"/100";
		bool warning=enemyHp>0 && enemyClock>=Mathf.Max(1.2f,EnemyAttackInterval)-Mathf.Max(0.1f,EnemyWarningTime);
		enemyStatus.Text=enemyHp<=0 ? "ПОБЕДА! +10 монет" : "ВРАГ "+wave+"  •  "+enemyHp+"/"+enemyMax+(warning ? "   ⚠ АТАКУЕТ!" : "");
		enemyStatus.Modulate=warning ? new Color("ff8a5b") : Colors.White;
		status.Text=message; healthBar.Value=hp; rageBar.Value=rage; enemyBar.MaxValue=enemyMax; enemyBar.Value=enemyHp;
		actions[0].Disabled=!CanAct(); actions[1].Disabled=!CanAct() || rage<Math.Max(1,ShotCost);
		actions[1].Text="ВЫСТРЕЛ [2]  "+ShotCost+" ЯР";
		actions[2].Disabled=!CanAct() || dodgeCooldown>0; actions[3].Disabled=!CanAct(); actions[4].Disabled=gameOver;
		inventoryText.Text="ИНВЕНТАРЬ\n\nМонеты: "+coins+"\nАптечки: "+kits+"\nБой приостановлен";
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



