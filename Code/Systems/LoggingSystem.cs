// Source: gamemode/modules/logging/sv_logging.lua
// Lua: DarkRP.log(text, colour, noFileSave) — вывод в консоль + файл
using System;
using System.Collections.Generic;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Расширенная система логирования DarkRP.
/// Lua: gamemode/modules/logging/sv_logging.lua
/// Хранит последние N записей в памяти (для /logs команды) и пишет в консоль.
/// </summary>
public sealed class LoggingSystem : GameObjectSystem
{
	private const int MaxLogEntries = 500;

	private static readonly List<LogEntry> _entries = new();

	public LoggingSystem( Scene scene ) : base( scene ) { }

	// ─── Публичное API ────────────────────────────────────────────────────────

	/// <summary>
	/// Добавить запись в лог.
	/// Lua: DarkRP.log(text, colour) → уже вызывается через DarkRP.Log()
	/// </summary>
	public static void Add( string message, LogSeverity severity = LogSeverity.Info )
	{
		var entry = new LogEntry
		{
			Time = DateTime.Now,
			Message = message,
			Severity = severity,
		};

		_entries.Add( entry );

		if ( _entries.Count > MaxLogEntries )
			_entries.RemoveAt( 0 );

		// Lua: print("[DarkRP] " .. message)
		switch ( severity )
		{
			case LogSeverity.Warning:
				Log.Warning( $"[DarkRP] {message}" );
				break;
			case LogSeverity.Error:
				Log.Error( $"[DarkRP] {message}" );
				break;
			default:
				Log.Info( $"[DarkRP] {message}" );
				break;
		}
	}

	/// <summary>Получить последние записи лога.</summary>
	public static IReadOnlyList<LogEntry> GetEntries() => _entries;

	// ─── Команды ──────────────────────────────────────────────────────────────

	/// <summary>Admin: показать последние 20 записей лога.</summary>
	[ChatCommand( "/logs", Cooldown = 2f )]
	public static void CmdLogs( Connection ply, string[] args )
	{
		if ( !ply.IsHost )
		{
			ply.Notify( "Только для администраторов.", NotifyType.Error );
			return;
		}

		var count = 20;
		if ( args.Length > 0 && int.TryParse( args[0], out var n ) )
			count = Math.Clamp( n, 1, 100 );

		var start = Math.Max( 0, _entries.Count - count );
		for ( var i = start; i < _entries.Count; i++ )
		{
			var e = _entries[i];
			ply.Notify( $"[{e.Time:HH:mm:ss}] {e.Message}", NotifyType.Info, 8f );
		}
	}

	// ─── Хуки ─────────────────────────────────────────────────────────────────

	[DarkRPHook( "PlayerConnected" )]
	public static void OnConnected( Connection ply ) =>
		Add( $"{ply.DisplayName} ({ply.SteamId}) подключился." );

	[DarkRPHook( "PlayerDisconnected" )]
	public static void OnDisconnected( Connection ply ) =>
		Add( $"{ply.DisplayName} ({ply.SteamId}) отключился." );

	[DarkRPHook( "PlayerChangedJob" )]
	public static void OnJobChanged( Connection ply, Job? oldJob, Job? newJob ) =>
		Add( $"{ply.DisplayName} сменил работу: {oldJob?.Name ?? "?"} → {newJob?.Name ?? "?"}" );
}

public sealed class LogEntry
{
	public DateTime Time { get; init; }
	public string Message { get; init; } = "";
	public LogSeverity Severity { get; init; }
}

public enum LogSeverity
{
	Info,
	Warning,
	Error,
}
