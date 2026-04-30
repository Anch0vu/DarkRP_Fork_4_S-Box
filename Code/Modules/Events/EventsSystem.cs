// Source: gamemode/modules/events/sv_events.lua
// Lua: definePrivilegedChatCommand("enablestorm") / ("disablestorm")
//      timer.Create("EarthquakeTest", ...) — случайные землетрясения
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Случайные события: землетрясения, метеоритный шторм.
/// Lua: gamemode/modules/events/sv_events.lua
/// Упрощённый порт: только уведомления и хуки (без физических эффектов).
/// </summary>
public sealed class EventsSystem : GameObjectSystem
{
	// Lua: GAMEMODE.Config.earthquakes
	private static bool _earthquakesEnabled = true;
	private static bool _stormActive = false;

	// Lua: timer.Create("EarthquakeTest", 1, 0, ...) — каждую секунду
	private const float EarthquakeCheckInterval = 1f;
	private const int EarthquakeChance = 600; // 1 / N шанс

	// Lua: timer.Create("stormControl", ...) — управляет фазой шторма
	private const float StormCheckInterval = 1f;
	private static float _stormTimer = 0f;
	private static int _stormDuration = 60;

	private TimeUntil _nextQuake = EarthquakeCheckInterval;
	private TimeUntil _nextStormCheck = StormCheckInterval;

	public EventsSystem( Scene scene ) : base( scene ) { }

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;

		if ( _earthquakesEnabled && _nextQuake )
		{
			_nextQuake = EarthquakeCheckInterval;
			TickEarthquake();
		}

		if ( _nextStormCheck )
		{
			_nextStormCheck = StormCheckInterval;
			TickStorm();
		}
	}

	// ─── Землетрясения ────────────────────────────────────────────────────────

	private static void TickEarthquake()
	{
		if ( Game.Random.Int( 0, EarthquakeChance ) != 0 ) return;
		var magnitude = Game.Random.Int( 1, 100 ) / 10f;
		BroadcastEarthquake( magnitude );
	}

	private static void BroadcastEarthquake( float magnitude )
	{
		// Lua: DarkRP.notifyAll(0, 3, DarkRP.getPhrase("earthtremor_report", mag))
		var key = magnitude < 6.5f ? "earthtremor_report" : "earthquake_report";
		var msg = LanguageSystem.Get( key, magnitude.ToString( "F1" ) );

		foreach ( var conn in Connection.All )
			conn.Notify( msg, NotifyType.Warning, 5f );

		Hook.Run( "earthquakeOccurred", magnitude );
		DarkRP.Log( $"[Events] Землетрясение силой {magnitude:F1}" );
	}

	// ─── Метеоритный шторм ────────────────────────────────────────────────────

	private static void TickStorm()
	{
		if ( !_stormActive ) return;

		_stormTimer += StormCheckInterval;
		if ( _stormTimer >= _stormDuration )
		{
			EndStorm();
		}
	}

	private static void StartStormInternal()
	{
		_stormActive = true;
		_stormTimer = 0f;
		_stormDuration = Game.Random.Int( 60, 90 );

		var msg = LanguageSystem.Get( "meteor_approaching" );
		foreach ( var conn in Connection.All )
			conn.Notify( msg, NotifyType.Warning, 8f );

		Hook.Run( "meteorStormStart" );
	}

	private static void EndStorm()
	{
		_stormActive = false;
		_stormTimer = 0f;

		var msg = LanguageSystem.Get( "meteor_passing" );
		foreach ( var conn in Connection.All )
			conn.Notify( msg, NotifyType.Info, 5f );

		Hook.Run( "meteorStormEnd" );
	}

	// ─── Команды ──────────────────────────────────────────────────────────────

	/// <summary>Lua: definePrivilegedChatCommand("enablestorm")</summary>
	[ChatCommand( "/enablestorm", Cooldown = 5f )]
	public static void CmdEnableStorm( Connection ply, string[] args )
	{
		if ( !ply.IsHost )
		{
			ply.Notify( "Только для администраторов.", NotifyType.Error );
			return;
		}
		StartStormInternal();
		ply.Notify( LanguageSystem.Get( "meteor_enabled" ), NotifyType.Info );
	}

	/// <summary>Lua: definePrivilegedChatCommand("disablestorm")</summary>
	[ChatCommand( "/disablestorm", Cooldown = 5f )]
	public static void CmdDisableStorm( Connection ply, string[] args )
	{
		if ( !ply.IsHost )
		{
			ply.Notify( "Только для администраторов.", NotifyType.Error );
			return;
		}
		EndStorm();
		ply.Notify( LanguageSystem.Get( "meteor_disabled" ), NotifyType.Info );
	}

	/// <summary>Включить/выключить случайные землетрясения.</summary>
	[ChatCommand( "/toggleearthquakes", Cooldown = 5f )]
	public static void CmdToggleEarthquakes( Connection ply, string[] args )
	{
		if ( !ply.IsHost )
		{
			ply.Notify( "Только для администраторов.", NotifyType.Error );
			return;
		}
		_earthquakesEnabled = !_earthquakesEnabled;
		ply.Notify( $"Землетрясения: {(_earthquakesEnabled ? "ВКЛ" : "ВЫКЛ")}", NotifyType.Info );
	}

	/// <summary>Принудительно вызвать землетрясение.</summary>
	[ChatCommand( "/earthquake", Cooldown = 10f )]
	public static void CmdEarthquake( Connection ply, string[] args )
	{
		if ( !ply.IsHost )
		{
			ply.Notify( "Только для администраторов.", NotifyType.Error );
			return;
		}

		var magnitude = 5.0f;
		if ( args.Length > 0 && float.TryParse( args[0],
				System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out var m ) )
		{
			magnitude = System.Math.Clamp( m, 1f, 10f );
		}

		BroadcastEarthquake( magnitude );
	}
}
