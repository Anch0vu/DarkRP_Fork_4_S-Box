// Source: gamemode/modules/fspectate/sv_init.lua
// Lua: concommand.Add("FSpectate", Spectate) — admin spectator mode
//      net.Receive("FSpectateTarget") — целью становится указанный игрок
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Система слежения (FSpectate) для администраторов.
/// Lua: gamemode/modules/fspectate/sv_init.lua
/// Упрощённый порт: храним кто за кем следит, клиентская камера сама обрабатывает позицию цели.
/// </summary>
public static class SpectateSystem
{
	// spectator.SteamId → target Connection
	private static readonly Dictionary<ulong, Connection?> _spectating = new();

	// ─── Публичное API ────────────────────────────────────────────────────────

	public static Connection? GetSpectateTarget( Connection ply ) =>
		_spectating.TryGetValue( ply.SteamId, out var t ) ? t : null;

	public static bool IsSpectating( Connection ply ) =>
		_spectating.ContainsKey( ply.SteamId ) && _spectating[ply.SteamId] is not null;

	/// <summary>Lua: startSpectating(ply, target)</summary>
	public static void Start( Connection ply, Connection target )
	{
		// Lua: hook.Call("FSpectate_canSpectate", nil, ply, target)
		var canResult = Hook.Run( "FSpectate_canSpectate", ply, target );
		if ( canResult is false ) return;

		_spectating[ply.SteamId] = target;
		ply.Notify( $"Вы наблюдаете за {target.DisplayName}.", NotifyType.Info );
		Hook.Run( "FSpectate_start", ply, target );
		DarkRP.Log( $"[Spectate] {ply.DisplayName} начал наблюдение за {target.DisplayName}" );
	}

	/// <summary>Lua: endSpectate(ply)</summary>
	public static void Stop( Connection ply )
	{
		if ( !_spectating.Remove( ply.SteamId, out var target ) ) return;
		ply.Notify( "Слежение прекращено.", NotifyType.Info );
		Hook.Run( "FSpectate_stop", ply, target );
	}

	// ─── Хуки ─────────────────────────────────────────────────────────────────

	[DarkRPHook( "PlayerDisconnected" )]
	public static void OnDisconnect( Connection ply )
	{
		_spectating.Remove( ply.SteamId );

		// Если за этим игроком кто-то следил — остановить слежение
		var watchers = _spectating
			.Where( kv => kv.Value?.SteamId == ply.SteamId )
			.Select( kv => kv.Key )
			.ToList();

		foreach ( var watcherId in watchers )
		{
			var watcher = Connection.All.FirstOrDefault( c => c.SteamId == watcherId );
			if ( watcher is not null ) Stop( watcher );
		}
	}

	// ─── Команды ──────────────────────────────────────────────────────────────

	/// <summary>Lua: concommand.Add("FSpectate", Spectate)</summary>
	[ChatCommand( "/spectate", Cooldown = 1f )]
	public static void CmdSpectate( Connection ply, string[] args )
	{
		if ( !ply.IsHost )
		{
			ply.Notify( "Только для администраторов.", NotifyType.Error );
			return;
		}

		if ( args.Length == 0 )
		{
			ply.Notify( "Использование: /spectate <игрок>", NotifyType.Error );
			return;
		}

		var name = string.Join( " ", args );
		var target = Connection.All.FirstOrDefault( c =>
			c.DisplayName.Contains( name, System.StringComparison.OrdinalIgnoreCase ) );

		if ( target is null )
		{
			ply.Notify( $"Игрок '{name}' не найден.", NotifyType.Error );
			return;
		}

		if ( target.SteamId == ply.SteamId )
		{
			ply.Notify( "Нельзя наблюдать за самим собой.", NotifyType.Error );
			return;
		}

		Start( ply, target );
	}

	/// <summary>Lua: concommand.Add("FSpectate_StopSpectating", endSpectate)</summary>
	[ChatCommand( "/stopspectate", Cooldown = 0.5f )]
	[ChatCommand( "/endspectate", Cooldown = 0.5f )]
	public static void CmdStopSpectate( Connection ply, string[] args ) => Stop( ply );
}
