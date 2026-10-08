using System;
using System.Globalization;
using System.IO;
using Biosplit.Game;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        checks++;
    }

    private static GameSettings Quiet() => new GameSettings
    {
        EnemyAttackInterval = 1000m, EnemyHealth = 10000,
        PunchCooldownMs = 0, ShotCooldownMs = 0, PunchDamage = 1, ShotDamage = 1
    };

    private static void Weapons()
    {
        var g = new GameSession(Quiet());
        int enemy = g.EnemyHealth;
        Check(!g.Punch() && g.EnemyHealth == enemy && g.Rage == 0, "Knife without stamina cannot damage or grant rage");
        g.Tick(1.5m);
        Check(g.Stamina == 3m && g.Punch() && g.Stamina == 0m && g.Rage == 10, "Exact knife cost");
        Check(!g.Shoot(), "Insufficient rage");
        for (int i = 0; i < 2; i++) { g.Tick(1.5m); g.Punch(); }
        Check(g.Rage == 30 && g.Shoot() && g.Stamina == 0 && g.Rage == 0, "Handgun uses rage and needs no stamina by default");

        var s = Quiet();
        s.ShotStaminaCost = 1.25m;
        g = new GameSession(s);
        for (int i = 0; i < 3; i++) { g.Tick(1.5m); g.Punch(); }
        Check(!g.Shoot() && g.Rage == 30, "Both resources required");
        g.Tick(0.625m);
        Check(g.Shoot() && g.Rage == 0 && g.Stamina == 0, "Decimal shot cost spent exactly");

        s = Quiet(); s.PunchCooldownMs = 260; s.ShotCooldownMs = 550; s.PunchStaminaCost = 0;
        g = new GameSession(s);
        g.Punch();
        Check(!g.Punch(), "Cooldown starts");
        g.Tick(0.259m); Check(!g.Punch(), "Cooldown before boundary");
        g.Tick(0.001m); Check(g.Punch(), "Cooldown expires at 260 milliseconds");
        g.Tick(0.26m); g.Punch(); g.Tick(0.26m); g.Shoot();
        g.Tick(0.549m); Check(!g.Punch(), "Shared handgun cooldown");
        g.Tick(0.001m); Check(g.Punch(), "Handgun cooldown expires at 550 milliseconds");
    }

    private static void KnifeCooldownStamina()
    {
        var settings = Quiet();
        settings.PunchCooldownMs = 260;
        settings.ShotCooldownMs = 550;
        settings.ShotCost = 0;
        var game = new GameSession(settings);
        game.Tick(1.5m);
        Check(game.Punch() && game.Stamina == 0m, "Knife consumes accumulated stamina");
        game.Tick(0.259m);
        Check(game.Stamina == 0m && game.CooldownRemaining == 0.001m, "Knife cooldown prevents stamina growth");
        Check(!game.Punch(), "Rejected knife does not restart cooldown");
        game.Tick(0.001m);
        Check(game.Stamina == 0m && game.CooldownRemaining == 0m, "No regeneration exactly at cooldown boundary");
        game.Tick(0.1m);
        Check(game.Stamina == 0.2m, "Stamina resumes after knife cooldown");

        game.Tick(1.4m); game.Punch(); game.Tick(0.36m);
        Check(game.Stamina == 0.2m, "Large update regenerates only time after knife cooldown");

        game.Tick(1.4m); game.Punch(); game.SetInventoryOpen(true); game.Tick(5m);
        Check(game.Stamina == 0m && game.CooldownRemaining == 0.26m, "Inventory pauses knife cooldown and regeneration");
        game.SetInventoryOpen(false); game.Tick(0.26m);
        Check(game.Stamina == 0m && game.Shoot(), "Shot can follow knife cooldown");
        game.Tick(0.1m);
        Check(game.Stamina == 0.2m && game.CooldownRemaining == 0.45m, "Handgun cooldown allows stamina growth");

        settings.PunchCooldownMs = 0;
        game = new GameSession(settings); game.Tick(1.5m); game.Punch(); game.Tick(0.1m);
        Check(game.Stamina == 0.2m, "Zero knife cooldown allows immediate regeneration");
    }

    private static void CharacterInventories()
    {
        var settings = Quiet();
        var first = new Character("first");
        var second = new Character("second");
        Check(!ReferenceEquals(first.Inventory, second.Inventory), "Characters own independent inventories");
        Check(first.Inventory.Weapon1 == null && first.Inventory.Weapon2 == null, "New inventory has empty slots");
        var definition = settings.Weapons["handgun"];
        first.Inventory.Weapon1 = new WeaponItem(definition, 12);
        second.Inventory.Weapon2 = new WeaponItem(definition, 30);
        Check(ReferenceEquals(first.Inventory.Weapon1.Definition, definition) &&
            ReferenceEquals(second.Inventory.Weapon2.Definition, definition), "Items reference the exact lookup definition");
        first.Inventory.Weapon1.Ammo = 5;
        Check(second.Inventory.Weapon2.Ammo == 30 && second.Inventory.Weapon1 == null, "Ammo and slots are per character and per item");
        first.Inventory.Weapon2 = new WeaponItem(settings.Weapons["knife"]);
        Check(first.Inventory.Weapon2.Definition.Type == WeaponType.Melee && first.Inventory.Weapon2.Ammo == 0, "Melee item also has ammo defaulting to zero");
        first.Inventory.Weapon1 = null;
        Check(second.Inventory.Weapon2.Ammo == 30, "Clearing one inventory leaves another unchanged");
        bool rejected = false;
        try { new WeaponItem(null); } catch (ArgumentNullException) { rejected = true; }
        Check(rejected, "Item requires a weapon definition");

        var game = new GameSession(settings);
        var otherGame = new GameSession(settings);
        Check(game.Player.Inventory.Weapon1.Definition.Name == "knife" &&
            game.Player.Inventory.Weapon2.Definition.Name == "handgun", "Current player starts with knife and handgun");
        Check(!ReferenceEquals(game.Player.Inventory, otherGame.Player.Inventory) &&
            !ReferenceEquals(game.Player.Inventory.Weapon2, otherGame.Player.Inventory.Weapon2), "Sessions do not share inventory or item instances");
        settings.Weapons["handgun"].Damage = 999;
        Check(game.Player.Inventory.Weapon2.Definition.Damage == 1, "Inventory uses session lookup snapshot");
        for (int i = 0; i < 3; i++) { game.Tick(1.5m); game.Punch(); }
        Check(game.Player.Inventory.Weapon2.Ammo == 0 && game.Shoot() &&
            game.Player.Inventory.Weapon2.Ammo == 0, "Ammo is not required or consumed by firing yet");
    }

    private static void Defense()
    {
        var s = Quiet(); s.EnemyAttackInterval = 0.5m;
        var g = new GameSession(s);
        g.SetBlocking(true); g.Tick(5m);
        Check(g.Blocking && g.Health == 100 && g.Stamina == 0 && !g.CanPunch, "Holding block prevents damage and stamina growth");
        g.SetBlocking(false); g.Tick(0.5m);
        Check(g.Health == 85 && g.Stamina == 1m, "Block release resumes combat");
        s = Quiet(); s.PunchStaminaCost = 0;
        g = new GameSession(s);
        g.Tick(5m); for (int i = 0; i < 3; i++) g.Punch();
        Check(g.Dodge() && g.Stamina == 0 && !g.Dodge() && !g.Punch() && g.Shoot(), "Dodge spends cost once, blocks knife, allows shooting");
        g.Tick(2.999m); Check(g.Dodging, "Dodge before three seconds");
        g.Tick(0.001m); Check(!g.Dodging && g.CanPunch, "Dodge expires at three seconds");
        s.EnemyAttackInterval = 0.1m;
        g = new GameSession(s); g.SetBlocking(true); g.Tick(5m); g.SetBlocking(false);
        // Accumulate safely, then use a fresh quiet session to cover immunity at enemy attack times.
        s.StaminaGrow = 100m; g = new GameSession(s); g.SetBlocking(true); g.Tick(1m); g.SetBlocking(false);
        g.Tick(0.1m); int hp = g.Health;
        g.Dodge(); g.Tick(0.5m);
        Check(g.Health == hp && g.Dodging, "Enemy cannot damage dodging player");
        g.SetBlocking(true); g.Tick(0.5m); g.SetBlocking(false);
        Check(g.Dodging, "Block does not prematurely cancel dodge");
        s.DodgeTimeMs = 750; s.DodgeStamina = 1.25m; s.EnemyAttackInterval = 1000m;
        g = new GameSession(s); g.Tick(0.0125m); Check(g.Dodge() && g.Stamina == 0, "Configured dodge cost");
        g.Tick(0.75m); Check(!g.Dodging, "Configured dodge duration");
    }

    private static void InventoryAndRespawn()
    {
        var s = Quiet(); s.EnemyAttackInterval = 0.5m;
        var g = new GameSession(s); g.Tick(0.5m); g.SetInventoryOpen(true);
        decimal stamina = g.Stamina;
        g.Tick(100m);
        Check(g.Health == 85 && g.Stamina == stamina && !g.Punch() && !g.Dodge(), "Inventory pauses time and actions");
        Check(g.UseKit() && g.Health == 100 && g.Kits == 1 && !g.UseKit(), "Healing consumes one kit and respects maximum");
        g.SetInventoryOpen(false); g.Tick(0.5m); Check(g.Health == 85, "Combat resumes after inventory");
        s = Quiet(); s.EnemyHealth = 120; s.PunchDamage = 120; s.PunchStaminaCost = 0;
        g = new GameSession(s);
        int spawned = 0; g.Changed += (_, e) => { if (e.Kind == GameEventKind.EnemySpawned) spawned++; };
        for (int i = 0; i < 4; i++)
        {
            Check(g.Punch() && g.EnemyHealth == 0 && !g.CanShoot, "Enemy defeat");
            g.Tick(0.899m); Check(g.EnemyHealth == 0, "Respawn delay");
            g.Tick(0.001m); Check(g.EnemyHealth == 120, "Respawn preserves configured health");
        }
        Check(g.Coins == 40 && g.Kits == 3 && g.Wave == 5 && spawned == 4, "Wave rewards and spawn events");
        s = Quiet(); s.MaxHealth = 10; s.EnemyDamage = 10; s.EnemyAttackInterval = 0.1m;
        g = new GameSession(s); g.Tick(0.1m); g.Tick(100m);
        Check(g.GameOver && g.Paused && !g.Punch() && !g.Dodge(), "Defeat stops gameplay");
    }

    private static void ConfigurationAndBounds()
    {
        string directory = Path.Combine(Path.GetTempPath(), "BiosplitGameTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "cfg"));
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            File.WriteAllText(Path.Combine(directory, "cfg", "main.ini"), "[hero]\nhealth=150\nrage=200\nstamina=40.5\nstaminagrow=0.25\ndodgestamina=1.25\ndodgetime=750\n");
            File.WriteAllText(Path.Combine(directory, "cfg", "weapons.ini"), "[knife]\ndamage=9\nstamina=2.5\ncooldown=700\n[handgun]\ndamage=80\nrage=40\nstamina=1.5\ncooldown=900\n");
            File.WriteAllText(Path.Combine(directory, "cfg", "enemy.ini"), "[zombie]\nhealth=120\ndamage=7\ncooldown=500\n");
            var s = new GameSettings(); GameConfiguration.Load(directory, s);
            var g = new GameSession(s);
            Check(g.Health == 150 && g.MaxRage == 200 && g.RagePointStep == 20, "Hero settings and derived rage step");
            Check(s.MaxStamina == 40.5m && s.StaminaGrow == 0.25m && s.DodgeStamina == 1.25m && s.DodgeTimeMs == 750, "Invariant decimal INI");
            Check(s.PunchStaminaCost == 2.5m && s.ShotStaminaCost == 1.5m && s.PunchCooldownMs == 700 && s.ShotCooldownMs == 900, "Legacy weapon INI");
            Check(s.EnemyHealth == 120 && s.EnemyDamage == 7 && s.EnemyAttackInterval == 0.5m, "Enemy milliseconds");
            File.WriteAllText(Path.Combine(directory, "cfg", "weapon.ini"), "[knife]\nstamina=3.25\n[handgun]\nstamina=-1\n");
            GameConfiguration.Load(directory, s);
            Check(s.PunchStaminaCost == 3.25m && s.ShotStaminaCost == 1.5m, "weapon.ini precedence and invalid fallback");
            s.MaxHealth = 1; Check(g.MaxHealth == 150, "Session owns its settings snapshot");
            GameConfiguration.Load(Path.Combine(directory, "missing"), s);
            Check(s.PunchStaminaCost == 3.25m, "Missing INI preserves defaults");
            Check(s.PunchWeaponType == WeaponType.Melee && s.ShotWeaponType == WeaponType.Firearm, "Missing type preserves existing weapon defaults");
            foreach (string alias in new[] { "gun", "firearm", "FiReArM" })
            {
                File.WriteAllText(Path.Combine(directory, "cfg", "weapon.ini"), "[knife]\ntype=melee\n[handgun]\ntype=" + alias + "\n");
                GameConfiguration.Load(directory, s);
                var typedGame = new GameSession(s);
                Check(typedGame.PunchWeaponType == WeaponType.Melee && typedGame.ShotWeaponType == WeaponType.Firearm, "Weapon type aliases and session snapshot: " + alias);
            }
            File.WriteAllText(Path.Combine(directory, "cfg", "weapon.ini"), "[knife]\ntype=unknown\n[handgun]\ntype=none\n");
            GameConfiguration.Load(directory, s);
            Check(s.PunchWeaponType == WeaponType.None && s.ShotWeaponType == WeaponType.None, "Unknown and none type map to None");
            File.WriteAllText(Path.Combine(directory, "cfg", "weapon.ini"),
                "ignored=1\n[Axe]\ntype=melee\ndamage=27\nragebonus=2\nrage=3\nstamina=1.25\ncooldown=450\n" +
                "[AXE]\ndamage=29\n[laser]\ntype=gun\ndamage=80\n[empty]\n");
            GameConfiguration.Load(directory, s);
            Check(s.Weapons.Count == 5, "All named sections loaded, repeated sections merged, unnamed values skipped");
            Check(s.Weapons.TryGetValue("aXe", out var axe) && axe.Name == "Axe" && axe.Type == WeaponType.Melee && axe.Damage == 29, "Weapon lookup is case insensitive");
            Check(axe.RageBonus == 2 && axe.RageCost == 3 && axe.StaminaCost == 1.25m && axe.CooldownMs == 450, "Additional weapons load all parameters");
            Check(s.Weapons["laser"].Type == WeaponType.Firearm && s.Weapons["laser"].StaminaCost == 0m, "Additional firearm defaults");
            Check(s.Weapons["empty"].Type == WeaponType.None && !s.Weapons.TryGetValue("missing", out _), "Empty section and missing weapon lookup");
            s.PunchDamage = 7;
            Check(s.Weapons["KNIFE"].Damage == 7, "Legacy setters update lookup");
            s.Weapons["knife"].Damage = 8;
            Check(s.PunchDamage == 8, "Lookup updates legacy getters");
            s.PunchStaminaCost = 0m;
            var snapshot = new GameSession(s);
            s.Weapons["knife"].Damage = 99;
            Check(snapshot.Punch() && snapshot.EnemyHealth == 112, "Session deep copies weapon definitions");
            File.WriteAllText(Path.Combine(directory, "cfg", "weapon.ini"), "[axe]\nstamina=-1\ndamage=invalid\n");
            GameConfiguration.Load(directory, s);
            Check(s.Weapons["axe"].Damage == 29 && s.Weapons["axe"].StaminaCost == 1.25m, "Invalid values preserve loaded weapon parameters");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
            foreach (string file in Directory.GetFiles(Path.Combine(directory, "cfg"))) File.Delete(file);
            Directory.Delete(Path.Combine(directory, "cfg"));
            Directory.Delete(directory);
        }
        var huge = Quiet(); huge.MaxStamina = decimal.MaxValue; huge.StaminaGrow = decimal.MaxValue;
        var capped = new GameSession(huge); capped.Tick(decimal.MaxValue);
        Check(capped.Stamina == decimal.MaxValue, "Stamina caps without decimal overflow");
        var zero = new GameSession(new GameSettings { MaxHealth = 0, MaxRage = 5, MaxStamina = -1, DodgeTimeMs = 0 });
        Check(zero.Health == 1 && zero.MaxStamina == 0 && zero.RagePointStep == 1, "Safe configuration limits");
    }

    private static int Main()
    {
        try
        {
            Weapons(); KnifeCooldownStamina(); CharacterInventories(); Defense(); InventoryAndRespawn(); ConfigurationAndBounds();
            Console.WriteLine("PASS: " + checks + " checks without Godot");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL: " + error); return 1; }
    }
}
