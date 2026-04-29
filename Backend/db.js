// Подключение к MySQL через пул соединений.
// Аналог: gamemode/libraries/mysqlite/mysqlite.lua (но без Lua-специфики)
require('dotenv').config();
const mysql = require('mysql2/promise');

const pool = mysql.createPool({
  host:     process.env.DB_HOST     || 'localhost',
  port:     parseInt(process.env.DB_PORT || '3306'),
  user:     process.env.DB_USER     || 'darkrp',
  password: process.env.DB_PASSWORD || '',
  database: process.env.DB_NAME     || 'darkrp_sbox',
  waitForConnections: true,
  connectionLimit: 10,
  queueLimit: 0,
  charset: 'utf8mb4',
});

// Создать таблицы при первом запуске (если не существуют)
// Lua: sv_data.lua createTables()
async function initSchema() {
  const conn = await pool.getConnection();
  try {
    await conn.query(`
      CREATE TABLE IF NOT EXISTS players (
        steam_id     BIGINT UNSIGNED NOT NULL PRIMARY KEY,
        display_name VARCHAR(64)     NOT NULL DEFAULT '',
        money        INT             NOT NULL DEFAULT 500,
        job_id       VARCHAR(64)     NOT NULL DEFAULT 'citizen',
        has_license  TINYINT(1)      NOT NULL DEFAULT 0,
        hunger       FLOAT           NOT NULL DEFAULT 100,
        created_at   TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP,
        updated_at   TIMESTAMP       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
        INDEX idx_updated (updated_at)
      ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
    `);

    // Key-value хранилище для произвольных данных игрока
    // Lua: DarkRP.DB.SetPlayerData / GetPlayerData
    await conn.query(`
      CREATE TABLE IF NOT EXISTS player_kv (
        steam_id   BIGINT UNSIGNED NOT NULL,
        kv_key     VARCHAR(128)    NOT NULL,
        kv_value   TEXT            NOT NULL DEFAULT '',
        PRIMARY KEY (steam_id, kv_key),
        FOREIGN KEY (steam_id) REFERENCES players(steam_id) ON DELETE CASCADE
      ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
    `);

    // Данные дверей
    // Lua: sv_data.lua door ownership tables
    await conn.query(`
      CREATE TABLE IF NOT EXISTS door_ownership (
        map_name   VARCHAR(128) NOT NULL,
        door_index INT          NOT NULL,
        steam_id   BIGINT UNSIGNED,
        title      VARCHAR(128) NOT NULL DEFAULT '',
        non_ownable TINYINT(1)  NOT NULL DEFAULT 0,
        PRIMARY KEY (map_name, door_index)
      ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
    `);

    console.log('[DarkRP DB] Схема инициализирована.');
  } finally {
    conn.release();
  }
}

module.exports = { pool, initSchema };
