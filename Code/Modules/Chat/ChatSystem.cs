// Source: gamemode/modules/chat/sv_chatcommands.lua
// Source: gamemode/modules/chat/sh_chatcommands.lua
// Lua: DarkRP.defineChatCommand("pm", PM, 1.5)
//      DarkRP.defineChatCommand("w" / "y" / "me" / "ooc" / "broadcast" / "channel" / "radio" / "g")
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Чат-система DarkRP — PM, OOC, шёпот, крик, /me, broadcast, рация, group chat.
/// Lua: gamemode/modules/chat/sv_chatcommands.lua
/// </summary>
public static class ChatSystem
{
	// Lua: GAMEMODE.Config.whisperDistance / yellDistance / meDistance
	private const float WhisperDistance = 90f;
	private const float YellDistance = 600f;
	private const float MeDistance = 256f;

	// Lua: ply.RadioChannel
	private static readonly Dictionary<ulong, int> _radioChannels = new();

	// ─── /pm <name> <msg> — личное сообщение ─────────────────────────────────
	[ChatCommand( "/pm", Cooldown = 1.5f )]
	public static void CmdPm( Connection ply, string[] args )
	{
		if ( args.Length < 2 )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var targetName = args[0];
		var message = string.Join( " ", args.Skip( 1 ) );
		var target = FindPlayer( targetName );

		if ( target is null )
		{
			ply.Notify( LanguageSystem.Get( "could_not_find", targetName ), NotifyType.Error );
			return;
		}
		if ( target == ply ) return;

		var color = GetJobColor( ply );
		var prefix = $"(PM) {ply.DisplayName}";
		ChatMessage.SendToPlayer( target, color, prefix, Color.White, message );
		ChatMessage.SendToPlayer( ply, color, prefix, Color.White, message );
	}

	// ─── /w <text> — шёпот (близко) ──────────────────────────────────────────
	[ChatCommand( "/w", Cooldown = 1.5f )]
	[ChatCommand( "/whisper", Cooldown = 1.5f )]
	public static void CmdWhisper( Connection ply, string[] args )
	{
		var text = string.Join( " ", args );
		if ( string.IsNullOrWhiteSpace( text ) )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var label = $"({LanguageSystem.Get( "whisper" )}) {ply.DisplayName}";
		TalkToRange( ply, GetJobColor( ply ), label, Color.White, text, WhisperDistance );
	}

	// ─── /y <text> — крик (далеко) ───────────────────────────────────────────
	[ChatCommand( "/y", Cooldown = 1.5f )]
	[ChatCommand( "/yell", Cooldown = 1.5f )]
	public static void CmdYell( Connection ply, string[] args )
	{
		var text = string.Join( " ", args );
		if ( string.IsNullOrWhiteSpace( text ) )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var label = $"({LanguageSystem.Get( "yell" )}) {ply.DisplayName}";
		TalkToRange( ply, GetJobColor( ply ), label, Color.White, text, YellDistance );
	}

	// ─── /me <action> — действие в третьем лице ─────────────────────────────
	[ChatCommand( "/me", Cooldown = 1.5f )]
	public static void CmdMe( Connection ply, string[] args )
	{
		var text = string.Join( " ", args );
		if ( string.IsNullOrWhiteSpace( text ) )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var color = GetJobColor( ply );
		var label = $"{ply.DisplayName} {text}";
		TalkToRange( ply, color, label, Color.White, "", MeDistance );
	}

	// ─── /ooc, /, /a — out of character ─────────────────────────────────────
	[ChatCommand( "/ooc", Cooldown = 1.5f )]
	[ChatCommand( "//", Cooldown = 1.5f )]
	[ChatCommand( "/a", Cooldown = 1.5f )]
	public static void CmdOOC( Connection ply, string[] args )
	{
		var text = string.Join( " ", args );
		if ( string.IsNullOrWhiteSpace( text ) )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var phrase = LanguageSystem.Get( "ooc" );
		var color = GetJobColor( ply );
		var prefix = $"({phrase}) {ply.DisplayName}";

		foreach ( var conn in Connection.All )
			ChatMessage.SendToPlayer( conn, color, prefix, Color.White, text );
	}

	// ─── /broadcast — мэрское объявление ────────────────────────────────────
	[ChatCommand( "/broadcast", Cooldown = 1.5f )]
	public static void CmdBroadcast( Connection ply, string[] args )
	{
		var text = string.Join( " ", args );
		if ( string.IsNullOrWhiteSpace( text ) )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var job = ply.GetJob();
		if ( job is null || !job.IsMayor )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", LanguageSystem.Get( "broadcast" ) ), NotifyType.Error );
			return;
		}

		var phrase = LanguageSystem.Get( "broadcast" );
		var color = GetJobColor( ply );
		var prefix = $"{phrase} {ply.DisplayName}";
		var msgColor = new Color( 170 / 255f, 0f, 0f );

		foreach ( var conn in Connection.All )
			ChatMessage.SendToPlayer( conn, color, prefix, msgColor, text );
	}

	// ─── /channel <num> — переключить рацию ─────────────────────────────────
	[ChatCommand( "/channel" )]
	public static void CmdChannel( Connection ply, string[] args )
	{
		if ( args.Length == 0 || !int.TryParse( args[0], out var ch ) || ch < 0 || ch > 100 )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", LanguageSystem.Get( "arguments" ),
				$"0<{LanguageSystem.Get( "channel" )}<100" ), NotifyType.Error );
			return;
		}

		_radioChannels[ply.SteamId] = ch;
		ply.Notify( LanguageSystem.Get( "channel_set_to_x", ch ), NotifyType.Info );
	}

	// ─── /radio <text> — сообщение по рации ─────────────────────────────────
	[ChatCommand( "/radio", Cooldown = 1.5f )]
	public static void CmdRadio( Connection ply, string[] args )
	{
		var text = string.Join( " ", args );
		if ( string.IsNullOrWhiteSpace( text ) )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var channel = _radioChannels.GetValueOrDefault( ply.SteamId, 1 );
		var phrase = LanguageSystem.Get( "radio_x", channel );
		var color = new Color( 180 / 255f, 180 / 255f, 180 / 255f );

		foreach ( var conn in Connection.All )
		{
			if ( _radioChannels.GetValueOrDefault( conn.SteamId, 1 ) != channel ) continue;
			ChatMessage.SendToPlayer( conn, color, phrase, color, text );
		}
	}

	// ─── /g — group chat (одна работа/команда) ──────────────────────────────
	[ChatCommand( "/g" )]
	[ChatCommand( "/group" )]
	public static void CmdGroup( Connection ply, string[] args )
	{
		var text = string.Join( " ", args );
		if ( string.IsNullOrWhiteSpace( text ) )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var senderJob = ply.GetDarkRPComponent()?.JobId ?? "";
		var phrase = LanguageSystem.Get( "group" );
		var color = GetJobColor( ply );
		var prefix = $"{phrase} {ply.DisplayName}";

		foreach ( var conn in Connection.All )
		{
			var targetJob = conn.GetDarkRPComponent()?.JobId ?? "";
			if ( targetJob != senderJob ) continue;
			ChatMessage.SendToPlayer( conn, color, prefix, Color.White, text );
		}
	}

	// ─── Вспомогательное ────────────────────────────────────────────────────

	/// <summary>Lua: DarkRP.findPlayer(name)</summary>
	private static Connection? FindPlayer( string name ) =>
		Connection.All.FirstOrDefault( c =>
			c.DisplayName.Contains( name, System.StringComparison.OrdinalIgnoreCase ) );

	private static Color GetJobColor( Connection ply ) =>
		ply.GetJob()?.Color ?? Color.White;

	/// <summary>
	/// Lua: DarkRP.talkToRange(ply, name, text, distance)
	/// Отправить сообщение всем игрокам в радиусе.
	/// </summary>
	private static void TalkToRange( Connection sender, Color nameColor, string label,
		Color textColor, string text, float distance )
	{
		var senderPos = sender.Pawn?.WorldPosition ?? Vector3.Zero;
		var distSqr = distance * distance;

		foreach ( var conn in Connection.All )
		{
			var pos = conn.Pawn?.WorldPosition ?? Vector3.Zero;
			if ( (pos - senderPos).LengthSquared > distSqr ) continue;
			ChatMessage.SendToPlayer( conn, nameColor, label, textColor, text );
		}
	}
}

/// <summary>
/// RPC-обёртка для отправки сообщения в чат конкретному игроку.
/// Lua: DarkRP.talkToPerson(ply, col, name, col2, msg, sender)
/// </summary>
public static class ChatMessage
{
	[Rpc.Owner]
	public static void SendToPlayer( Connection target, Color nameColor, string name,
		Color textColor, string text )
	{
		ChatBox.PushMessage( nameColor, name, textColor, text );
	}
}

/// <summary>
/// Очередь сообщений чата для UI. Razor-компонент Chat.razor читает из неё.
/// </summary>
public static class ChatBox
{
	public sealed class Entry
	{
		public Color NameColor { get; init; }
		public string Name { get; init; } = "";
		public Color TextColor { get; init; }
		public string Text { get; init; } = "";
		public RealTimeSince Age { get; init; }
	}

	public static List<Entry> Messages { get; } = new();
	public const int MaxMessages = 50;
	public const float FadeAfter = 10f;

	public static void PushMessage( Color nameColor, string name, Color textColor, string text )
	{
		Messages.Add( new Entry
		{
			NameColor = nameColor,
			Name = name,
			TextColor = textColor,
			Text = text,
			Age = 0f,
		} );

		while ( Messages.Count > MaxMessages )
			Messages.RemoveAt( 0 );
	}
}
