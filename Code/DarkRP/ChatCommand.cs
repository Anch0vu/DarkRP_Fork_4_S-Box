// Source: gamemode/modules/chat/sh_chatcommands.lua
// Source: gamemode/modules/chat/sv_chat.lua (DarkRP.defineChatCommand)
using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Атрибут для регистрации статического метода как команды чата.
/// Lua: DarkRP.defineChatCommand(name, callback)
///
/// Пример:
/// <code>
/// [ChatCommand( "/give" )]
/// public static void CmdGive( Connection sender, string[] args ) { }
/// </code>
/// </summary>
[AttributeUsage( AttributeTargets.Method, AllowMultiple = true )]
public sealed class ChatCommandAttribute : Attribute
{
	public string Command { get; }
	/// <summary>Минимальная задержка между вызовами (секунды). Lua: третий аргумент defineChatCommand</summary>
	public float Cooldown { get; init; } = 0f;
	public ChatCommandAttribute( string command ) => Command = command.ToLowerInvariant();
}

/// <summary>
/// Запись в реестре чат-команд.
/// </summary>
internal sealed class ChatCommandEntry
{
	public string Command { get; init; } = "";
	public string Description { get; init; } = "";
	public float Cooldown { get; init; } = 0f;
	public Func<Connection, string[], bool>? Handler { get; init; }
	private readonly Dictionary<ulong, RealTimeUntil> _cooldowns = new();

	public bool IsOnCooldown( Connection sender )
	{
		if ( Cooldown <= 0f ) return false;
		return _cooldowns.TryGetValue( sender.SteamId, out var until ) && !until;
	}

	public void StartCooldown( Connection sender )
	{
		if ( Cooldown > 0f )
			_cooldowns[sender.SteamId] = Cooldown;
	}
}

/// <summary>
/// Реестр и диспетчер чат-команд.
/// Lua: DarkRP.declareChatCommand / DarkRP.defineChatCommand / DarkRP.getChatCommands
/// </summary>
public static class ChatCommandRegistry
{
	private static readonly Dictionary<string, ChatCommandEntry> _commands = new();

	/// <summary>
	/// Зарегистрировать команду программно.
	/// Lua: DarkRP.defineChatCommand(name, callback, cooldown)
	/// </summary>
	public static void Add( string command, Func<Connection, string[], bool> handler,
		string description = "", float cooldown = 0f )
	{
		var key = command.ToLowerInvariant().TrimStart( '/' );
		_commands[key] = new ChatCommandEntry
		{
			Command = key,
			Description = description,
			Cooldown = cooldown,
			Handler = handler,
		};
	}

	/// <summary>
	/// Удалить команду.
	/// Lua: DarkRP.removeChatCommand(command)
	/// </summary>
	public static void Remove( string command ) =>
		_commands.Remove( command.ToLowerInvariant().TrimStart( '/' ) );

	/// <summary>
	/// Получить все зарегистрированные команды (для F1-меню).
	/// Lua: DarkRP.getChatCommands()
	/// </summary>
	public static IReadOnlyDictionary<string, ChatCommandEntry> GetAll() => _commands;

	/// <summary>
	/// Обработать сообщение чата. Возвращает true если это была команда (и её нужно скрыть из чата).
	/// Вызывается из PlayerSay хука.
	/// </summary>
	internal static bool Dispatch( Connection sender, string message )
	{
		if ( string.IsNullOrWhiteSpace( message ) || message[0] != '/' )
			return false;

		var parts = message.Trim().Split( ' ', StringSplitOptions.RemoveEmptyEntries );
		var key = parts[0].ToLowerInvariant().TrimStart( '/' );

		if ( !_commands.TryGetValue( key, out var entry ) )
			return false;

		if ( entry.IsOnCooldown( sender ) )
			return true; // команда существует, но на кулдауне — всё равно скрываем из чата

		entry.StartCooldown( sender );
		var args = parts.Skip( 1 ).ToArray();

		try
		{
			entry.Handler?.Invoke( sender, args );
		}
		catch ( Exception ex )
		{
			Log.Error( $"[DarkRP ChatCmd] /{key}: {ex.Message}" );
		}

		return true;
	}

	/// <summary>
	/// Сканировать сборку и зарегистрировать все [ChatCommand] методы.
	/// </summary>
	internal static void AutoRegisterFromAssembly()
	{
		foreach ( var type in TypeLibrary.GetTypes() )
		{
			foreach ( var method in type.Methods )
			{
				var attr = method.GetCustomAttribute<ChatCommandAttribute>();
				if ( attr is null ) continue;

				if ( !method.IsStatic )
				{
					Log.Warning( $"[DarkRP ChatCmd] {type.Name}.{method.Name}: [ChatCommand] работает только на static методах." );
					continue;
				}

				Add( attr.Command, ( conn, args ) =>
				{
					method.Invoke( null, new object[] { conn, args } );
					return true;
				}, cooldown: attr.Cooldown );
			}
		}
	}
}
