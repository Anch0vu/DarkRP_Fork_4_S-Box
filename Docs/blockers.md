# Blockers (Phase 0+)

> Файл обновляется при обнаружении блокеров. При разрешении блокера — отметить дату и решение.

---

## Открытые блокеры

### ~~BLOCKER-1 — Выбор backend БД~~ ✅ РЕШЕНО (2026-04-29)
- **Phase**: phase-1
- **Статус**: RESOLVED
- **Решение**: Node.js (Express) + mysql2 → MySQL. S&Box делает HTTP запросы к `Backend/server.js`.
- **Реализовано**:
  - `Backend/server.js` — Express сервер
  - `Backend/db.js` — MySQL пул + авто-создание схемы
  - `Backend/routes/player.js` — CRUD данных игрока
  - `Backend/routes/doors.js` — данные дверей
  - `Code/Systems/DataManager.cs` — C# клиент к API

---

### BLOCKER-2 — Source 2 модели для entities
- **Phase**: phase-4
- **Статус**: OPEN
- **Проблема**: Все HIGH-приоритетные entities требуют .vmdl моделей в Source 2 формате:
  - `money_printer` — принтер денег
  - `spawned_money` — монеты/купюры на полу
  - `spawned_shipment` — ящик с оружием
  - `drug_lab` — лаборатория
  - `gunlab` — оружейная мастерская
  - `darkrp_tip_jar` — банка чаевых
  - `darkrp_billboard` — рекламный щит
- **Кто решает**: mapper/contentartist
- **Блокирует**: phase-4, финальный геймплей

---

### BLOCKER-3 — Source 2 модели для CS-weapons
- **Phase**: phase-4
- **Статус**: OPEN
- **Проблема**: Все 10 CS-weapons (`weapon_ak472`, `weapon_deagle2`, и т.д.) требуют Source 2 моделей и риггинга/анимаций. DarkRP-специфичные weapons (arrest_stick, lockpick, door_ram) могут переиспользовать базовые S&Box анимации, но нужны уточнения.
- **Кто решает**: contentartist
- **Блокирует**: phase-4

---

### BLOCKER-4 — Юридическая верификация
- **Phase**: phase-0
- **Статус**: OPEN (некритично для начала порта)
- **Вопрос**: Лицензия ванильного FPtje/DarkRP — MIT, это ок для порта. Но если в будущем планируется добавлять модули из сторонних DarkRP-серверов (например, Доброград), нужно подтверждение что это разрешено.
- **Кто решает**: Anch0vu
- **Блокирует**: Только специфические модули из сторонних источников

---

## Решённые блокеры

*(пусто)*
