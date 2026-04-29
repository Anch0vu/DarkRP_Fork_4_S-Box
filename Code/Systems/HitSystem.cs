// Source: gamemode/modules/hitmenu/sv_init.lua
// Lua: ply:requestHit, ply:placeHit, ply:cancelHit, ply:finishHit
//      DarkRP.defineChatCommand("hitprice"), ("requesthit"), ("cancelhit")
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Система заказных убийств (hitmenu).
/// Lua: gamemode/modules/hitmenu/sv_init.lua
/// </summary>
public static class HitSystem
{
	private sealed class HitEntry
	{
		public Connection Customer { get; init; } = null!;
		public Connection Target { get; init; } = null!;
		public int Price { get; init; }
	}

	private static readonly Dictionary<ulong, HitEntry> _hits = new(); // hitman.SteamId → hit

	// Lua: GAMEMODE.Config.minHitPrice / maxHitPrice
	public const int MinHitPrice = 200;
	public const int MaxHitPrice = 50000;

	// Пользовательские цены хитменов: steamId → цена
	private static readonly Dictionary<ulong, int> _hitPrices = new();

	// ─── /hitprice <price> ────────────────────────────────────────────────────
	[ChatCommand( "/hitprice", Cooldown = 0.5f )]
	public static void CmdHitPrice( Connection ply, string[] args )
	{
		if ( !ply.IsHitman() )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", "/hitprice" ), NotifyType.Error );
			return;
		}
		if ( args.Length == 0 || !int.TryParse( args[0], out var rawPrice ) )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var price = System.Math.Clamp( rawPrice, MinHitPrice, MaxHitPrice );
		_hitPrices[ply.SteamId] = price;
		ply.Notify( LanguageSystem.Get( "hit_price_set", DarkRP.FormatMoney( price ) ), NotifyType.Info );
	}

	// ─── /requesthit <target> — заказать убийство ────────────────────────────
	[ChatCommand( "/requesthit", Cooldown = 1f )]
	public static void CmdRequestHit( Connection ply, string[] args )
	{
		if ( args.Length == 0 )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		// args[0] = имя цели, смотрим на хитмена через raycast (TODO phase-4)
		var target = FindPlayer( args[0] );
		if ( target is null )
		{
			ply.Notify( LanguageSystem.Get( "could_not_find", args[0] ), NotifyType.Error );
			return;
		}

		// Найти любого свободного хитмена
		var hitman = Connection.All.FirstOrDefault( c => c.IsHitman() && !_hits.ContainsKey( c.SteamId ) );
		if ( hitman is null )
		{
			ply.Notify( LanguageSystem.Get( "could_not_find", "hitman" ), NotifyType.Error );
			return;
		}

		var hookResult = Hook.Run( "canRequestHit", hitman, ply, target );
		if ( hookResult is false ) return;

		var price = _hitPrices.GetValueOrDefault( hitman.SteamId, MinHitPrice );

		if ( !ply.CanAfford( price ) )
		{
			ply.Notify( LanguageSystem.Get( "cant_afford" ), NotifyType.Error );
			return;
		}

		// Уведомить хитмена о предложении
		hitman.Notify(
			LanguageSystem.Get( "accept_hit_request", ply.DisplayName, target.DisplayName, DarkRP.FormatMoney( price ) ),
			NotifyType.Warning );
		ply.Notify( LanguageSystem.Get( "hit_requested" ), NotifyType.Info );

		// TODO: интерактивный DarkRP.createQuestion → PlaceHit (phase-4 Modal UI)
		// Временно: авто-принять для хитмена
		PlaceHit( hitman, ply, target, price );
	}

	// ─── /cancelhit — отменить текущий заказ ─────────────────────────────────
	[ChatCommand( "/cancelhit", Cooldown = 1f )]
	public static void CmdCancelHit( Connection ply, string[] args )
	{
		if ( !_hits.ContainsKey( ply.SteamId ) )
		{
			ply.Notify( LanguageSystem.Get( "no_active_hit" ), NotifyType.Error );
			return;
		}

		var hit = _hits[ply.SteamId];
		if ( !ply.CanAfford( hit.Price ) )
		{
			ply.Notify( LanguageSystem.Get( "cant_afford" ), NotifyType.Error );
			return;
		}

		// Возврат денег заказчику
		ply.AddMoney( -hit.Price );
		hit.Customer.AddMoney( hit.Price );

		AbortHit( ply, LanguageSystem.Get( "hit_cancelled" ) );
	}

	// ─── Серверные методы ────────────────────────────────────────────────────

	public static void PlaceHit( Connection hitman, Connection customer, Connection target, int price )
	{
		if ( _hits.ContainsKey( hitman.SteamId ) ) return;
		if ( !customer.CanAfford( price ) )
		{
			customer.Notify( LanguageSystem.Get( "cant_afford" ), NotifyType.Error );
			return;
		}

		customer.AddMoney( -price );
		hitman.AddMoney( price );

		_hits[hitman.SteamId] = new HitEntry { Customer = customer, Target = target, Price = price };

		Hook.Run( "onHitAccepted", hitman, target, customer );

		hitman.Notify( LanguageSystem.Get( "hit_accepted" ), NotifyType.Info );
		DarkRP.Log( $"Hit: {hitman.DisplayName} → {target.DisplayName} (by {customer.DisplayName}, {DarkRP.FormatMoney( price )})" );
	}

	public static void AbortHit( Connection hitman, string reason )
	{
		if ( !_hits.TryGetValue( hitman.SteamId, out var hit ) ) return;

		Hook.Run( "onHitFailed", hitman, hit.Target, reason );
		_hits.Remove( hitman.SteamId );

		foreach ( var conn in Connection.All )
			conn.Notify( LanguageSystem.Get( "hit_aborted", reason ), NotifyType.Warning );
	}

	// ─── Хуки ────────────────────────────────────────────────────────────────

	[DarkRPHook( "PlayerDeath" )]
	public static void OnPlayerDeath( Connection ply, object? inflictor, Connection? attacker )
	{
		// Хитмен умер
		if ( _hits.ContainsKey( ply.SteamId ) )
			AbortHit( ply, LanguageSystem.Get( "hitman_died" ) );

		// Хитмен убил цель
		if ( attacker is not null && _hits.TryGetValue( attacker.SteamId, out var hit ) && hit.Target == ply )
		{
			Hook.Run( "onHitCompleted", attacker, ply, hit.Customer );
			attacker.Notify( LanguageSystem.Get( "hit_complete", attacker.DisplayName ), NotifyType.Info );
			_hits.Remove( attacker.SteamId );
			DarkRP.Log( $"Hit completed: {attacker.DisplayName} killed {ply.DisplayName}" );
		}

		// У кого-то цель умерла
		foreach ( var (id, h) in _hits.Where( kvp => kvp.Value.Target == ply ).ToList() )
		{
			var hm = Connection.All.FirstOrDefault( c => c.SteamId == id );
			if ( hm is not null ) AbortHit( hm, LanguageSystem.Get( "target_died" ) );
		}
	}

	[DarkRPHook( "PlayerDisconnected" )]
	public static void OnPlayerDisconnected( Connection ply )
	{
		if ( _hits.ContainsKey( ply.SteamId ) )
			AbortHit( ply, LanguageSystem.Get( "hitman_left_server" ) );

		foreach ( var (id, h) in _hits.Where( kvp => kvp.Value.Target == ply || kvp.Value.Customer == ply ).ToList() )
		{
			var hm = Connection.All.FirstOrDefault( c => c.SteamId == id );
			if ( hm is not null ) AbortHit( hm, LanguageSystem.Get( "target_left_server" ) );
		}
	}

	[DarkRPHook( "PlayerChangedJob" )]
	public static void OnPlayerChangedJob( Connection ply, string prevJobId, string newJobId )
	{
		if ( _hits.ContainsKey( ply.SteamId ) )
			AbortHit( ply, LanguageSystem.Get( "hitman_changed_team" ) );
	}

	[DarkRPHook( "playerArrested" )]
	public static void OnPlayerArrested( Connection ply, float time, Connection? arrester )
	{
		if ( !_hits.TryGetValue( ply.SteamId, out var hit ) ) return;

		foreach ( var conn in Connection.All.Where( c => c.IsCP() ) )
			conn.Notify( LanguageSystem.Get( "x_had_hit_ordered_by_y", ply.DisplayName, hit.Customer.DisplayName ), NotifyType.Info );

		AbortHit( ply, LanguageSystem.Get( "hitman_arrested" ) );
	}

	// ─── Вспомогательное ─────────────────────────────────────────────────────

	private static Connection? FindPlayer( string name ) =>
		Connection.All.FirstOrDefault( c =>
			c.DisplayName.Contains( name, System.StringComparison.OrdinalIgnoreCase ) );
}
