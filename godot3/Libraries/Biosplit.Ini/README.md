# Biosplit.Ini

Самостоятельная C# библиотека для чтения INI, target framework netstandard2.0. Не зависит от Godot и классов игры.

Сборка: `dotnet build Biosplit.Ini.csproj`.

```csharp
using Biosplit.Ini;

var ini = IniDocument.Load("weapons.ini");
int damage = ini.GetInt("knife", "damage", fallback: 12);
bool found = ini.TryGetInt("handgun", "rage", out int rage);
string name = ini.GetString("knife", "name", "Knife");
```

`Parse(TextReader)` позволяет читать строки или другие источники. Поддерживаются секции, ключи без учёта регистра, целые числа Int32 и комментарии `;` / `#`. Повторяющийся ключ использует последнее значение. Пустые и некорректные integer значения возвращают fallback. Ошибки чтения файла передаются вызывающему коду.

В игре библиотека подключена через ProjectReference. Script WeaponConfiguration определяет путь cfg/weapons.ini и применяет значения к Main.
