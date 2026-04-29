# DarkRP for s&box (C# port) — Work In Progress

> ⚠️ Эта ветка (`csharp-port`) — порт DarkRP с Lua/Garry's Mod на C#/s&box.
> `master` остаётся оригинальным Lua-кодом FPtje DarkRP и используется как read-only reference.

## Статус

В разработке. Phase-0 (аудит) → см. [`Docs/module-status.md`](Docs/module-status.md).

## Структура репозитория

```
.
├── gamemode/        ← оригинал Lua (FPtje DarkRP), read-only reference
├── entities/        ← оригинал Lua entities/weapons, read-only reference
├── content/         ← оригинал ассетов GMod, read-only reference
│
├── Code/            ← C# код порта
│   ├── DarkRP/      ← API-фасад (Hook, Job, Player extensions, Notify)
│   ├── Modules/     ← порты модулей (один файл/папка на модуль)
│   ├── Systems/     ← GameObjectSystem-менеджеры
│   ├── Entities/    ← Component'ы для бывших Lua entities
│   ├── Weapons/     ← Component'ы для SWEP'ов
│   ├── UI/          ← Razor-компоненты (HUD, F4Menu, Chat)
│   └── Player/      ← PlayerController, расширения
├── Resources/       ← s&box ассеты (.job, .shipment GameResource)
├── Docs/            ← документация порта
└── sbox-darkrp-port-plan.json  ← мастер-план для Claude Code
```

## Порт делается через Claude Code

План разбит на фазы в `sbox-darkrp-port-plan.json`. Запуск:

```bash
git checkout csharp-port
# в папке репо:
claude-code "Прочитай sbox-darkrp-port-plan.json и выполни phase-0 согласно плану."
```

После каждой фазы — ревью, коммит, переход к следующей.

## Целевая аудитория кода порта

Кодер с 7-летним опытом Lua и минимальным C# — поэтому API-фасад намеренно мимикрирует под Lua DarkRP:

```csharp
// Регистрация работы — как DarkRP.addJob в Lua
DarkRP.AddJob(new Job {
    Name = "Полицейский",
    Salary = 500,
    MaxPlayers = 8,
    Color = Color.Blue,
});

// Хуки — как hook.Add
[DarkRPHook("PlayerSpawn")]
static void OnSpawn(Player ply) {
    ply.AddMoney(100);
}

// Деньги — как ply:addMoney в Lua
player.AddMoney(500);
```

См. [`Docs/migration-guide-for-lua-devs.md`](Docs/migration-guide-for-lua-devs.md) после генерации.

## Команда

- **Anch0vu** — lead, admin
- **Coder** — Lua dev (7 years)
- **Mapper** — `rp_NY_city` + новые районы под s&box

## Юридика

- Базовый Lua-код: MIT (FPtje DarkRP)
- C# порт: MIT (наследуется)
- Любые модули, импортированные из сторонних форков (например, Доброград от Octothorp), требуют отдельного письменного разрешения авторов на портирование в другой движок. Lua-разрешение ≠ C#-разрешение.

---

# DarkRP (original, Lua / Garry's Mod) ![run-glualint](https://github.com/FPtje/DarkRP/workflows/run-glualint/badge.svg?branch=master)

A roleplay gamemode for Garry's Mod.

## Getting DarkRP
Please use either git or the workshop.
Manually downloading DarkRP or using SVN is possible, but not recommended.

The workshop version of DarkRP can be found here:

https://steamcommunity.com/sharedfiles/filedetails/?id=248302805

## Modifying DarkRP
Check out the wiki!

https://darkrp.miraheze.org/wiki/Main_Page

Make sure to download the DarkRPMod:

https://github.com/FPtje/darkrpmodification

Do you want to create a gamemode based on DarkRP?
You probably shouldn't. If you insist, use the derived gamemode that can be downloaded here:

https://github.com/FPtje/DarkRP/releases/tag/derived

Just whatever you do, don't touch DarkRP's core files.

## Getting help
Please head to the official Discord!

https://darkrp.page.link/discord
