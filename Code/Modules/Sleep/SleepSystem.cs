// Source: gamemode/modules/sleep/sv_sleep.lua
// Lua: DarkRP.toggleSleep(player, command) + DarkRP.defineChatCommand("sleep")
// Упрощённый порт: без ragdoll (S&Box не имеет prop_ragdoll аналога в фазе-5).
// IsSleeping = true → клиент замораживает ввод и показывает оверлей.
using System.Collections.Generic;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Система сна.
/// Lua: gamemode/modules/sleep/sv_sleep.lua
/// </summary>
public static class SleepSystem
{
	// Lua: local KnockoutTime = 5 — кулдаун между засыпаниями
	private const float KnockoutCooldown = 5f;

	private static readonly Dictionary<ulong, float> _lastSleep = new();

	// ─── Публичное API ────────────────────────────────────────────────────────

	/// <summary>
	/// Переключить сон игрока.
	/// Lua: DarkRP.toggleSleep(player, command)
	/// </summary>
	public static void ToggleSleep( Connection ply, bool force = false )
	{
		var comp = ply.GetDarkRPComponent();
		if ( comp is null ) return;

		if ( comp.IsSleeping )
		{
			Wake( ply, comp );
		}
		else
		{
			TryFallAsleep( ply, comp, force );
		}
	}

	private static void TryFallAsleep( Connection ply, DarkRPPlayerComponent comp, bool force )
	{
		if ( ply.IsArrested() && !force )
		{
			ply.Notify( LanguageSystem.Get( "arrest_cant_do" ), NotifyType.Error );
			return;
		}

		if ( !force && _lastSleep.TryGetValue( ply.SteamId, out var t ) &&
			Time.Now < t + KnockoutCooldown )
		{
			var wait = System.Math.Ceiling( t + KnockoutCooldown - Time.Now );
			ply.Notify( $"Подождите ещё {wait} сек. перед сном.", NotifyType.Error );
			return;
		}

		comp.IsSleeping = true;
		_lastSleep[ply.SteamId] = Time.Now;

		foreach ( var c in Connection.All )
			c.Notify( $"{ply.DisplayName} заснул.", NotifyType.Info, 5f );

		Hook.Run( "playerSlept", ply );
	}

	private static void Wake( Connection ply, DarkRPPlayerComponent comp )
	{
		comp.IsSleeping = false;
		ply.Notify( "Вы проснулись.", NotifyType.Info );
		Hook.Run( "playerWoke", ply );
	}

	// ─── Хуки ─────────────────────────────────────────────────────────────────

	/// <summary>Lua: hook.Add("OnPlayerChangedTeam", "SleepMod", stopSleep)</summary>
	[DarkRPHook( "PlayerChangedJob" )]
	public static void OnJobChanged( Connection ply, Job? oldJob, Job? newJob )
	{
		var comp = ply.GetDarkRPComponent();
		if ( comp?.IsSleeping == true )
			Wake( ply, comp );
	}

	// ─── Команды ──────────────────────────────────────────────────────────────

	/// <summary>Lua: DarkRP.defineChatCommand("sleep") / ("wake") / ("wakeup")</summary>
	[ChatCommand( "/sleep", Cooldown = 0.5f )]
	[ChatCommand( "/wake", Cooldown = 0.5f )]
	[ChatCommand( "/wakeup", Cooldown = 0.5f )]
	public static void CmdSleep( Connection ply, string[] args ) => ToggleSleep( ply );
}
