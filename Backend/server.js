// DarkRP S&Box — Node.js REST API Backend
// Посредник между S&Box (C# Http.RequestAsync) и MySQL.
// Запуск: npm start (production) / npm run dev (разработка с nodemon)
//
// Переменные окружения: см. .env.example
// Lua-эквивалент: gamemode/libraries/mysqlite/mysqlite.lua + sv_data.lua
require('dotenv').config();

const express = require('express');
const { initSchema } = require('./db');
const playerRouter = require('./routes/player');
const doorsRouter  = require('./routes/doors');

const app  = express();
const PORT = parseInt(process.env.API_PORT || '3000');

// ── Middleware ────────────────────────────────────────────────────────────────
app.use(express.json());

// Простое логирование запросов
app.use((req, _res, next) => {
  console.log(`[${new Date().toISOString()}] ${req.method} ${req.path}`);
  next();
});

// Глобальный обработчик ошибок (не даёт серверу упасть при SQL ошибках)
app.use((err, _req, res, _next) => {
  console.error('[DarkRP API Error]', err);
  res.status(500).json({ error: 'internal_error', message: err.message });
});

// ── Health check ─────────────────────────────────────────────────────────────
// DataManager.cs вызывает GET /health при старте сервера
app.get('/health', (_req, res) => {
  res.json({ status: 'ok', time: new Date().toISOString() });
});

// ── Роуты ────────────────────────────────────────────────────────────────────
app.use('/player', playerRouter);
app.use('/doors',  doorsRouter);

// ── Запуск ───────────────────────────────────────────────────────────────────
async function main() {
  try {
    await initSchema();
    app.listen(PORT, () => {
      console.log(`[DarkRP API] Сервер запущен на http://localhost:${PORT}`);
      console.log(`[DarkRP API] MySQL: ${process.env.DB_HOST}/${process.env.DB_NAME}`);
    });
  } catch (err) {
    console.error('[DarkRP API] Ошибка запуска:', err);
    process.exit(1);
  }
}

main();
