// Source: gamemode/modules/police/sv_init.lua
// Source: gamemode/modules/police/sv_commands.lua
// Source: gamemode/modules/police/sh_init.lua
// Lua: ply:arrest(), ply:unArrest(), ply:wanted(), ply:unWanted()
//      DarkRP.defineChatCommand("wanted", ...), ("arrest", ...), ("lockdown", ...)
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Полицейская система — арест, розыск, ордера, лицензии, блокировка.
/// Lua: gamemode/modules/police/sv_init.lua + sv_commands.lua
/// </summary>
public static class PoliceSystem
{
	// Lua: GAMEMODE.Config.jailtimer
	public const float DefaultArrestTime = 120f;
	// Lua: GAMEMODE.Config.wantedtime
	public const float DefaultWantedTime = 240f;

	// Позиции тюрьмы — задаются через /jailpos (хранятся host-side)
	private static readonly List<Vector3> _jailPositions = new();

	public static int JailPosCount() => _jailPositions.Count;
	public static Vector3? GetRandomJailPos() =>
		_jailPositions.Count > 0 ? _jailPositions[System.Random.Shared.Next( _jailPositions.Count )] : null;

	// ─── /wanted <name> <reason> — объявить в розыск ─────────────────────────
	[ChatCommand( "/wanted", Cooldown = 1f )]
	public static void CmdWanted( Connection ply, string[] args )
	{
		if ( !ply.IsCP() )
		{
			ply.Notify( LanguageSystem.Get( "need_to_be_cp" ), NotifyType.Error );
			return;
		}
		if ( args.Length < 2 )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var target = FindPlayer( args[0] );
		if ( target is null )
		{
			ply.Notify( LanguageSystem.Get( "could_not_find", args[0] ), NotifyType.Error );
			return;
		}

		var hookResult = Hook.Run( "canWanted", target, ply );
		if ( hookResult is false ) return;

		var reason = string.Join( " ", args.Skip( 1 ) );
		Wanted( target, ply, reason );
	}

	// ─── /unwanted <name> — снять розыск ─────────────────────────────────────
	[ChatCommand( "/unwanted", Cooldown = 1f )]
	public static void CmdUnwanted( Connection ply, string[] args )
	{
		if ( !ply.IsCP() )
		{
			ply.Notify( LanguageSystem.Get( "need_to_be_cp" ), NotifyType.Error );
			return;
		}
		if ( args.Length == 0 )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var target = FindPlayer( args[0] );
		if ( target is null )
		{
			ply.Notify( LanguageSystem.Get( "could_not_find", args[0] ), NotifyType.Error );
			return;
		}

		var comp = target.GetDarkRPComponent();
		if ( comp is null || !comp.IsWanted )
		{
			ply.Notify( LanguageSystem.Get( "not_wanted" ), NotifyType.Error );
			return;
		}

		UnWanted( target, ply );
	}

	// ─── /911, /cr, /999, /112, /000 — вызов полиции ─────────────────────────
	[ChatCommand( "/911", Cooldown = 1.5f )]
	[ChatCommand( "/cr", Cooldown = 1.5f )]
	[ChatCommand( "/999", Cooldown = 1.5f )]
	[ChatCommand( "/112", Cooldown = 1.5f )]
	[ChatCommand( "/000", Cooldown = 1.5f )]
	public static void CmdCombineRequest( Connection ply, string[] args )
	{
		var text = string.Join( " ", args );
		if ( string.IsNullOrWhiteSpace( text ) )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var color = ply.GetJob()?.Color ?? Color.White;
		var prefix = $"{LanguageSystem.Get( "request" )} {ply.DisplayName}";
		var msgColor = new Color( 1f, 0f, 0f );

		foreach ( var conn in Connection.All )
		{
			if ( !conn.IsCP() && conn != ply ) continue;
			ChatMessage.SendToPlayer( conn, color, prefix, msgColor, text );
		}
	}

	// ─── /givelicense <name> — выдать лицензию ────────────────────────────────
	[ChatCommand( "/givelicense", Cooldown = 0.5f )]
	public static void CmdGiveLicense( Connection ply, string[] args )
	{
		if ( args.Length == 0 )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var hookResult = Hook.Run( "canGiveLicense", ply, null );
		if ( hookResult is false )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", "/givelicense" ), NotifyType.Error );
			return;
		}

		// Иерархия: мэр > шеф > CP
		if ( !ply.IsMayor() && !ply.IsChief() && !ply.IsCP() )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", "/givelicense" ), NotifyType.Error );
			return;
		}

		var target = FindPlayer( args[0] );
		if ( target is null )
		{
			ply.Notify( LanguageSystem.Get( "could_not_find", args[0] ), NotifyType.Error );
			return;
		}

		var comp = target.GetDarkRPComponent();
		if ( comp is null ) return;

		comp.HasGunLicense = true;
		Hook.Run( "playerGotLicense", target, ply );

		target.Notify( LanguageSystem.Get( "gunlicense_granted", ply.DisplayName, target.DisplayName ), NotifyType.Info );
		ply.Notify( LanguageSystem.Get( "gunlicense_granted", ply.DisplayName, target.DisplayName ), NotifyType.Info );
	}

	// ─── /requestlicense — попросить лицензию ────────────────────────────────
	[ChatCommand( "/requestlicense", Cooldown = 2f )]
	public static void CmdRequestLicense( Connection ply, string[] args )
	{
		var comp = ply.GetDarkRPComponent();
		if ( comp is null || comp.HasGunLicense )
		{
			ply.Notify( LanguageSystem.Get( "unable", "/requestlicense" ), NotifyType.Error );
			return;
		}

		// Найти первого доступного мэра/шефа/CP
		Connection? authority = Connection.All.FirstOrDefault( c => c.IsMayor() )
			?? Connection.All.FirstOrDefault( c => c.IsChief() )
			?? Connection.All.FirstOrDefault( c => c.IsCP() );

		if ( authority is null )
		{
			ply.Notify( LanguageSystem.Get( "unable", "/requestlicense" ), NotifyType.Error );
			return;
		}

		// Отправить вопрос авторитету
		var question = LanguageSystem.Get( "gunlicense_question_text", ply.DisplayName );
		authority.Notify( $"[ЛИЦЕНЗИЯ] {question}", NotifyType.Info );
		ply.Notify( LanguageSystem.Get( "gunlicense_requested", ply.DisplayName, authority.DisplayName ), NotifyType.Info );
		// TODO: интерактивный DarkRP.createQuestion UI (phase-4 — нужен Modal Razor)
	}

	// ─── /jailpos — сохранить позицию тюрьмы ─────────────────────────────────
	[ChatCommand( "/jailpos" )]
	[ChatCommand( "/setjailpos" )]
	[ChatCommand( "/addjailpos" )]
	public static void CmdJailPos( Connection ply, string[] args )
	{
		if ( !ply.IsChief() && !IsAdmin( ply ) )
		{
			ply.Notify( LanguageSystem.Get( "no_privilege" ), NotifyType.Error );
			return;
		}

		var pos = ply.Pawn?.WorldPosition ?? Vector3.Zero;
		_jailPositions.Add( pos );
		ply.Notify( $"Позиция тюрьмы #{_jailPositions.Count} сохранена.", NotifyType.Info );
	}

	// ─── Серверные методы ply:arrest() / ply:unArrest() ─────────────────────

	/// <summary>
	/// Арестовать игрока.
	/// Lua: ply:arrest(time, arrester)
	/// </summary>
	public static void Arrest( Connection target, float time, Connection? arrester )
	{
		if ( !Networking.IsHost ) return;

		var hookResult = Hook.Run( "canArrest", arrester, target );
		if ( hookResult is false ) return;

		var comp = target.GetDarkRPComponent();
		if ( comp is null ) return;

		// Снять розыск при аресте
		if ( comp.IsWanted )
			UnWanted( target, arrester );

		comp.IsArrested = true;
		comp.ArrestTimeRemaining = time;
		comp.HasGunLicense = false;

		Hook.Run( "playerArrested", target, time, arrester );

		// Оповестить всех
		var phrase = LanguageSystem.Get( "hes_arrested", target.DisplayName, (int)time );
		foreach ( var conn in Connection.All )
			conn.Notify( phrase, NotifyType.Warning );

		target.Notify( LanguageSystem.Get( "youre_arrested", (int)time ), NotifyType.Error );

		if ( arrester is not null )
			target.Notify( LanguageSystem.Get( "youre_arrested_by", arrester.DisplayName ), NotifyType.Error );

		// Телепорт в тюрьму
		var jailPos = GetRandomJailPos();
		if ( jailPos.HasValue && target.Pawn is not null )
			target.Pawn.WorldPosition = jailPos.Value;
	}

	/// <summary>
	/// Освободить из-под ареста.
	/// Lua: ply:unArrest(unarrester)
	/// </summary>
	public static void UnArrest( Connection target, Connection? unarrester )
	{
		if ( !Networking.IsHost ) return;

		var comp = target.GetDarkRPComponent();
		if ( comp is null || !comp.IsArrested ) return;

		comp.IsArrested = false;
		comp.ArrestTimeRemaining = 0f;

		Hook.Run( "playerUnArrested", target, unarrester );

		foreach ( var conn in Connection.All )
			conn.Notify( LanguageSystem.Get( "hes_unarrested", target.DisplayName ), NotifyType.Info );
	}

	/// <summary>
	/// Объявить в розыск.
	/// Lua: ply:wanted(actor, reason)
	/// </summary>
	public static void Wanted( Connection target, Connection? actor, string reason )
	{
		if ( !Networking.IsHost ) return;

		var comp = target.GetDarkRPComponent();
		if ( comp is null ) return;
		if ( comp.IsWanted )
		{
			actor?.Notify( LanguageSystem.Get( "already_wanted" ), NotifyType.Error );
			return;
		}

		comp.IsWanted = true;
		comp.WantedReason = reason;

		Hook.Run( "playerWanted", target, actor, reason );

		var actorName = actor?.DisplayName ?? "Unknown";
		var msg = LanguageSystem.Get( "wanted_by_police", target.DisplayName, reason, actorName );
		foreach ( var conn in Connection.All )
			conn.Notify( msg, NotifyType.Warning );
	}

	/// <summary>
	/// Снять розыск.
	/// Lua: ply:unWanted(actor)
	/// </summary>
	public static void UnWanted( Connection target, Connection? actor )
	{
		if ( !Networking.IsHost ) return;

		var comp = target.GetDarkRPComponent();
		if ( comp is null ) return;

		comp.IsWanted = false;
		comp.WantedReason = "";

		Hook.Run( "playerUnWanted", target, actor );

		var msg = actor is not null
			? LanguageSystem.Get( "wanted_revoked", target.DisplayName, actor.DisplayName )
			: LanguageSystem.Get( "wanted_expired", target.DisplayName );

		foreach ( var conn in Connection.All )
			conn.Notify( msg, NotifyType.Info );
	}

	// ─── Вспомогательное ─────────────────────────────────────────────────────

	private static Connection? FindPlayer( string name ) =>
		Connection.All.FirstOrDefault( c =>
			c.DisplayName.Contains( name, System.StringComparison.OrdinalIgnoreCase ) );

	private static bool IsAdmin( Connection ply ) =>
		ply.IsHost; // TODO: полноценная система ролей (phase-4)
}
