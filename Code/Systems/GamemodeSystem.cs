// Source: gamemode/init.lua — server entrypoint
// Source: gamemode/cl_init.lua — client entrypoint
// Source: gamemode/modules/base/sv_gamemode_functions.lua
// Главный GameObjectSystem, запускающий DarkRP при старте сцены.
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Точка входа DarkRP. Инициализирует все подсистемы.
/// Lua: gamemode/init.lua (SERVER) и gamemode/cl_init.lua (CLIENT)
/// </summary>
public sealed class GamemodeSystem : GameObjectSystem
{
	public GamemodeSystem( Scene scene ) : base( scene )
	{
		Listen( Stage.StartUpdate, 0, OnFirstFrame, "DarkRP_Init" );
	}

	private bool _initialized = false;

	private void OnFirstFrame()
	{
		if ( _initialized ) return;
		_initialized = true;

		// Lua: DarkRP.DARKRP_LOADING = true ... DarkRP.finish()
		DarkRP.Initialize();

		// Загружаем данные из БД после инициализации
		if ( Networking.IsHost )
			_ = DataManager.OnGameStartAsync();

		// Lua: hook.Run("InitPostEntity")
		Hook.Run( "InitPostEntity" );

		Log.Info( "[DarkRP] GamemodeSystem ready." );
	}

	// ─── Player connect / disconnect ────────────────────────────────────────

	[GameEvent.Player.Connected]
	private static void OnPlayerConnected( Connection conn )
	{
		// Создать DarkRP компонент для игрока
		// TODO: найти PlayerController GameObject для этого соединения (phase-2)

		// Загрузить данные из БД
		if ( Networking.IsHost )
			_ = DataManager.LoadPlayerAsync( conn );

		// Lua: hook.Run("PlayerInitialSpawn", ply)
		Hook.Run( "PlayerInitialSpawn", conn );
	}

	[GameEvent.Player.Disconnected]
	private static void OnPlayerDisconnected( Connection conn )
	{
		// Сохранить данные в БД
		if ( Networking.IsHost )
			_ = DataManager.SavePlayerAsync( conn );

		// Lua: hook.Run("PlayerDisconnected", ply)
		Hook.Run( "PlayerDisconnected", conn );
	}

	[GameEvent.Player.Spawned]
	private static void OnPlayerSpawned( PlayerController ply )
	{
		// Гарантируем наличие ArmorComponent на пешке (создаём, если нет)
		// Lua: ply:SetArmor(0) при спавне
		if ( ply.GameObject.GetComponent<ArmorComponent>() is null )
			ply.GameObject.Components.Create<ArmorComponent>();

		// Lua: hook.Run("PlayerSpawn", ply)
		Hook.Run( "PlayerSpawn", ply.Connection );
	}

	[GameEvent.Player.Killed]
	private static void OnPlayerKilled( PlayerController ply )
	{
		// Lua: hook.Run("PlayerDeath", ply, inflictor, attacker)
		// TODO (phase-9+): извлечь attacker из DamageInfo через S&Box hook
		Hook.Run( "PlayerDeath", ply.Connection, (Connection?)null );
	}
}
