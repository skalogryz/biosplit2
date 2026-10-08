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

## Инвентарь персонажа

Каждый Character создаёт собственный CharacterInventory. В нём два опциональных слота: Weapon1 и Weapon2 типа WeaponItem (null означает пустой слот). WeaponItem.Definition ссылается на конкретное определение WeaponSettings из Lookup, а Ammo хранится отдельно для каждого экземпляра предмета. Ammo по умолчанию равен 0, пока не ограничивает стрельбу и не расходуется.

Пример:

    var hero = new Character("hero");
    hero.Inventory.Weapon1 = new WeaponItem(settings.Weapons["knife"]);
    hero.Inventory.Weapon2 = new WeaponItem(settings.Weapons["handgun"], ammo: 12);

Текущий персонаж доступен через GameSession.Player. Его стартовые предметы — knife и handgun; определения берутся из копии Lookup, принадлежащей сессии. Другие персонажи имеют отдельные инвентари; единого инвентаря на GameSession нет. Punch/Shoot используют Weapon1/Weapon2 и общий API StartAttack.

## Жизненный цикл атаки

StartAttack(WeaponItem) возвращает AttackResult.Success, если атака принята. CanStartAttack(item) проверяет состояние, кулдаун, ресурсы и тип; None и null недопустимы. Одновременно возможна одна атака. Melee недоступен во время отскока; Firearm доступен. Тип оружия определяет анимационное событие Punch/Shot и остановку регенерации выносливости на время кулдауна Melee. Ammo по-прежнему не учитывается.

Стоимость ярости и выносливости списывается один раз при старте. Кулдаун назначается экземпляру WeaponItem после завершения атаки, а не при старте. Урон и RageBonus берутся из снимка определения WeaponItem и применяются один раз при завершении; изменение Definition во время атаки не меняет её результат.

WeaponSettings.AttackDurationMs (INI: attackduration, миллисекунды) задаёт задержку до урона. По умолчанию 0: атака начинается и завершается в StartAttack. Для длительной атаки время движется через Tick; инвентарь приостанавливает его. AttackRemaining, ActiveWeapon и IsAttacking позволяют отслеживать состояние.

CancelAttack() возвращает true только если была активная атака. Отменённая атака не нанесёт урон и не даст бонус; уже списанные ресурсы не возвращаются; отмена также назначает кулдаун предмету. Повторная или поздняя отмена возвращает false без новых событий. Блок, поражение и отскок для Melee отменяют незавершённую атаку.

События: AttackStarted, AttackCancelled, AttackCompleted. В GameEvent.Weapon передаётся предмет; AttackCompleted.Damage содержит фактически нанесённый урон. У завершённой атаки не возникает AttackCancelled, у отменённой — AttackCompleted. Существующие Punch/Shot и события урона сохранены для UI.

## Кулдауны предметов и причины отказа

WeaponItem.Cooldown — оставшийся кулдаун decimal в секундах. Продолжительность по-прежнему берётся из WeaponSettings.CooldownMs (INI cooldown, миллисекунды). Два предмета одного определения имеют независимые кулдауны. При завершении или отмене атаки кулдаун назначается только использованному предмету. WeaponOnCooldown запрещает его запуск; другое готовое оружие доступно, если активной атаки нет.

GameSession.Characters содержит зарегистрированных персонажей, включая Player; AddCharacter(character) добавляет другого персонажа. Tick уменьшает кулдаун всех предметов из обоих слотов всех этих персонажей, с ограничением снизу 0. Один и тот же предмет, указанный в нескольких слотах, обновляется один раз. Предмет вне инвентарей не получает обновления кулдауна. Инвентарь и поражение по-прежнему приостанавливают игровое время. Выносливость игрока не растёт, пока хотя бы одно из носимых им Melee-оружий находится на кулдауне.

AttackResult: Success, NoWeapon, UnsupportedWeaponType, GameOver, InventoryOpen, Blocking, NoEnemy, AttackInProgress, WeaponOnCooldown, Dodging, InsufficientRage, InsufficientStamina. При нескольких причинах возвращается первая по порядку проверки. GetAttackStartResult(item) позволяет получить причину без запуска и без расхода ресурсов; CanStartAttack(item) — булева проверка. Punch/Shoot сохраняют возврат bool для совместимости с UI.

    AttackResult result = game.StartAttack(game.Player.Inventory.Weapon1);
    if (result == AttackResult.WeaponOnCooldown)
        Console.WriteLine("Оружие ещё не готово");
