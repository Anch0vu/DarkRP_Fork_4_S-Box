// REST эндпоинты для данных дверей.
// Источник логики: gamemode/modules/doorsystem/sv_data.lua / sv_doors.lua
const express = require('express');
const { pool } = require('../db');
const router = express.Router();

// ── GET /doors/:map ──────────────────────────────────────────────────────────
// Lua: hook.Add("InitPostEntity", ...) → загрузка дверей карты
router.get('/:map', async (req, res) => {
  const [rows] = await pool.query(
    'SELECT door_index, steam_id, title, non_ownable FROM door_ownership WHERE map_name = ?',
    [req.params.map]
  );
  res.json(rows.map(r => ({
    doorIndex:  r.door_index,
    steamId:    r.steam_id?.toString() ?? null,
    title:      r.title,
    nonOwnable: r.non_ownable === 1,
  })));
});

// ── PUT /doors/:map/:doorIndex ────────────────────────────────────────────────
// Lua: ent:keysOwn(ply) / ent:setKeysTitle(title) → сохранение в БД
router.put('/:map/:doorIndex', async (req, res) => {
  const { steamId, title, nonOwnable } = req.body;
  const { map, doorIndex } = req.params;

  await pool.query(
    `INSERT INTO door_ownership (map_name, door_index, steam_id, title, non_ownable)
     VALUES (?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       steam_id    = VALUES(steam_id),
       title       = VALUES(title),
       non_ownable = VALUES(non_ownable)`,
    [map, parseInt(doorIndex), steamId ? BigInt(steamId) : null, title || '', nonOwnable ? 1 : 0]
  );

  res.json({ ok: true });
});

// ── DELETE /doors/:map/:doorIndex ─────────────────────────────────────────────
// Lua: ent:keysUnOwn(ply) → удалить владельца
router.delete('/:map/:doorIndex', async (req, res) => {
  await pool.query(
    'UPDATE door_ownership SET steam_id = NULL, title = "" WHERE map_name = ? AND door_index = ?',
    [req.params.map, parseInt(req.params.doorIndex)]
  );
  res.json({ ok: true });
});

// ── DELETE /doors/:map/owner/:steamId ─────────────────────────────────────────
// Lua: ply:keysUnOwnAll() → продать все двери игрока
router.delete('/:map/owner/:steamId', async (req, res) => {
  const steamId = BigInt(req.params.steamId);
  const [result] = await pool.query(
    'UPDATE door_ownership SET steam_id = NULL, title = "" WHERE map_name = ? AND steam_id = ?',
    [req.params.map, steamId]
  );
  res.json({ ok: true, affected: result.affectedRows });
});

module.exports = router;
