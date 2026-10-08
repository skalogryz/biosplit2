# Biosplit.Game

Самостоятельная библиотека netstandard2.0. Зависит только от Biosplit.Ini; ссылки на Godot и UI отсутствуют.

- GameSettings — начальные параметры героя, оружия и противника.
- GameConfiguration.Load(directory, settings, warning) — загрузка cfg/main.ini, weapon.ini (или weapons.ini) и enemy.ini относительно переданного каталога. Вывод предупреждений задаётся необязательным Action<string>.
- GameSession — состояние и правила боя. Настройки копируются и проверяются при создании; изменение исходного GameSettings не меняет уже запущенную игру.
- Tick(decimal seconds) — шаг игрового времени. Пауза инвентаря и поражение останавливают таймеры.
- Punch, Shoot, Dodge, UseKit возвращают bool: действие выполнено либо отклонено. Отклонённое действие не тратит ресурсы.
- SetBlocking(bool) и SetInventoryOpen(bool) меняют состояние.
- CanPunch, CanShoot, CanDodge, CanHeal позволяют UI показывать доступность действий.
- Changed передаёт GameEvent для отображения атаки, урона, возрождения и других событий. Текст, анимации, спрайты, фон и ввод находятся в приложении.

Все интервалы в настройках и Tick — секунды, кроме полей с суффиксом Ms. Выносливость, её стоимость и прирост используют decimal.

Пример:

    var settings = new GameSettings();
    GameConfiguration.Load(executableDirectory, settings, Console.WriteLine);
    var game = new GameSession(settings);
    game.Changed += (_, change) => Console.WriteLine(change.Kind);
    game.Tick(1.5m);
    bool usedKnife = game.Punch();
    game.SetBlocking(true);
    game.Tick(3m);
    game.SetBlocking(false);

Godot передаёт OS.GetExecutablePath() только из Scripts/Main.cs. Внутри библиотеки путь, движок и экран неизвестны. Длительности визуальных анимаций не влияют на боевые таймеры.

Сборка из корня проекта:

    dotnet build Libraries/Biosplit.Game/Biosplit.Game.csproj

Проверки без Godot (для запуска консольного проекта тестов нужен .NET 9 SDK):

    dotnet run --project Tests/Biosplit.Game.Tests/Biosplit.Game.Tests.csproj

Тесты покрывают расход ресурсов, кулдауны, удержание блока, отскок, паузу и лечение, возрождение и награды, поражение, настройки INI, InvariantCulture и ограничения числовых значений.

## Таблица оружия

GameSettings.Weapons — Lookup по имени секции INI (регистр не учитывается). Все именованные секции weapon.ini или weapons.ini загружаются автоматически, поэтому можно добавлять [axe], [rifle] и другие виды без изменения загрузчика.

Запись WeaponSettings содержит Name, Type, Damage, RageBonus, RageCost, StaminaCost и CooldownMs. Ключи INI: type, damage, ragebonus, rage, stamina, cooldown. По умолчанию присутствуют knife и handgun. У нового оружия Type=None, остальные параметры равны нулю. Отсутствующие или некорректные параметры сохраняют существующее значение. Коллекция доступна для чтения; параметры существующих определений доступны для настройки.

Пример безопасного поиска:

    if (settings.Weapons.TryGetValue("axe", out var weapon))
        Console.WriteLine(weapon.Damage);

Старые свойства PunchDamage, ShotCost и другие являются обращениями к соответствующим определениям knife/handgun. GameSession копирует таблицу и каждое определение при создании, поэтому последующие изменения настроек не меняют текущий бой. Добавление в Lookup само по себе не назначает новое оружие кнопкам интерфейса.
