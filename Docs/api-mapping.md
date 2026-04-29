# DarkRP API Mapping: Lua → C# (Phase 0.3)

> Цель: каждый Lua-разработчик с 7 годами опыта DarkRP должен видеть знакомые вызовы в C#.  
> Источник: `gamemode/modules/base/sh_createitems.lua`, `sh_interface.lua`, `sv_interface.lua` и другие.

---

## Содержание

1. [DarkRP.AddJob / createJob](#1-darkrpaddjob--createjob)
2. [DarkRP.createShipment](#2-darkrpcreateship​ment)
3. [DarkRP.createEntity](#3-darkrpcreateentity)
4. [DarkRP.createVehicle](#4-darkrpcreatevehicle)
5. [DarkRP.createAmmoType](#5-darkrpcreateammotype)
6. [DarkRP.createAgenda](#6-darkrpcreateagenda)
7. [DarkRP.createFood (hungermod)](#7-darkrpcreatef​ood-hungermod)
8. [DarkRP.defineChatCommand](#8-darkrpdefinechatcommand)
9. [DarkRP.notify](#9-darkrpnotify)
10. [DarkRP.formatMoney](#10-darkrpformatmoney)
11. [DarkRP.log](#11-darkrplog)
12. [Player Extensions (ply:метод)](#12-player-extensions-plyметод)
13. [DarkRP.ENTITY extensions](#13-darkrpentity-extensions)
14. [Дополнительные утилиты](#14-дополнительные-утилиты)

---

## 1. DarkRP.AddJob / createJob

### Lua (оригинал)
```lua
-- gamemode/modules/base/sh_createitems.lua:412
DarkRP.createJob("Police Officer", {
    color = Color(25, 25, 170),
    model = {"models/player/police.mdl"},
    description = "Keep the town safe.",
    weapons = {"arrest_stick", "stunstick"},
    command = "police",
    max = 4,
    salary = 65,
    admin = 0,
    vote = false,
    hasLicense = true,
    candemote = true,
    cp = true
})
```

### C# (эквивалент)
```csharp
// Code/DarkRP/DarkRP.cs
// Lua: DarkRP.createJob(name, table)
DarkRP.AddJob(new Job
{
    Id = "police",
    Name = "Police Officer",
    Color = Color.FromBytes(25, 25, 170),
    Model = "models/citizen_police.vmdl",
    Description = "Keep the town safe.",
    Weapons = new List<string> { "arrest_stick", "stunstick" },
    MaxPlayers = 4,
    Salary = 65,
    AdminOnly = false,
    VoteRequired = false,
    HasGunLicense = true,
    IsCP = true,
    CanBeDemoted = true,
});
```

### GameResource вариант (для хранения в `.job` файлах)
```csharp
// Resources/Jobs/police.job
// Заполняется через S&Box Editor, не через код
```

---

## 2. DarkRP.createShipment

### Lua (оригинал)
```lua
-- gamemode/modules/base/sh_createitems.lua:514
DarkRP.createShipment("AK47 Shipment", {
    model = "models/weapons/w_rif_ak47.mdl",
    entity = "weapon_ak472",
    price = 2500,
    amount = 10,
    separate = true,
    pricesep = 400,
    noship = false,
    allowed = {TEAM_GUN_DEALER},
})
```

### C# (эквивалент)
```csharp
// Lua: DarkRP.createShipment(name, table)
DarkRP.AddShipment(new Shipment
{
    Id = "ak47_shipment",
    Name = "AK47 Shipment",
    Model = "models/weapons/w_rif_ak47.vmdl",
    EntityClass = "weapon_ak472",
    Price = 2500,
    Amount = 10,
    SeparatelyBuyable = true,
    SeparatePrice = 400,
    AllowedJobs = new List<string> { "gun_dealer" },
});
```

---

## 3. DarkRP.createEntity

### Lua (оригинал)
```lua
-- gamemode/modules/base/sh_createitems.lua:608
DarkRP.createEntity("Money Printer", {
    ent = "money_printer",
    model = "models/props_c17/consolebox01a.mdl",
    price = 200,
    max = 4,
    cmd = "buyprinter",
    allowed = {TEAM_HOBO, TEAM_CITIZEN},
})
```

### C# (эквивалент)
```csharp
// Lua: DarkRP.createEntity(name, table)
DarkRP.AddBuyableEntity(new BuyableEntity
{
    Id = "money_printer",
    Name = "Money Printer",
    EntityClass = "money_printer",
    Model = "models/props/consolebox.vmdl",
    Price = 200,
    MaxPerPlayer = 4,
    Command = "buyprinter",
    AllowedJobs = new List<string> { "hobo", "citizen" },
});
```

---

## 4. DarkRP.createVehicle

### Lua (оригинал)
```lua
DarkRP.createVehicle("Jeep", {
    model = "models/buggy.mdl",
    price = 3500,
    allowed = {TEAM_CITIZEN},
})
```

### C# (эквивалент)
```csharp
// Lua: DarkRP.createVehicle(name, table)
DarkRP.AddVehicle(new BuyableVehicle
{
    Id = "jeep",
    Name = "Jeep",
    Model = "models/buggy.vmdl",
    Price = 3500,
    AllowedJobs = new List<string> { "citizen" },
});
```

---

## 5. DarkRP.createAmmoType

### Lua (оригинал)
```lua
-- gamemode/modules/base/sh_createitems.lua:726
DarkRP.createAmmoType("pistol", {
    name = "Pistol Ammo",
    model = "models/items/boxsrounds.mdl",
    price = 100,
    amountGiven = 30,
})
```

### C# (эквивалент)
```csharp
// Lua: DarkRP.createAmmoType(ammoType, table)
DarkRP.AddAmmoType(new AmmoType
{
    Id = "pistol",
    Name = "Pistol Ammo",
    Model = "models/items/boxsrounds.vmdl",
    Price = 100,
    AmountGiven = 30,
});
```

---

## 6. DarkRP.createAgenda

### Lua (оригинал)
```lua
-- gamemode/modules/base/sh_createitems.lua:658
DarkRP.createAgenda("The Agenda", TEAM_MAYOR, {TEAM_POLICE, TEAM_SWAT})
```

### C# (эквивалент)
```csharp
// Lua: DarkRP.createAgenda(title, managerJob, listenerJobs)
DarkRP.AddAgenda(new Agenda
{
    Title = "The Agenda",
    ManagerJobId = "mayor",
    ListenerJobIds = new List<string> { "police", "swat" },
});
```

---

## 7. DarkRP.createFood (hungermod)

### Lua (оригинал)
```lua
-- gamemode/modules/hungermod/sh_init.lua:5
DarkRP.createFood("Sandwich", "models/food/sandwich.mdl", 30, 50)
```

### C# (эквивалент)
```csharp
// Lua: DarkRP.createFood(name, model, energy, price)
DarkRP.AddFood(new FoodItem
{
    Name = "Sandwich",
    Model = "models/food/sandwich.vmdl",
    HungerRestored = 30,
    Price = 50,
});
```

---

## 8. DarkRP.defineChatCommand

### Lua (оригинал)
```lua
-- gamemode/modules/chat/sv_chat.lua:9
DarkRP.defineChatCommand("give", function(ply, args)
    -- логика
end, 0.2) -- cooldown 0.2s
```

### C# — атрибутный вариант (рекомендуется)
```csharp
// Lua: DarkRP.defineChatCommand("give", callback)
[ChatCommand("/give")]
public static void CmdGive(PlayerController ply, string[] args)
{
    // логика
}
```

### C# — программный вариант
```csharp
// Lua: DarkRP.defineChatCommand("give", callback, cooldown)
DarkRP.AddChatCommand("/give", (ply, args) =>
{
    // логика
}, cooldown: 0.2f);
```

---

## 9. DarkRP.notify

### Lua (оригинал)
```lua
-- Используется повсеместно
DarkRP.notify(ply, 0, 4, "You bought a door!")   -- type 0 = info (зелёный)
DarkRP.notify(ply, 1, 4, "You can't afford it!")  -- type 1 = error (красный)
DarkRP.notify(ply, 2, 4, "You received a hit!")   -- type 2 = warning (жёлтый)
DarkRP.notify(ply, 3, 4, "Tax day!")              -- type 3 = purple
DarkRP.notify(ply, 4, 4, "Payday!")               -- type 4 = money/special
```

### C# (эквивалент)
```csharp
// Lua: DarkRP.notify(ply, type, duration, message)
// Метод-расширение на PlayerController:
ply.Notify("You bought a door!", NotifyType.Info, duration: 4f);
ply.Notify("You can't afford it!", NotifyType.Error, duration: 4f);
ply.Notify("Tax day!", NotifyType.Warning, duration: 4f);
ply.Notify("Payday!", NotifyType.Money, duration: 4f);

// Или через статический API (Lua-совместимый стиль):
// Lua: DarkRP.notify(ply, 0, 4, "msg")
DarkRP.Notify(ply, NotifyType.Info, 4f, "You bought a door!");
```

### NotifyType enum
```csharp
public enum NotifyType { Info = 0, Error = 1, Warning = 2, Purple = 3, Money = 4 }
```

---

## 10. DarkRP.formatMoney

### Lua (оригинал)
```lua
DarkRP.formatMoney(1500)  -- "$1,500"
```

### C# (эквивалент)
```csharp
// Lua: DarkRP.formatMoney(amount)
DarkRP.FormatMoney(1500);  // "$1,500"
// или:
1500.FormatMoney();        // Extension method
```

---

## 11. DarkRP.log

### Lua (оригинал)
```lua
-- gamemode/modules/logging/sv_logging.lua:20
DarkRP.log("Player bought a printer", Color(0,255,0))
```

### C# (эквивалент)
```csharp
// Lua: DarkRP.log(text, colour)
DarkRP.Log("Player bought a printer", Color.Green);
```

---

## 12. Player Extensions (ply:метод)

Оригинальные Lua методы реализованы через `plyMeta` (метатаблица Player).  
В C# — extension methods на `PlayerController` + `[Sync]` свойства.

| Lua (ply:метод) | Файл | C# Extension Method |
|---|---|---|
| `ply:addMoney(amount)` | `money/sv_interface.lua` | `ply.AddMoney(int amount)` |
| `ply:getMoney()` | `money/sh_interface.lua` | `ply.GetMoney()` → `int` |
| `ply:canAfford(amount)` | `money/sh_interface.lua` | `ply.CanAfford(int amount)` → `bool` |
| `ply:setMoney(amount)` | `money/sv_interface.lua` | `ply.SetMoney(int amount)` |
| `ply:getJobTable()` | `base/sh_createitems.lua:393` | `ply.GetJob()` → `Job` |
| `ply:getDarkRPVar("var")` | `base/sh_interface.lua:388` | `ply.GetDarkRPVar<T>(string key)` |
| `ply:setDarkRPVar("var", val)` | `base/sv_interface.lua:606` | `ply.SetDarkRPVar<T>(string key, T val)` |
| `ply:isCP()` | `police/sh_init.lua:18` | `ply.IsCP()` → `bool` |
| `ply:isMayor()` | `police/sh_init.lua:22` | `ply.IsMayor()` → `bool` |
| `ply:isChief()` | `police/sh_init.lua:23` | `ply.IsChief()` → `bool` |
| `ply:isArrested()` | `police/sh_init.lua:6` | `ply.IsArrested()` → `bool` |
| `ply:isWanted()` | `police/sh_init.lua:10` | `ply.IsWanted()` → `bool` |
| `ply:isHitman()` | `hitmenu/sh_init.lua:5` | `ply.IsHitman()` → `bool` |
| `ply:isMedic()` | `medic/sh_init.lua:2` | `ply.IsMedic()` → `bool` |
| `ply:isCook()` | `hungermod/sh_init.lua:32` | `ply.IsCook()` → `bool` |
| `ply:getAgenda()` | `base/sh_createitems.lua:649` | `ply.GetAgenda()` → `Agenda?` |
| `ply:sendDoorData()` | `doorsystem/sv_doorvars.lua` | `ply.SendDoorData()` — Rpc.Owner |
| `ply:keysUnOwnAll()` | `doorsystem/sv_interface.lua` | `ply.UnOwnAllDoors()` |

### Реализация синхронизации через [Sync]
```csharp
// В Code/Player/DarkRPPlayerComponent.cs (Component на PlayerController)
[Sync] public int Money { get; set; }
[Sync] public string JobId { get; set; }
[Sync] public bool IsArrestedSync { get; set; }
[Sync] public bool IsWantedSync { get; set; }
[Sync] public int ArrestTimeRemaining { get; set; }
[Sync] public bool HasGunLicense { get; set; }
[Sync] public float Hunger { get; set; }  // hungermod
```

---

## 13. DarkRP.ENTITY extensions

Методы на Entity (двери, транспорт):

| Lua (ent:метод) | Файл | C# |
|---|---|---|
| `ent:keysOwn(ply)` | `doorsystem/sv_interface.lua` | `door.Own(PlayerController ply)` |
| `ent:keysUnOwn(ply)` | `doorsystem/sv_interface.lua` | `door.UnOwn(PlayerController ply)` |
| `ent:isKeysOwned()` | `doorsystem/sh_interface.lua` | `door.IsOwned()` → `bool` |
| `ent:isKeysOwnable()` | `doorsystem/sh_interface.lua` | `door.IsOwnable()` → `bool` |
| `ent:getDoorOwner()` | `doorsystem/sh_interface.lua` | `door.GetOwner()` → `PlayerController?` |
| `ent:isMasterOwner(ply)` | `doorsystem/sh_interface.lua` | `door.IsMasterOwner(ply)` → `bool` |
| `ent:addKeysDoorOwner(ply)` | `doorsystem/sv_interface.lua` | `door.AddOwner(ply)` |
| `ent:removeKeysDoorOwner(ply)` | `doorsystem/sv_interface.lua` | `door.RemoveOwner(ply)` |
| `ent:keysLock()` | `doorsystem/sv_interface.lua` | `door.Lock()` |
| `ent:keysUnLock()` | `doorsystem/sv_interface.lua` | `door.Unlock()` |
| `ent:isLocked()` | `doorsystem/sv_interface.lua` | `door.IsLocked()` → `bool` |
| `ent:setKeysNonOwnable(bool)` | `doorsystem/sv_interface.lua` | `door.SetNonOwnable(bool)` |
| `ent:setKeysTitle(string)` | `doorsystem/sv_interface.lua` | `door.SetTitle(string)` |
| `ent:isDoor()` | `doorsystem/sh_interface.lua` | `go.HasComponent<DoorComponent>()` |

---

## 14. Дополнительные утилиты

| Lua | Файл | C# |
|---|---|---|
| `DarkRP.createMoneyBag(pos, amount)` | `money/sv_money.lua:41` | `DarkRP.SpawnMoney(Vector3 pos, int amount)` |
| `DarkRP.payPlayer(ply1, ply2, amount)` | `money/sv_money.lua:17` | `DarkRP.PayPlayer(PlayerController from, PlayerController to, int amount)` |
| `DarkRP.lockdown(ply)` | `police/sv_commands.lua:142` | `DarkRP.StartLockdown(PlayerController mayor)` |
| `DarkRP.unLockdown(ply)` | `police/sv_commands.lua:178` | `DarkRP.EndLockdown(PlayerController mayor)` |
| `DarkRP.arrestedPlayers()` | `police/sv_init.lua:136` | `DarkRP.GetArrestedPlayers()` → `IEnumerable<PlayerController>` |
| `DarkRP.arrestedPlayerCount()` | `police/sv_init.lua:146` | `DarkRP.GetArrestedPlayerCount()` → `int` |
| `DarkRP.iterateArrestedPlayers()` | `police/sv_init.lua:118` | `DarkRP.GetArrestedPlayers()` (итерация) |
| `DarkRP.addHitmanTeam(job)` | `hitmenu/sh_init.lua:21` | `DarkRP.AddHitmanJob(string jobId)` |
| `DarkRP.getAvailableVehicles()` | `workarounds/sh_workarounds.lua:3` | `DarkRP.GetVehicles()` → `List<BuyableVehicle>` |
| `DarkRP.getDoorVars()` | `doorsystem/sh_doors.lua:139` | `DoorManager.GetDoorVars()` |
| `DarkRP.registerDoorVar(name, ...)` | `doorsystem/sh_doors.lua:142` | `DoorManager.RegisterVar(string name, ...)` |
| `DarkRP.registerDarkRPVar(name, ...)` | `base/sh_entityvars.lua:10` | `[Sync]` свойство на PlayerComponent |
| `DarkRP.openF4Menu()` | `f4menu/cl_init.lua:6` | F4Menu Razor component toggle |
| `DarkRP.openF1Menu()` | `f1menu/cl_f1menu.lua:14` | F1Menu Razor component toggle |
| `DarkRP.openHitMenu(hitman)` | `hitmenu/cl_menu.lua:220` | HitMenu Razor component |
| `DarkRP.addF4MenuTab(name, panel, order)` | `f4menu/cl_init.lua:34` | `F4Menu.AddTab(string name, RenderFragment content, int order)` |
| `DarkRP.addChatReceiver(prefix, text, fn)` | `chat/cl_chatlisteners.lua:24` | `ChatSystem.AddReceiver(string prefix, ...)` |
| `DarkRP.getPhrase(key, ...)` | Повсеместно | `DarkRP.Translate(string key, params object[] args)` |
| `DarkRP.createFood(name, mdl, energy, price)` | `hungermod/sh_init.lua:5` | `DarkRP.AddFood(FoodItem)` |

---

## Итог: Структура C# фасада

```
Code/DarkRP/
├── DarkRP.cs          — статический класс, все DarkRP.* методы
├── Hook.cs            — DarkRP.Hook.Add/Run/Remove
├── Job.cs             — GameResource [job] + DarkRP.AddJob
├── Shipment.cs        — DarkRP.AddShipment
├── BuyableEntity.cs   — DarkRP.AddBuyableEntity
├── BuyableVehicle.cs  — DarkRP.AddVehicle
├── AmmoType.cs        — DarkRP.AddAmmoType
├── Agenda.cs          — DarkRP.AddAgenda
├── FoodItem.cs        — DarkRP.AddFood (hungermod)
├── ChatCommand.cs     — [ChatCommand] атрибут
├── Notify.cs          — NotifyType enum + отправка
└── PlayerExtensions.cs — extension methods на PlayerController
```
