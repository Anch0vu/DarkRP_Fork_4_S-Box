// Source: gamemode/modules/positions/sv_spawnpos.lua
// Source: gamemode/modules/positions/sv_database.lua
// Lua: DarkRP.storeTeamSpawnPos(team, pos), DarkRP.addTeamSpawnPos, DarkRP.removeTeamSpawnPos
//      definePrivilegedChatCommand("setspawn"), ("addspawn"), ("removespawn")
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Кастомные позиции спавна для работ.
/// Lua: gamemode/modules/positions/sv_spawnpos.lua
/// </summary>
public static class SpawnPositionSystem
{
	// jobId → список позиций (для нескольких спавнов на работу)
	private static readonly Dictionary<string, List<Vector3>> _spawnPositions = new();

	// ─── Публичное API ────────────────────────────────────────────────────────

	/// <summary>Lua: DarkRP.storeTeamSpawnPos(team, pos) — сбросить и установить одну.</summary>
	public static void StoreTeamSpawnPos( string jobId, Vector3 pos )
	{
		_spawnPositions[jobId] = new List<Vector3> { pos };
	}

	/// <summary>Lua: DarkRP.addTeamSpawnPos(team, pos) — добавить ещё одну.</summary>
	public static void AddTeamSpawnPos( string jobId, Vector3 pos )
	{
		if ( !_spawnPositions.TryGetValue( jobId, out var list ) )
		{
			list = new List<Vector3>();
			_spawnPositions[jobId] = list;
		}
		list.Add( pos );
	}

	/// <summary>Lua: DarkRP.removeTeamSpawnPos(team) — удалить все позиции работы.</summary>
	public static void RemoveTeamSpawnPos( string jobId ) =>
		_spawnPositions.Remove( jobId );

	/// <summary>
	/// Получить случайную позицию спавна для работы.
	/// Lua: DarkRP.retrieveTeamSpawnPos(team)
	/// </summary>
	public static Vector3? GetSpawnPos( string jobId )
	{
		if ( !_spawnPositions.TryGetValue( jobId, out var list ) || list.Count == 0 )
			return null;
		return list[Game.Random.Int( 0, list.Count - 1 )];
	}

	public static int CountForJob( string jobId ) =>
		_spawnPositions.TryGetValue( jobId, out var list ) ? list.Count : 0;

	// ─── Хуки ─────────────────────────────────────────────────────────────────

	/// <summary>
	/// Применить кастомную позицию спавна, если есть для текущей работы.
	/// Lua: hook.Add("PlayerSpawn", "DarkRPSpawnPos", ...)
	/// </summary>
	[DarkRPHook( "PlayerSpawn" )]
	public static void OnPlayerSpawn( Connection ply )
	{
		var jobId = ply.GetDarkRPComponent()?.JobId;
		if ( string.IsNullOrEmpty( jobId ) ) return;

		var pos = GetSpawnPos( jobId );
		if ( pos is null || ply.Pawn is null ) return;

		ply.Pawn.WorldPosition = pos.Value;
	}

	// ─── Команды ──────────────────────────────────────────────────────────────

	private static Job? FindJobByCommand( string command )
	{
		return DarkRP.GetAllJobs().Values.FirstOrDefault( j =>
			j.Commands.Any( c => string.Equals( c, command, System.StringComparison.OrdinalIgnoreCase ) ) ??
			string.Equals( j.Id, command, System.StringComparison.OrdinalIgnoreCase ) );
	}

	/// <summary>Lua: definePrivilegedChatCommand("setspawn")</summary>
	[ChatCommand( "/setspawn", Cooldown = 1.5f )]
	public static void CmdSetSpawn( Connection ply, string[] args )
	{
		if ( !ply.IsHost )
		{
			ply.Notify( "Только для администраторов.", NotifyType.Error );
			return;
		}
		if ( args.Length == 0 )
		{
			ply.Notify( "Использование: /setspawn <команда работы>", NotifyType.Error );
			return;
		}

		var job = FindJobByCommand( args[0] );
		if ( job is null )
		{
			ply.Notify( LanguageSystem.Get( "could_not_find", args[0] ), NotifyType.Error );
			return;
		}

		var pos = ply.Pawn?.WorldPosition ?? Vector3.Zero;
		StoreTeamSpawnPos( job.Id, pos );
		ply.Notify( LanguageSystem.Get( "updated_spawnpos", job.Name ), NotifyType.Info );
		DarkRP.Log( $"[Spawn] {ply.DisplayName} установил спавн для {job.Name} в {pos}" );
	}

	/// <summary>Lua: definePrivilegedChatCommand("addspawn")</summary>
	[ChatCommand( "/addspawn", Cooldown = 1.5f )]
	public static void CmdAddSpawn( Connection ply, string[] args )
	{
		if ( !ply.IsHost )
		{
			ply.Notify( "Только для администраторов.", NotifyType.Error );
			return;
		}
		if ( args.Length == 0 )
		{
			ply.Notify( "Использование: /addspawn <команда работы>", NotifyType.Error );
			return;
		}

		var job = FindJobByCommand( args[0] );
		if ( job is null )
		{
			ply.Notify( LanguageSystem.Get( "could_not_find", args[0] ), NotifyType.Error );
			return;
		}

		var pos = ply.Pawn?.WorldPosition ?? Vector3.Zero;
		AddTeamSpawnPos( job.Id, pos );

		var count = CountForJob( job.Id );
		ply.Notify( LanguageSystem.Get( "created_spawnpos", job.Name ) + $" (всего: {count})", NotifyType.Info );
	}

	/// <summary>Lua: definePrivilegedChatCommand("removespawn")</summary>
	[ChatCommand( "/removespawn", Cooldown = 1.5f )]
	public static void CmdRemoveSpawn( Connection ply, string[] args )
	{
		if ( !ply.IsHost )
		{
			ply.Notify( "Только для администраторов.", NotifyType.Error );
			return;
		}
		if ( args.Length == 0 )
		{
			ply.Notify( "Использование: /removespawn <команда работы>", NotifyType.Error );
			return;
		}

		var job = FindJobByCommand( args[0] );
		if ( job is null )
		{
			ply.Notify( LanguageSystem.Get( "could_not_find", args[0] ), NotifyType.Error );
			return;
		}

		RemoveTeamSpawnPos( job.Id );
		ply.Notify( LanguageSystem.Get( "remove_spawnpos", job.Name ), NotifyType.Info );
	}
}
