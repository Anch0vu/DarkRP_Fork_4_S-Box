// Source: gamemode/modules/base/sh_gamemode_functions.lua
// Реализует систему хуков аналогичную Lua hook.Add / hook.Run / hook.Remove
using System;
using System.Collections.Generic;
using System.Reflection;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Атрибут для автоматической регистрации метода как DarkRP-хука.
/// Lua: hook.Add(eventName, identifier, callback)
///
/// Пример:
/// <code>
/// [DarkRPHook( "PlayerSpawn" )]
/// public static void OnPlayerSpawn( PlayerController ply ) { }
/// </code>
/// </summary>
[AttributeUsage( AttributeTargets.Method, AllowMultiple = true )]
public sealed class DarkRPHookAttribute : Attribute
{
	public string EventName { get; }
	public DarkRPHookAttribute( string eventName ) => EventName = eventName;
}

/// <summary>
/// Один зарегистрированный обработчик хука.
/// Lua: { identifier = callback }
/// </summary>
internal sealed class HookHandler
{
	public string Identifier { get; init; } = "";
	public Delegate Callback { get; init; } = null!;
}

/// <summary>
/// Реестр DarkRP хуков.
/// Lua: hook.Add / hook.Remove / hook.Run
///
/// Использование:
/// <code>
/// // Программная регистрация (аналог hook.Add в Lua):
/// DarkRP.Hook.Add( "PlayerSpawn", "MyMod_Spawn", (PlayerController ply) => { ... } );
///
/// // Вызов хука (аналог hook.Run):
/// DarkRP.Hook.Run( "PlayerSpawn", ply );
/// </code>
/// </summary>
public static class Hook
{
	private static readonly Dictionary<string, List<HookHandler>> _hooks = new();

	/// <summary>
	/// Зарегистрировать обработчик хука.
	/// Lua: hook.Add(eventName, identifier, callback)
	/// </summary>
	public static void Add( string eventName, string identifier, Delegate callback )
	{
		if ( !_hooks.TryGetValue( eventName, out var list ) )
		{
			list = new List<HookHandler>();
			_hooks[eventName] = list;
		}

		// Заменить если уже есть с тем же identifier
		list.RemoveAll( h => h.Identifier == identifier );
		list.Add( new HookHandler { Identifier = identifier, Callback = callback } );
	}

	/// <summary>
	/// Удалить обработчик.
	/// Lua: hook.Remove(eventName, identifier)
	/// </summary>
	public static void Remove( string eventName, string identifier )
	{
		if ( _hooks.TryGetValue( eventName, out var list ) )
			list.RemoveAll( h => h.Identifier == identifier );
	}

	/// <summary>
	/// Вызвать все обработчики хука.
	/// Lua: hook.Run(eventName, ...)
	/// Возвращает первый non-null результат (аналог GMod — первый хук который return'ит значение).
	/// </summary>
	public static object? Run( string eventName, params object?[] args )
	{
		if ( !_hooks.TryGetValue( eventName, out var list ) )
			return null;

		// Итерируем по копии, т.к. хук может удалить себя в процессе
		foreach ( var handler in list.ToArray() )
		{
			try
			{
				var result = handler.Callback.DynamicInvoke( args );
				if ( result is not null && result is not bool b || result is bool boolVal && boolVal == false )
					return result;
			}
			catch ( Exception ex )
			{
				Log.Error( $"[DarkRP Hook] Error in '{eventName}' / '{handler.Identifier}': {ex.Message}" );
			}
		}

		return null;
	}

	/// <summary>
	/// Сканировать сборку и зарегистрировать все [DarkRPHook] методы.
	/// Вызывается один раз при старте игры.
	/// </summary>
	internal static void AutoRegisterFromAssembly()
	{
		foreach ( var type in TypeLibrary.GetTypes() )
		{
			foreach ( var method in type.Methods )
			{
				var attr = method.GetCustomAttribute<DarkRPHookAttribute>();
				if ( attr is null ) continue;

				if ( !method.IsStatic )
				{
					Log.Warning( $"[DarkRP Hook] {type.Name}.{method.Name}: [DarkRPHook] работает только на static методах." );
					continue;
				}

				var del = method.CreateDelegate( typeof( Action ) )
				       ?? method.CreateDelegate( typeof( Action<object?[]> ) );

				// Оборачиваем в Action<object?[]> для унифицированного вызова
				Add( attr.EventName, $"{type.Name}.{method.Name}", new Action<object?[]>( args =>
				{
					method.Invoke( null, args );
				} ) );
			}
		}
	}
}
