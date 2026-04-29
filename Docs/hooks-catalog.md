# DarkRP Hooks Catalog (Phase 0.2)

> Источник: `grep -rn "hook.Add" gamemode/ entities/`  
> Все хуки сгруппированы по типу. Для каждого указан Lua-файл, сигнатура и предлагаемый C# эквивалент.

---

## Категории

1. [Player — стандартные GMod хуки](#1-player--стандартные-gmod-хуки)
2. [Player — кастомные DarkRP хуки](#2-player--кастомные-darkrp-хуки)
3. [Entity хуки](#3-entity-хуки)
4. [Gamemode / World хуки](#4-gamemode--world-хуки)
5. [UI / Client хуки](#5-ui--client-хуки)
6. [FAdmin/FPP хуки (DROP — не портировать)](#6-fadminfpp-хуки-drop--не-портировать)

---

## 1. Player — стандартные GMod хуки

| Hook (Lua) | Сигнатура | Файл | C# эквивалент |
|---|---|---|---|
| `PlayerInitialSpawn` | `(Player ply)` | `base/sv_data.lua`, `hungermod/sv_hungermod.lua` | `[GameEvent] void OnPlayerConnected(Connection conn)` → затем spawn |
| `PlayerSpawn` | `(Player ply)` | `hungermod/sv_hungermod.lua`, `sleep/sv_sleep.lua` | `[GameEvent] void OnPlayerSpawned(PlayerController ply)` |
| `PlayerDeath` | `(Player ply, Entity inflictor, Entity killer)` | `hitmenu/sv_init.lua`, `pocket/sv_init.lua` | `[GameEvent] void OnPlayerKilled(PlayerController ply, DamageInfo info)` |
| `PlayerDisconnected` | `(Player ply)` | `base/sv_data.lua`, `hitmenu/sv_init.lua` | `[GameEvent] void OnPlayerDisconnected(Connection conn)` |
| `PlayerSay` | `(Player ply, string text, bool teamChat, bool dead)` | `chat/sv_chatcommands.lua`, `fspectate/sv_init.lua` | `DarkRP.Hook.Add("PlayerSay", ...)` или `[Rpc.Host]` |
| `PlayerAuthed` | `(Player ply, string steamId)` | `fadmin/sv_init.lua` | `[GameEvent] void OnPlayerConnected` (Steam ID доступен в S&Box) |
| `PlayerNoClip` | `(Player ply)` | `fadmin/logging` | Нет прямого аналога → `[Sync] bool NoClip` изменение |
| `PlayerEnteredVehicle` | `(Player ply, Entity vehicle)` | `fadmin/logging` | `[GameEvent] void OnVehicleEntered` |
| `PlayerLeaveVehicle` | `(Player ply, Entity vehicle)` | `fadmin/logging` | `[GameEvent] void OnVehicleLeft` |
| `PlayerStartVoice` | `(Player ply)` | `chat/cl_chatlisteners.lua` | `[GameEvent] void OnVoiceStarted` |
| `PlayerEndVoice` | `(Player ply)` | `chat/cl_chatlisteners.lua` | `[GameEvent] void OnVoiceEnded` |
| `PlayerSpray` | `(Player ply)` | `fadmin/logging` | DROP (нет спреев в S&Box) |
| `PlayerCanHearPlayersVoice` | `(Player listener, Player speaker) → bool` | `fspectate/sv_init.lua` | `[Rpc.Host] bool CanHearVoice(...)` |

---

## 2. Player — кастомные DarkRP хуки

| Hook (Lua) | Сигнатура | Файл | C# эквивалент |
|---|---|---|---|
| `playerArrested` | `(Player ply)` | `hitmenu/sv_init.lua`, `pocket/sv_init.lua`, `afk/sv_afk.lua` | `DarkRP.Hook.Run("PlayerArrested", ply)` → `[DarkRPHook("PlayerArrested")]` |
| `playerUnArrested` | `(Player ply)` | `afk/sv_afk.lua` | `DarkRP.Hook.Run("PlayerUnArrested", ply)` |
| `OnPlayerChangedTeam` | `(Player ply, int prevTeam, int newTeam)` | `hitmenu/sv_init.lua`, `sleep/sv_sleep.lua`, `afk/sv_afk.lua` | `DarkRP.Hook.Run("PlayerChangedJob", ply, oldJob, newJob)` |
| `DarkRPVarChanged` | `(Player ply, string var, any old, any new)` | `hud/cl_hud.lua`, `f4menu/cl_init.lua` | `[Sync] свойство` → автоматическая нотификация клиентам |
| `DarkRPDBInitialized` | `()` | `fadmin/kickban`, `fpp/sv_fpp.lua`, `workarounds/sv_antimultirun.lua` | `DarkRP.Hook.Run("DBInitialized")` / `IDataBackend.OnReady` |
| `playerGetSalary` | `(Player ply, int amount) → int` | `afk/sv_afk.lua` | `DarkRP.Hook.Run("PlayerGetSalary", ply, ref amount)` |
| `playerCanChangeTeam` | `(Player ply, int team) → bool` | `afk/sv_afk.lua` | `DarkRP.Hook.Run("PlayerCanChangeJob", ply, job)` |
| `playerSetAFK` | `(Player ply, bool afk)` | `hungermod/sv_hungermod.lua` | `DarkRP.Hook.Run("PlayerSetAFK", ply, afk)` |
| `playerBoughtVehicle` | `(Player ply, Entity vehicle)` | `passengermodcompat.lua` | `DarkRP.Hook.Run("PlayerBoughtVehicle", ply, vehicle)` |
| `playerBoughtCustomVehicle` | `(Player ply, string class, Entity ent)` | `passengermodcompat.lua` | `DarkRP.Hook.Run("PlayerBoughtCustomVehicle", ply, ent)` |
| `loadCustomDarkRPItems` | `()` | `base/sh_gamemode_functions.lua`, `chat/cl_chatlisteners.lua`, `animations` | `DarkRP.Hook.Run("LoadCustomItems")` — вызов после загрузки всех GameResource |
| `AFKCanChangeTeam` | `(Player ply) → bool` | `afk/sv_afk.lua` | Часть `PlayerCanChangeJob` хука |
| `playerScale` | `(Player ply)` | `playerscale/sv_playerscale.lua` | `[Sync] float Scale` изменение |
| `onJobRemoved` | `(int i, table job)` | `hitmenu/sh_init.lua` | `DarkRP.Hook.Run("JobRemoved", job)` |

---

## 3. Entity хуки

| Hook (Lua) | Сигнатура | Файл | C# эквивалент |
|---|---|---|---|
| `EntityRemoved` | `(Entity ent)` | `hitmenu/cl_init.lua`, `base/cl_entityvars.lua` | `GameObject.OnDestroy()` |
| `EntityTakeDamage` | `(Entity ent, CTakeDamageInfo info)` | `sleep/sv_sleep.lua` | `Component.OnDamage(DamageInfo info)` |
| `OnEntityCreated` | `(Entity ent)` | `workarounds/sh_workarounds.lua` | `GameObject.Awake()` / `Component.OnEnabled()` |
| `PhysgunPickup` | `(Player ply, Entity ent) → bool` | `fadmin/pickupplayers` | Нет встроенного в S&Box — реализовать через interaction |
| `GravGunPunt` | `(Player ply, Entity ent)` | `fpp/server/core.lua` | DROP (нет гравгана в S&Box) |
| `PropBreak` | `(Player attacker, Entity ent)` | `workarounds/sh_workarounds.lua` | `Component.OnBreak(...)` |
| `EntityKeyValue` | `(Entity ent, string key, string value)` | `base/sv_data.lua` | Нет аналога — данные дверей загружать из БД по entity ID |

---

## 4. Gamemode / World хуки

| Hook (Lua) | Сигнатура | Файл | C# эквивалент |
|---|---|---|---|
| `InitPostEntity` | `()` | Многие модули | `[GameEvent] void OnGameStarted()` / `ISceneStartup` |
| `PostCleanupMap` | `()` | `base/sv_data.lua`, `events/sv_events.lua` | `Scene.OnLoaded` |
| `ShutDown` | `()` | `fadmin/logging`, `workarounds/sv_antimultirun.lua` | `Application.quitting` / `Scene.OnDestroyed` |
| `DatabaseInitialized` | `()` | `fadmin/sv_fadmin_sql.lua`, `fadmin/access/sv_init.lua` | `IDataBackend.OnReady` event |
| `Think` | `()` | `animations`, `chat/cl_chatlisteners.lua`, `mysqlite` | `Component.Update()` (избегать — предпочесть таймеры) |
| `PostCleanupMap` | `()` | `events/sv_events.lua` | `[GameEvent] void SceneLoaded()` |

---

## 5. UI / Client хуки

| Hook (Lua) | Сигнатура | Файл | C# эквивалент |
|---|---|---|---|
| `HUDPaint` | `()` | `hud/cl_hud.lua`, `fpp/client/hud.lua`, `hitmenu/cl_init.lua` | Razor `.razor` компонент (рендер автоматический) |
| `CalcView` | `(Player ply, Vector origin, Angle angles, float fov) → table` | `deathpov/cl_init.lua`, `fspectate/cl_init.lua` | `CameraComponent` overrides / `SceneCamera` |
| `PlayerBindPress` | `(Player ply, string bind, bool pressed) → bool` | `f1menu`, `f4menu`, `hobo`, `fspectate` | `Input.Pressed(InputAction)` в Razor/Component |
| `StartChat` | `()` | `chat/cl_chatlisteners.lua` | Chat open event в Razor |
| `FinishChat` | `()` | `chat/cl_chatlisteners.lua` | Chat close event в Razor |
| `ChatTextChanged` | `(string text)` | `chat/cl_chatlisteners.lua` | `@oninput` в Razor chat component |
| `PostPlayerDraw` | `(Player ply)` | `hitmenu/cl_init.lua`, `chatindicator/cl_init.lua` | `Component.DrawGizmos()` / WorldPanel |
| `ShouldDrawLocalPlayer` | `() → bool` | `fspectate/cl_init.lua` | `PlayerController.ThirdPerson` property |
| `RenderScreenspaceEffects` | `()` | `fspectate/cl_init.lua` | Post-process component в S&Box |
| `ScoreboardShow` | `()` | `fadmin/scoreboard` | Razor component visibility toggle |
| `ScoreboardHide` | `()` | `fadmin/scoreboard` | Razor component visibility toggle |
| `PreDrawHalos` | `()` | `weapon_keypadchecker/cl_init.lua` | `Gizmo.Draw` в component |
| `KeyPress` | `(Player ply, int key)` | `hitmenu/cl_init.lua`, `animations/sh_animations.lua` | `Input.Pressed(...)` в компоненте |
| `DarkRPVarChanged` | `(Player ply, string var, any old, any new)` | `hud/cl_hud.lua`, `f4menu/cl_init.lua` | `[Sync]` property автонотификация |
| `SendLaws` | `()` | DarkRP custom | `[Rpc.Broadcast] void ReceiveLaws(string[] laws)` |
| `agendaHUD` | `()` | `hud/cl_hud.lua` | Razor HUD binding |

---

## 6. FAdmin/FPP хуки (DROP — не портировать)

Следующие хуки используются только в `fadmin/` и `fpp/` модулях, которые помечены `DROP`:

```
FAdmin_Log, FAdmin, FAdmin_ragdoll, FAdmin_jailed, FAdmin_jail,
FAdmin_scoreboard, FAdmin_RestrictWeapons, FAdmin_PickUpPlayers,
FAdmin_Bans, FAdmin_Chat_autocomplete, FAdmin_ShowFAdminMenu,
CAMI.OnUsergroupRegistered, CAMI.OnUsergroupUnregistered,
CAMI.PlayerUsergroupChanged, CAMI.SteamIDUsergroupChanged,
FPP_SpawnEffect, FPP_Load_CAMI, FPPMenus
```

---

## Сводная таблица — DarkRP-специфичные хуки для C# реализации

Это хуки, которые должны быть реализованы в `Code/DarkRP/Hook.cs`:

```csharp
// Список DarkRP.Hook.Run(...) вызовов
"PlayerInitialSpawn"      // (PlayerController ply)
"PlayerSpawn"             // (PlayerController ply)
"PlayerDeath"             // (PlayerController ply, DamageInfo info)
"PlayerDisconnected"      // (Connection conn)
"PlayerSay"               // (PlayerController ply, string text) → bool (блокировать?)
"PlayerChangedJob"        // (PlayerController ply, Job oldJob, Job newJob)
"PlayerArrested"          // (PlayerController ply, float time, PlayerController arrester)
"PlayerUnArrested"        // (PlayerController ply, PlayerController actor)
"PlayerWanted"            // (PlayerController ply, PlayerController actor, string reason)
"PlayerUnWanted"          // (PlayerController ply, PlayerController actor)
"PlayerGetSalary"         // (PlayerController ply, ref int amount)
"PlayerCanChangeJob"      // (PlayerController ply, Job job) → bool
"PlayerSetAFK"            // (PlayerController ply, bool afk)
"PlayerBoughtEntity"      // (PlayerController ply, string entityClass)
"PlayerBoughtShipment"    // (PlayerController ply, Shipment shipment)
"PlayerBoughtVehicle"     // (PlayerController ply, GameObject vehicle)
"DBInitialized"           // ()
"LoadCustomItems"         // ()  — вызывать после загрузки всех GameResource ассетов
"JobRemoved"              // (Job job)
```
