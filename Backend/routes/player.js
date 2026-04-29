// REST эндпоинты для данных игрока.
// Источник логики: gamemode/modules/base/sv_data.lua
const express = require('express');
const { pool } = require('../db');
const router = express.Router();

// ── GET /player/:steamId ─────────────────────────────────────────────────────
// Lua: loadPlayerData(ply) — загрузка при подключении
router.get('/:steamId', async (req, res) => {
  const steamId = BigInt(req.params.steamId);
  const [rows] = await pool.query(
    'SELECT steam_id, display_name, money, job_id, has_license, hunger FROM players WHERE steam_id = ?',
    [steamId]
  );

  if (rows.length === 0) {
    return res.status(404).json({ error: 'player_not_found' });
  }

  const row = rows[0];
  res.json({
    steamId:     row.steam_id.toString(),
    displayName: row.display_name,
    money:       row.money,
    jobId:       row.job_id,
    hasGunLicense: row.has_license === 1,
    hunger:      row.hunger,
  });
});

// ── POST /player ─────────────────────────────────────────────────────────────
// Создать нового игрока при первом подключении
router.post('/', async (req, res) => {
  const { steamId, displayName, money, jobId, hasGunLicense, hunger } = req.body;
  if (!steamId) return res.status(400).json({ error: 'steamId required' });

  await pool.query(
    `INSERT INTO players (steam_id, display_name, money, job_id, has_license, hunger)
     VALUES (?, ?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE display_name = VALUES(display_name)`,
    [BigInt(steamId), displayName || '', money ?? 500, jobId ?? 'citizen', hasGunLicense ? 1 : 0, hunger ?? 100]
  );

  res.status(201).json({ ok: true });
});

// ── PUT /player/:steamId ─────────────────────────────────────────────────────
// Lua: savePlayerData(ply) — сохранение при отключении
router.put('/:steamId', async (req, res) => {
  const steamId = BigInt(req.params.steamId);
  const { displayName, money, jobId, hasGunLicense, hunger } = req.body;

  await pool.query(
    `INSERT INTO players (steam_id, display_name, money, job_id, has_license, hunger)
     VALUES (?, ?, ?, ?, ?, ?)
     ON DUPLICATE KEY UPDATE
       display_name = VALUES(display_name),
       money        = VALUES(money),
       job_id       = VALUES(job_id),
       has_license  = VALUES(has_license),
       hunger       = VALUES(hunger)`,
    [steamId, displayName || '', money ?? 0, jobId ?? 'citizen', hasGunLicense ? 1 : 0, hunger ?? 100]
  );

  res.json({ ok: true });
});

// ── GET /player/:steamId/kv/:key ─────────────────────────────────────────────
// Lua: DarkRP.DB.GetPlayerData(steamId, key)
router.get('/:steamId/kv/:key', async (req, res) => {
  const steamId = BigInt(req.params.steamId);
  const { key } = req.params;

  const [rows] = await pool.query(
    'SELECT kv_value FROM player_kv WHERE steam_id = ? AND kv_key = ?',
    [steamId, key]
  );

  if (rows.length === 0) return res.status(404).json(null);

  try {
    res.json(JSON.parse(rows[0].kv_value));
  } catch {
    res.json(rows[0].kv_value);
  }
});

// ── PUT /player/:steamId/kv/:key ─────────────────────────────────────────────
// Lua: DarkRP.DB.SetPlayerData(steamId, key, value)
router.put('/:steamId/kv/:key', async (req, res) => {
  const steamId = BigInt(req.params.steamId);
  const { key } = req.params;
  const value = JSON.stringify(req.body.value);

  await pool.query(
    `INSERT INTO player_kv (steam_id, kv_key, kv_value) VALUES (?, ?, ?)
     ON DUPLICATE KEY UPDATE kv_value = VALUES(kv_value)`,
    [steamId, key, value]
  );

  res.json({ ok: true });
});

module.exports = router;
