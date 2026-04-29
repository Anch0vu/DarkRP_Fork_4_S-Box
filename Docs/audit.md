# DarkRP → S&Box Port — Аудит исходника (Phase 0.1 / 0.6)

> Дата: 2026-04-29  
> Ветка: `claude/darkrp-phase-0-kPK5G`  
> Источник: Lua-исходники в `gamemode/` и `entities/` — только для чтения

---

## 1. Общая статистика

| Параметр | Значение |
|---|---|
| Всего файлов `.lua` | 240 |
| Всего строк | 50 545 |
| Модулей в `gamemode/modules/` | 32 |
| Entities (`entities/entities/`) | 20 |
| Weapons (`entities/weapons/`) | 23 |
| Уникальных хуков (`hook.Add`) | ~107 |
| Сетевых сообщений (`util.AddNetworkString`) | ~83 |

---

## 2. Структура репозитория

```
DarkRP_Fork_4_S-Box/
├── gamemode/
│   ├── init.lua                    # Server entrypoint
│   ├── cl_init.lua                 # Client entrypoint
│   ├── config/
│   │   ├── config.lua              # 544 lines — основной конфиг сервера
│   │   ├── jobrelated.lua          # 304 lines — конфиг, связанный с job'ами
│   │   ├── addentities.lua         # список entity/shipment/vehicle регистраций
│   │   ├── ammotypes.lua           # типы боеприпасов
│   │   ├── licenseweapons.lua      # оружие по лицензии
│   │   └── _MySQL.lua              # MySQL конфиг (DROP — не нужен в S&Box)
│   ├── libraries/
│   │   ├── fn.lua                  # 337 lines — функциональные утилиты (map/filter/compose)
│   │   ├── simplerr.lua            # 557 lines — расширенные Lua ошибки
│   │   ├── tablecheck.lua          # 364 lines — валидация таблиц
│   │   ├── disjointset.lua         # структура данных
│   │   ├── interfaceloader.lua     # загрузчик интерфейсов
│   │   ├── modificationloader.lua  # загрузчик модификаций
│   │   ├── sh_cami.lua             # 362 lines — CAMI permission framework (DROP)
│   │   └── mysqlite/mysqlite.lua   # 519 lines — SQLite/MySQL ORM (DROP)
│   └── modules/
│       ├── base/                   # 8784 lines (23 файла) — ядро DarkRP
│       ├── fadmin/                 # 7951 lines (87 файлов) — FAdmin (DROP)
│       ├── fpp/                    # 4577 lines (13 файлов) — Prop Protection (DROP)
│       ├── doorsystem/             # 2706 lines (8 файлов) — система дверей
│       ├── police/                 # 1842 lines (5 файлов) — полиция/арест
│       ├── f4menu/                 # 1435 lines (7 файлов) — F4 меню
│       ├── chat/                   # 1185 lines (8 файлов) — чат
│       ├── hitmenu/                # 1107 lines (7 файлов) — система хитов
│       ├── language/               # 879 lines (3 файла) — локализация
│       ├── jobs/                   # 789 lines (4 файла) — смена работы
│       ├── tipjar/                 # 684 lines (4 файла) — чаевые
│       ├── fspectate/              # 687 lines (3 файла) — наблюдение
│       ├── hungermod/              # 566 lines (8 файлов) — голод
│       ├── money/                  # 576 lines (5 файлов) — деньги
│       ├── voting/                 # 720 lines (5 файлов) — голосование
│       ├── hud/                    # 505 lines (4 файла) — HUD
│       ├── f1menu/                 # 435 lines (8 файлов) — F1 меню
│       ├── positions/              # 450 lines (5 файлов) — позиции/тюрьма
│       ├── workarounds/            # 421 lines (3 файла) — патчи GMod (DROP)
│       ├── dermaskin/              # 249 lines (2 файла) — скины Derma (DROP)
│       ├── events/                 # 237 lines (2 файла) — события (метеоры)
│       ├── afk/                    # 230 lines (4 файла) — AFK система
│       ├── sleep/                  # 302 lines (3 файла) — сон/нокаут
│       ├── deathpov/               # 44 lines (1 файл) — вид от трупа
│       ├── hobo/                   # 40 lines (2 файла) — бродяга
│       ├── medic/                  # 16 lines (2 файла) — медик
│       ├── logging/                # 97 lines (3 файла) — логирование
│       ├── animations/             # 189 lines (2 файла) — анимации
│       ├── playerscale/            # 33 lines (2 файла) — масштаб игрока
│       ├── darkrpmessages/         # 26 lines (1 файл) — сообщения DarkRP
│       ├── chatindicator/          # 88 lines (2 файла) — индикатор чата
│       ├── cppi/                   # 56 lines (1 файл) — CPPI (DROP)
│       └── chatsounds.lua          # 377 lines — звуки чата
├── entities/
│   ├── entities/                   # 20 entities (см. секцию 3)
│   └── weapons/                   # 23 weapons (см. секцию 4)
└── Docs/                           # Документация порта (создаётся на phase-0)
```

---

## 3. Entities (20 штук)

| Entity | PrintName | Описание | Приоритет порта | Нужна модель |
|---|---|---|---|---|
| `money_printer` | Money Printer | Печатает деньги со временем, требует обслуживания | HIGH | Да |
| `spawned_money` | Spawned Money | Подбираемый пак денег на полу | HIGH | Да (coin/bill) |
| `spawned_weapon` | Spawned Weapon | Оружие, купленное через F4 | HIGH | Нет (берёт модель оружия) |
| `spawned_shipment` | Shipment | Ящик с оружием, нужно открывать | HIGH | Да (crate) |
| `spawned_food` | Spawned Food | Еда, купленная через F4 | MED | Нет (берёт модель еды) |
| `spawned_ammo` | Spawned Ammo | Патроны, купленные через F4 | MED | Нет |
| `drug_lab` | Drug Lab | Производство наркотиков | MED | Да (lab equipment) |
| `drug` | Drugs | Подбираемые наркотики | MED | Да |
| `gunlab` | Gun Lab | Производство оружия | MED | Да |
| `microwave` | Microwave | Разогрев еды (hungermod) | LOW | Да |
| `food` | Food | Базовая еда (hungermod) | LOW | Нет (берёт модель) |
| `darkrp_tip_jar` | Tip Jar | Банка чаевых для бродяги | LOW | Да |
| `lab_base` | Lab | Базовый класс для drug_lab/gunlab | — | — |
| `darkrp_billboard` | DarkRP Billboard | Рекламный щит | LOW | Да |
| `darkrp_cheque` | Cheque | Чек (перевод денег) | LOW | Нет |
| `darkrp_laws` | DarkRP Laws | Доска законов мэра | MED | Нет |
| `letter` | Letter | Письмо (записка) | LOW | Нет |
| `meteor` | Meteor | Метеор (событие) | LOW | Да |
| `fadmin_jail` | fadmin_jail | Тюремная клетка FAdmin | DROP | — |
| `fadmin_motd` | fadmin MOTD | MOTD-экран FAdmin | DROP | — |

**Blockers**: Все HIGH-приоритетные entities требуют Source 2 моделей — зависит от `contentartist`.  
Записать в `Docs/blockers.md`.

---

## 4. Weapons (23 штуки)

### DarkRP-специфичные (не CS-оружие)

| Weapon | PrintName | Описание | C# Target | Нужна анимация |
|---|---|---|---|---|
| `arrest_stick` | Arrest Baton | Дубинка для ареста | `Component ArrestBaton` | Нет (re-use stun) |
| `unarrest_stick` | Unarrest Baton | Дубинка для освобождения | `Component UnarrestBaton` | Нет |
| `stunstick` | Stun Stick | Шокер | `Component StunStick` | Нет |
| `door_ram` | Battering Ram | Выбивает двери | `Component DoorRam` | Да |
| `lockpick` | Lock Pick | Вскрывает замки | `Component Lockpick` | Да |
| `keys` | Keys | Ключи от дверей | `Component Keys` | Нет |
| `pocket` | Pocket | Карман (хранение предметов) | `Component Pocket` | Нет |
| `med_kit` | Medic Kit | Аптечка медика | `Component MedKit` | Нет |
| `weaponchecker` | Weapon Checker | Проверка оружия у игрока | `Component WeaponChecker` | Нет |
| `weapon_keypadchecker` | Admin Keypad Checker | Отладчик кодовых панелей | `Component KeypadChecker` | Нет |
| `ls_sniper` | Silenced Sniper | Бесшумная снайперка | `Component LSSniperRifle` | Да |
| `stick_base` | (base) | Базовый класс дубинок | — | — |
| `gmod_tool` | (tool) | GMod тулган (обёртка) | DROP | — |

### CS-оружие (оболочки над HL2-оружием)

| Weapon | PrintName | C# Target |
|---|---|---|
| `weapon_ak472` | AK47 | `Component Rifle_AK47` |
| `weapon_cs_base2` | (base) | базовый класс |
| `weapon_deagle2` | Deagle | `Component Pistol_Deagle` |
| `weapon_fiveseven2` | FiveSeven | `Component Pistol_FiveSeven` |
| `weapon_glock2` | Glock | `Component Pistol_Glock` |
| `weapon_m42` | M4 | `Component Rifle_M4` |
| `weapon_mac102` | Mac10 | `Component SMG_Mac10` |
| `weapon_mp52` | MP5 | `Component SMG_MP5` |
| `weapon_p2282` | P228 | `Component Pistol_P228` |
| `weapon_pumpshotgun2` | Pump Shotgun | `Component Shotgun_Pump` |

**Blockers**: Все CS-weapons требуют Source 2 моделей и анимаций — зависит от `contentartist`.

---

## 5. Топ-50 крупнейших файлов

| Файл | Строк |
|---|---|
| `gamemode/modules/base/sh_interface.lua` | 1527 |
| `gamemode/modules/base/sv_interface.lua` | 1325 |
| `gamemode/modules/fpp/pp/client/menu.lua` | 1268 |
| `gamemode/modules/base/sv_gamemode_functions.lua` | 1147 |
| `gamemode/modules/fpp/pp/server/settings.lua` | 988 |
| `gamemode/modules/base/sh_createitems.lua` | 922 |
| `gamemode/modules/doorsystem/sv_interface.lua` | 851 |
| `gamemode/modules/fpp/pp/server/core.lua` | 721 |
| `gamemode/modules/base/sh_checkitems.lua` | 628 |
| `gamemode/modules/base/sv_data.lua` | 627 |
| `gamemode/modules/doorsystem/sv_doors.lua` | 592 |
| `gamemode/modules/fpp/pp/server/ownability.lua` | 587 |
| `gamemode/modules/language/sh_english.lua` | 576 |
| `gamemode/libraries/simplerr.lua` | 557 |
| `gamemode/config/config.lua` | 544 |
| `gamemode/libraries/mysqlite/mysqlite.lua` | 519 |
| `gamemode/modules/jobs/sv_jobs.lua` | 516 |
| `entities/weapons/weapon_cs_base2/shared.lua` | 516 |
| `gamemode/modules/police/sv_interface.lua` | 503 |
| `gamemode/modules/base/sv_purchasing.lua` | 464 |

---

## 6. Зависимости конфигурации

Файлы в `gamemode/config/` **НЕ являются модулями** — это пользовательские данные:

| Файл | Содержимое | C# эквивалент |
|---|---|---|
| `config.lua` | ~100 конфиг-переменных сервера | `Resources/Config/ServerConfig.asset` |
| `jobrelated.lua` | Описания работ (DarkRP.createJob) | `Resources/Jobs/*.job` (GameResource) |
| `addentities.lua` | Регистрации entity/shipment/vehicle | `Resources/Items/*.item` |
| `ammotypes.lua` | Типы патронов | `Resources/Ammo/*.ammo` |
| `licenseweapons.lua` | Оружие, требующее лицензию | поле в Job.cs |
| `_MySQL.lua` | MySQL-настройки | DROP — заменить на IDataBackend |

---

## 7. Следующий шаг

После phase-0 необходимо подтверждение:
- **decision-1**: какой backend БД использовать (HTTP REST / S&Box Services / SQLite)
- **decision-2**: юридическая верификация форка (MIT лицензия ванильного FPtje/DarkRP — ок, но уточнить про Доброград)
