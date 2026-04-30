// Source: gamemode/modules/hobo/sv_hobo.lua
// Lua: concommand.Add("_hobo_emitsound", MakeZombieSoundsAsHobo) — зомби-звуки для бомжа
// Упрощённый порт: BroadCast уведомления вместо звуков (S&Box не использует .wav пути GMod).
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Hobo job module.
/// Lua: gamemode/modules/hobo/sv_hobo.lua
/// </summary>
public static class HoboSystem
{
	private const float SoundCooldown = 1.3f;

	// steamId → последнее время "звука"
	private static readonly System.Collections.Generic.Dictionary<ulong, float> _lastSound = new();

	// ─── Хуки ─────────────────────────────────────────────────────────────────

	/// <summary>
	/// При входе в работу с флагом "hobo" (из Job) даём пропаганду.
	/// Lua: hook.Add("PlayerLoadout", "HoboLoadout", ...)
	/// </summary>
	[DarkRPHook( "PlayerChangedJob" )]
	public static void OnJobChanged( Connection ply, Job? oldJob, Job? newJob )
	{
		if ( newJob is null ) return;

		// В DarkRP бомж распознаётся по job.hobo. У нас Job не имеет такого флага —
		// используем Id == "hobo" как соглашение.
		if ( string.Equals( newJob.Id, "hobo", System.StringComparison.OrdinalIgnoreCase ) )
		{
			ply.Notify( "Вы стали бездомным. У вас нет дома и работы. Попрошайничайте!", NotifyType.Info, 8f );
		}
	}

	// ─── Команды ──────────────────────────────────────────────────────────────

	/// <summary>
	/// Lua: concommand.Add("_hobo_emitsound") — раньше воспроизводил .wav
	/// Здесь просто рассылает /me-сообщение всем рядом.
	/// </summary>
	[ChatCommand( "/hobosound", Cooldown = 1.3f )]
	public static void CmdHoboSound( Connection ply, string[] args )
	{
		if ( !string.Equals( ply.GetJob()?.Id, "hobo", System.StringComparison.OrdinalIgnoreCase ) )
		{
			ply.Notify( "Только для бездомных.", NotifyType.Error );
			return;
		}

		if ( _lastSound.TryGetValue( ply.SteamId, out var t ) && Time.Now < t + SoundCooldown )
			return;

		_lastSound[ply.SteamId] = Time.Now;

		var pawnPos = ply.Pawn?.WorldPosition ?? Vector3.Zero;
		const float HearDistance = 600f * 600f;

		// Ближайшие игроки видят /me-стиль сообщение
		foreach ( var conn in Connection.All )
		{
			var p = conn.Pawn?.WorldPosition ?? Vector3.Zero;
			if ( p.DistanceSquared( pawnPos ) > HearDistance ) continue;

			var grayName = new Color( 0.6f, 0.6f, 0.6f );
			ChatMessage.SendToPlayer( conn, grayName,
				$"* {ply.DisplayName}", grayName, "издаёт зомби-стон" );
		}

		Hook.Run( "playerHoboSound", ply );
	}
}
