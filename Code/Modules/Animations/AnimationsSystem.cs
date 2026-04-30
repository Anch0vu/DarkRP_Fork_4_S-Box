// Source: gamemode/modules/animations/sh_animations.lua
// Lua: DarkRP.addPlayerGesture(anim, text)
//      hook.Add("loadCustomDarkRPItems", "loadAnimations", ...)
using System.Collections.Generic;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Реестр анимаций/жестов.
/// Lua: gamemode/modules/animations/sh_animations.lua
/// Чат-команды (/dance, /wave и т.д.) транслируются всем клиентам через [Rpc.Broadcast].
/// </summary>
public static class AnimationsSystem
{
	/// <summary>Регистр доступных жестов: команда → имя анимации.</summary>
	private static readonly Dictionary<string, string> _gestures = new( System.StringComparer.OrdinalIgnoreCase )
	{
		// Lua: ACT_GMOD_GESTURE_BOW и т.д.
		["bow"] = "gesture_bow",
		["dance"] = "taunt_dance",
		["sexydance"] = "taunt_muscle",
		["follow"] = "gesture_becon",
		["beckon"] = "gesture_becon",
		["laugh"] = "taunt_laugh",
		["lion"] = "taunt_persistence",
		["no"] = "gesture_disagree",
		["yes"] = "gesture_agree",
		["thumbsup"] = "gesture_agree",
		["wave"] = "gesture_wave",
		["hi"] = "gesture_wave",
	};

	/// <summary>Получить все зарегистрированные жесты.</summary>
	public static IReadOnlyDictionary<string, string> GetGestures() => _gestures;

	/// <summary>Lua: DarkRP.addPlayerGesture(anim, text)</summary>
	public static void AddGesture( string command, string animName ) =>
		_gestures[command] = animName;

	/// <summary>Lua: DarkRP.removePlayerGesture(anim)</summary>
	public static void RemoveGesture( string command ) =>
		_gestures.Remove( command );

	// ─── Воспроизведение ──────────────────────────────────────────────────────

	/// <summary>
	/// Транслировать выполнение анимации игроком всем клиентам.
	/// Клиент-сторона (PlayerController/AnimationGraph) подхватывает по имени.
	/// </summary>
	[Rpc.Broadcast]
	public static void PlayGestureRpc( ulong steamId, string animName )
	{
		// Lua: ply:DoAnimationEvent(ACT_X)
		// TODO (phase-7+): найти GameObject игрока по SteamId и установить параметр анимации.
		// Пока — лог в консоль для отладки.
		Log.Info( $"[Anim] {steamId} → {animName}" );
	}

	// ─── Команды ──────────────────────────────────────────────────────────────

	/// <summary>Универсальная команда жестов: /gesture <name></summary>
	[ChatCommand( "/gesture", Cooldown = 1.5f )]
	[ChatCommand( "/anim", Cooldown = 1.5f )]
	public static void CmdGesture( Connection ply, string[] args )
	{
		if ( args.Length == 0 )
		{
			var list = string.Join( ", ", _gestures.Keys );
			ply.Notify( $"Доступно: {list}", NotifyType.Info, 8f );
			return;
		}

		PlayGesture( ply, args[0] );
	}

	private static void PlayGesture( Connection ply, string gesture )
	{
		if ( !_gestures.TryGetValue( gesture, out var anim ) )
		{
			ply.Notify( $"Неизвестный жест '{gesture}'.", NotifyType.Error );
			return;
		}

		PlayGestureRpc( ply.SteamId, anim );
		Hook.Run( "playerPlayedGesture", ply, gesture, anim );
	}

	// Аккуратные алиасы для наиболее популярных
	[ChatCommand( "/bow", Cooldown = 1.5f )]
	public static void CmdBow( Connection ply, string[] args ) => PlayGesture( ply, "bow" );

	[ChatCommand( "/dance", Cooldown = 1.5f )]
	public static void CmdDance( Connection ply, string[] args ) => PlayGesture( ply, "dance" );

	[ChatCommand( "/wave", Cooldown = 1.5f )]
	public static void CmdWave( Connection ply, string[] args ) => PlayGesture( ply, "wave" );

	[ChatCommand( "/laugh", Cooldown = 1.5f )]
	public static void CmdLaugh( Connection ply, string[] args ) => PlayGesture( ply, "laugh" );
}
