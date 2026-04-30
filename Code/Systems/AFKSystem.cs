// Source: gamemode/modules/afk/sv_afk.lua
// Lua: DarkRP.defineChatCommand("afk", SetAFK)
//      hook.Add("KeyPress", "DarkRPKeyReleasedCheck", AFKTimer)
//      hook.Add("playerGetSalary", "AFKGetSalary", ...)
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Система AFK: ручной переключатель + авто-демоут при бездействии.
/// Lua: gamemode/modules/afk/sv_afk.lua
/// </summary>
public sealed class AFKSystem : GameObjectSystem
{
	// Lua: GAMEMODE.Config.afkdemotetime
	private const float AfkDemoteTime = 300f; // 5 минут без движения → авто-AFK

	private const float CheckInterval = 5f;

	// steamId → (последняя позиция, время последнего движения)
	private static readonly Dictionary<ulong, (Vector3 Pos, float MoveTime)> _activity = new();

	private TimeUntil _nextCheck = CheckInterval;

	public AFKSystem( Scene scene ) : base( scene ) { }

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;
		if ( !_nextCheck ) return;
		_nextCheck = CheckInterval;

		foreach ( var conn in Connection.All )
		{
			var comp = conn.GetDarkRPComponent();
			if ( comp is null || comp.IsAFK ) continue;

			var pos = conn.Pawn?.WorldPosition ?? Vector3.Zero;

			if ( !_activity.TryGetValue( conn.SteamId, out var data ) )
			{
				_activity[conn.SteamId] = (pos, Time.Now);
				continue;
			}

			if ( pos.DistanceSquared( data.Pos ) > 4f )
			{
				_activity[conn.SteamId] = (pos, Time.Now);
			}
			else if ( Time.Now - data.MoveTime > AfkDemoteTime )
			{
				SetAFK( conn, true );
				AutoDemote( conn );
				_activity[conn.SteamId] = (pos, Time.Now);
			}
		}
	}

	// ─── Публичное API ────────────────────────────────────────────────────────

	/// <summary>
	/// Установить AFK-статус.
	/// Lua: SetAFK(ply) в sv_afk.lua
	/// </summary>
	public static void SetAFK( Connection ply, bool isAFK )
	{
		var comp = ply.GetDarkRPComponent();
		if ( comp is null || comp.IsAFK == isAFK ) return;

		comp.IsAFK = isAFK;

		if ( isAFK )
		{
			foreach ( var c in Connection.All )
				c.Notify( $"{ply.DisplayName} ушёл в AFK", NotifyType.Info, 5f );
		}
		else
		{
			_activity[ply.SteamId] = (ply.Pawn?.WorldPosition ?? Vector3.Zero, Time.Now);
			foreach ( var c in Connection.All )
				c.Notify( $"{ply.DisplayName} вернулся из AFK", NotifyType.Info, 5f );
		}

		Hook.Run( "playerSetAFK", ply, isAFK );
	}

	// ─── Автодемоут ───────────────────────────────────────────────────────────

	private static void AutoDemote( Connection ply )
	{
		var defaultJob = DarkRP.GetAllJobs().Values.FirstOrDefault();
		if ( defaultJob is null ) return;

		var cur = ply.GetJob();
		if ( cur?.Id == defaultJob.Id ) return;

		JobManager.TryChangeJob( ply, defaultJob.Id );

		foreach ( var c in Connection.All )
			c.Notify( $"{ply.DisplayName} разжалован за AFK", NotifyType.Info, 5f );

		DarkRP.Log( $"[AFK] {ply.DisplayName} авто-разжалован за бездействие ({AfkDemoteTime}s)." );
	}

	// ─── Хуки ─────────────────────────────────────────────────────────────────

	/// <summary>
	/// AFK игроки не получают зарплату.
	/// Lua: hook.Add("playerGetSalary", "AFKGetSalary", function(ply, amount) return true, "", 0 end)
	/// </summary>
	[DarkRPHook( "PlayerGetSalary" )]
	public static int? OnGetSalary( Connection ply, int salary )
	{
		if ( ply.GetDarkRPComponent()?.IsAFK == true )
			return 0;
		return null;
	}

	/// <summary>Lua: hook.Add("playerArrested", "DarkRP_AFK", unAFKPlayer)</summary>
	[DarkRPHook( "playerArrested" )]
	public static void OnArrested( Connection ply, float time, Connection? arrester ) =>
		SetAFK( ply, false );

	/// <summary>Lua: hook.Add("playerUnArrested", "DarkRP_AFK", unAFKPlayer)</summary>
	[DarkRPHook( "playerUnArrested" )]
	public static void OnUnArrested( Connection ply, Connection? actor ) =>
		SetAFK( ply, false );

	// ─── Команды ──────────────────────────────────────────────────────────────

	/// <summary>Lua: DarkRP.defineChatCommand("afk")</summary>
	[ChatCommand( "/afk", Cooldown = 5f )]
	public static void CmdAFK( Connection ply, string[] args )
	{
		if ( ply.IsArrested() )
		{
			ply.Notify( "Нельзя идти в AFK под арестом.", NotifyType.Error );
			return;
		}

		var comp = ply.GetDarkRPComponent();
		if ( comp is null ) return;

		SetAFK( ply, !comp.IsAFK );
	}
}
