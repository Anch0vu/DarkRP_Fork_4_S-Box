// Source: gamemode/modules/police/sv_commands.lua (agenda, lockdown, lottery, license commands)
// Lua: DarkRP.defineChatCommand("agenda"), ("lockdown"), ("unlockdown"), ("lottery")
//      DarkRP.lockdown(ply), DarkRP.unLockdown(ply)
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Система мэра — повестка, комендантский час, лотерея.
/// Lua: gamemode/modules/police/sv_commands.lua (mayor section)
/// </summary>
public static class AgendaSystem
{
	// Lua: GetGlobalBool("DarkRP_LockDown")
	[Sync( SyncFlags.FromHost )] public static bool IsLockdown { get; private set; } = false;
	private static RealTimeSince _lastLockdown = float.MaxValue;
	private const float LockdownCooldown = 300f; // Lua: GAMEMODE.Config.lockdowndelay

	// Лотерея
	private static bool _lotteryActive = false;
	private static int _lotteryAmount = 0;
	private static readonly List<Connection> _lotteryParticipants = new();
	private static RealTimeSince _lastLottery;
	private const float LotteryCooldown = 60f;

	// ─── /agenda <text> — установить повестку ────────────────────────────────
	[ChatCommand( "/agenda", Cooldown = 0.1f )]
	public static void CmdAgenda( Connection ply, string[] args )
	{
		if ( !ply.IsMayor() )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", LanguageSystem.Get( "agenda" ) ), NotifyType.Error );
			return;
		}

		var text = string.Join( " ", args );
		SetAgenda( ply, text );
	}

	// ─── /addagenda <text> — добавить к повестке ─────────────────────────────
	[ChatCommand( "/addagenda", Cooldown = 0.1f )]
	public static void CmdAddAgenda( Connection ply, string[] args )
	{
		if ( !ply.IsMayor() )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", LanguageSystem.Get( "agenda" ) ), NotifyType.Error );
			return;
		}

		var addition = string.Join( " ", args );
		// Получить текущую повестку мэра
		var comp = ply.GetDarkRPComponent();
		var current = comp?.Agenda ?? "";
		var next = string.IsNullOrEmpty( current ) ? addition : current + "\n" + addition;
		SetAgenda( ply, next );
	}

	// ─── /lockdown — объявить комендантский час ────────────────────────────────
	[ChatCommand( "/lockdown", Cooldown = 1f )]
	public static void CmdLockdown( Connection ply, string[] args )
	{
		if ( !ply.IsMayor() )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", "/lockdown" ), NotifyType.Error );
			return;
		}
		if ( IsLockdown )
		{
			ply.Notify( LanguageSystem.Get( "already_lockdown" ), NotifyType.Error );
			return;
		}
		if ( _lastLockdown < LockdownCooldown )
		{
			ply.Notify( LanguageSystem.Get( "have_to_wait", (int)(LockdownCooldown - _lastLockdown), "/lockdown" ), NotifyType.Error );
			return;
		}

		IsLockdown = true;
		_lastLockdown = 0f;
		Hook.Run( "lockdownStarted", ply );

		var msg = LanguageSystem.Get( "lockdown" );
		foreach ( var conn in Connection.All )
			conn.Notify( msg, NotifyType.Error );
	}

	// ─── /unlockdown — завершить комендантский час ───────────────────────────
	[ChatCommand( "/unlockdown", Cooldown = 1f )]
	public static void CmdUnLockdown( Connection ply, string[] args )
	{
		if ( !ply.IsMayor() )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", "/unlockdown" ), NotifyType.Error );
			return;
		}
		if ( !IsLockdown )
		{
			ply.Notify( LanguageSystem.Get( "no_lockdown" ), NotifyType.Error );
			return;
		}

		IsLockdown = false;
		Hook.Run( "lockdownEnded", ply );

		var msg = LanguageSystem.Get( "lockdown_over" );
		foreach ( var conn in Connection.All )
			conn.Notify( msg, NotifyType.Info );
	}

	// ─── /lottery <amount> — запустить лотерею ───────────────────────────────
	[ChatCommand( "/lottery", Cooldown = 1f )]
	public static void CmdLottery( Connection ply, string[] args )
	{
		if ( !ply.IsMayor() )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", "/lottery" ), NotifyType.Error );
			return;
		}
		if ( _lotteryActive )
		{
			ply.Notify( LanguageSystem.Get( "lottery_ongoing" ), NotifyType.Error );
			return;
		}
		if ( _lastLottery < LotteryCooldown )
		{
			ply.Notify( LanguageSystem.Get( "have_to_wait", (int)(LotteryCooldown - _lastLottery), "/lottery" ), NotifyType.Error );
			return;
		}
		if ( Connection.All.Count() <= 2 )
		{
			ply.Notify( LanguageSystem.Get( "too_few_players_for_lottery", 2 ), NotifyType.Error );
			return;
		}
		if ( args.Length == 0 || !int.TryParse( args[0], out var amount ) )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		_lotteryAmount = System.Math.Clamp( amount, 100, 50000 );
		_lotteryActive = true;
		_lotteryParticipants.Clear();

		Hook.Run( "lotteryStarted", ply, _lotteryAmount );

		var phrase = LanguageSystem.Get( "lottery_has_started", DarkRP.FormatMoney( _lotteryAmount ) );
		foreach ( var conn in Connection.All )
			conn.Notify( phrase + " Введи /enterlottery чтобы участвовать!", NotifyType.Info );

		// Таймер закрытия (30 секунд)
		// TODO: заменить на async Task.Delay (phase-4 async refactor)
	}

	// ─── /enterlottery — вступить в лотерею ───────────────────────────────────
	[ChatCommand( "/enterlottery", Cooldown = 1f )]
	[ChatCommand( "/el", Cooldown = 1f )]
	public static void CmdEnterLottery( Connection ply, string[] args )
	{
		if ( !_lotteryActive )
		{
			ply.Notify( "Лотерея не активна!", NotifyType.Error );
			return;
		}
		if ( _lotteryParticipants.Contains( ply ) )
		{
			ply.Notify( "Вы уже участвуете в лотерее!", NotifyType.Error );
			return;
		}
		if ( !ply.CanAfford( _lotteryAmount ) )
		{
			ply.Notify( LanguageSystem.Get( "cant_afford" ), NotifyType.Error );
			return;
		}

		ply.AddMoney( -_lotteryAmount );
		_lotteryParticipants.Add( ply );
		Hook.Run( "playerEnteredLottery", ply );
		ply.Notify( LanguageSystem.Get( "lottery_entered", DarkRP.FormatMoney( _lotteryAmount ) ), NotifyType.Info );
	}

	// ─── /endlottery — завершить лотерею (мэр) ────────────────────────────────
	[ChatCommand( "/endlottery", Cooldown = 1f )]
	public static void CmdEndLottery( Connection ply, string[] args )
	{
		if ( !ply.IsMayor() )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", "/endlottery" ), NotifyType.Error );
			return;
		}
		if ( !_lotteryActive )
		{
			ply.Notify( "Нет активной лотереи!", NotifyType.Error );
			return;
		}

		FinishLottery();
	}

	// ─── Вспомогательные ──────────────────────────────────────────────────────

	private static void SetAgenda( Connection mayor, string text )
	{
		// Найти всех игроков на одной работе с мэром (подписчики повестки)
		var mayorJobId = mayor.GetDarkRPComponent()?.JobId ?? "";

		foreach ( var conn in Connection.All )
		{
			var comp = conn.GetDarkRPComponent();
			if ( comp is null ) continue;
			if ( comp.JobId != mayorJobId && conn != mayor ) continue;

			comp.Agenda = text;
			conn.Notify( LanguageSystem.Get( "agenda_set" ), NotifyType.Info );
		}

		Hook.Run( "agendaUpdated", mayor, text );
	}

	private static void FinishLottery()
	{
		_lotteryActive = false;
		_lastLottery = 0f;

		// Убрать отключившихся участников
		_lotteryParticipants.RemoveAll( p => !Connection.All.Contains( p ) );

		if ( _lotteryParticipants.Count == 0 )
		{
			foreach ( var conn in Connection.All )
				conn.Notify( LanguageSystem.Get( "lottery_noone_entered" ), NotifyType.Warning );
			Hook.Run( "lotteryEnded", _lotteryParticipants );
			return;
		}

		var winner = _lotteryParticipants[System.Random.Shared.Next( _lotteryParticipants.Count )];
		var pot = _lotteryParticipants.Count * _lotteryAmount;
		winner.AddMoney( pot );

		Hook.Run( "lotteryEnded", _lotteryParticipants, winner, pot );

		var msg = LanguageSystem.Get( "lottery_won", winner.DisplayName, DarkRP.FormatMoney( pot ) );
		foreach ( var conn in Connection.All )
			conn.Notify( msg, NotifyType.Info );

		_lotteryParticipants.Clear();
	}
}
