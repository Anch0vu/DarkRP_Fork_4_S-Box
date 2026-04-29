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

### ~~BLOCKER-2 — Source 2 модели для entities~~ ⚠️ ОБХОД (2026-04-29)
- **Phase**: phase-4
- **Статус**: WORKAROUND (вариант C: встроенные примитивы)
- **Решение**: Используем `models/dev/box.vmdl` / `sphere.vmdl` как placeholder.
  Цвет настраивается через `ModelRenderer.Tint`, размер — через `WorldScale`.
  Логика 100% рабочая, визуал — кубики разных цветов.
- **Реализовано**:
  - `Code/Modules/Entities/EntitySpawner.cs` — `SpawnPrimitive(...)`
  - `MoneyPrinterComponent` — серый куб
  - `SpawnedShipmentComponent` — коричневый куб
  - `SpawnedWeaponComponent` — серый прямоугольник
  - `SpawnedAmmoComponent` — жёлтый куб
  - `SpawnedMoneyComponent` — зелёная плоская плитка
- **TODO phase-5+**: Заменить `BoxModel` на реальные `.vmdl` когда будут готовы.

---

### BLOCKER-3 — Source 2 модели для CS-weapons
- **Phase**: phase-5
- **Статус**: OPEN (отложено)
- **Проблема**: Все 10 CS-weapons требуют Source 2 моделей и риггинга/анимаций.
- **Текущее решение**: При покупке игрок получает `SpawnedWeaponComponent` пикап (серый куб).
  При подборе пишется уведомление + вызывается `Hook.Run("playerPickedUpWeapon", ply, weaponClass)` —
  реальный inventory будет в phase-5 после получения моделей.
- **Кто решает**: contentartist
- **Блокирует**: реальную выдачу оружия в инвентарь

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
