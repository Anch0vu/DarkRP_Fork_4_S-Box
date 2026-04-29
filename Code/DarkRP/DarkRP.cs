// Source: gamemode/modules/base/sh_createitems.lua — статические регистраторы
// Source: gamemode/modules/base/sh_interface.lua — API stubs
// Source: gamemode/modules/base/sv_interface.lua — server implementations
// Source: gamemode/modules/base/sh_util.lua — утилиты
// Главный фасад DarkRP API. Цель: максимальное сходство с Lua DarkRP API,
// чтобы разработчик с опытом Lua мог читать и писать C# код без глубокого погружения.
using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Главный статический класс DarkRP — точка входа для всего API.
///
/// Lua: глобальная таблица DarkRP (DarkRP.createJob, DarkRP.notify, и т.д.)
/// </summary>
public static partial class DarkRP
{
	// ═══════════════════════════════════════════════════════════════════════════
	//  Работы / Jobs
	// ═══════════════════════════════════════════════════════════════════════════

	private static readonly Dictionary<string, Job> _jobs = new( StringComparer.OrdinalIgnoreCase );

	/// <summary>
	/// Зарегистрировать работу.
	/// Lua: DarkRP.createJob(name, table)
	///
	/// Пример:
	/// <code>
	/// DarkRP.AddJob( new Job { Id = "police", Name = "Police Officer", IsCP = true, Salary = 65 } );
	/// </code>
	/// </summary>
	public static void AddJob( Job job )
	{
		if ( string.IsNullOrWhiteSpace( job.Id ) )
		{
			Log.Error( "[DarkRP] AddJob: Id не может быть пустым." );
			return;
		}
		_jobs[job.Id] = job;
	}

	/// <summary>
	/// Получить работу по ID.
	/// Lua: RPExtraTeams[teamId]
	/// </summary>
	public static Job? GetJob( string id ) =>
		_jobs.TryGetValue( id, out var job ) ? job : null;

	/// <summary>
	/// Все зарегистрированные работы.
	/// Lua: RPExtraTeams
	/// </summary>
	public static IReadOnlyDictionary<string, Job> GetAllJobs() => _jobs;

	// ═══════════════════════════════════════════════════════════════════════════
	//  Кораблёвки / Shipments
	// ═══════════════════════════════════════════════════════════════════════════

	private static readonly Dictionary<string, Shipment> _shipments = new( StringComparer.OrdinalIgnoreCase );

	/// <summary>
	/// Lua: DarkRP.createShipment(name, table)
	/// </summary>
	public static void AddShipment( Shipment shipment ) => _shipments[shipment.Id] = shipment;

	public static Shipment? GetShipment( string id ) =>
		_shipments.TryGetValue( id, out var s ) ? s : null;

	public static IReadOnlyDictionary<string, Shipment> GetAllShipments() => _shipments;

	// ═══════════════════════════════════════════════════════════════════════════
	//  Покупаемые сущности / BuyableEntities
	// ═══════════════════════════════════════════════════════════════════════════

	private static readonly Dictionary<string, BuyableEntity> _entities = new( StringComparer.OrdinalIgnoreCase );

	/// <summary>
	/// Lua: DarkRP.createEntity(name, table)
	/// </summary>
	public static void AddBuyableEntity( BuyableEntity ent ) => _entities[ent.Id] = ent;

	public static BuyableEntity? GetBuyableEntity( string id ) =>
		_entities.TryGetValue( id, out var e ) ? e : null;

	public static IReadOnlyDictionary<string, BuyableEntity> GetAllBuyableEntities() => _entities;

	// ═══════════════════════════════════════════════════════════════════════════
	//  Транспорт / Vehicles
	// ═══════════════════════════════════════════════════════════════════════════

	private static readonly List<BuyableVehicle> _vehicles = new();

	/// <summary>Lua: DarkRP.createVehicle(name, table)</summary>
	public static void AddVehicle( BuyableVehicle vehicle ) => _vehicles.Add( vehicle );

	public static IReadOnlyList<BuyableVehicle> GetVehicles() => _vehicles;

	// ═══════════════════════════════════════════════════════════════════════════
	//  Повестки / Agendas
	// ═══════════════════════════════════════════════════════════════════════════

	private static readonly Dictionary<string, Agenda> _agendas = new( StringComparer.OrdinalIgnoreCase );

	/// <summary>Lua: DarkRP.createAgenda(title, managerJob, listenerJobs)</summary>
	public static void AddAgenda( Agenda agenda ) => _agendas[agenda.Title] = agenda;

	public static Agenda? GetAgenda( string title ) =>
		_agendas.TryGetValue( title, out var a ) ? a : null;

	// ═══════════════════════════════════════════════════════════════════════════
	//  Еда (hungermod) / Food
	// ═══════════════════════════════════════════════════════════════════════════

	private static readonly List<FoodItem> _food = new();

	/// <summary>Lua: DarkRP.createFood(name, model, energy, price)</summary>
	public static void AddFood( FoodItem food ) => _food.Add( food );

	public static IReadOnlyList<FoodItem> GetFoodItems() => _food;

	// ═══════════════════════════════════════════════════════════════════════════
	//  Патроны / AmmoTypes
	// ═══════════════════════════════════════════════════════════════════════════

	private static readonly List<AmmoType> _ammoTypes = new();

	/// <summary>Lua: DarkRP.createAmmoType(ammoType, table)</summary>
	public static void AddAmmoType( AmmoType ammo ) => _ammoTypes.Add( ammo );

	public static IReadOnlyList<AmmoType> GetAmmoTypes() => _ammoTypes;

	// ═══════════════════════════════════════════════════════════════════════════
	//  Чат-команды
	// ═══════════════════════════════════════════════════════════════════════════

	/// <summary>
	/// Lua: DarkRP.defineChatCommand(name, callback, cooldown)
	/// </summary>
	public static void AddChatCommand( string command,
		Func<Connection, string[], bool> callback,
		string description = "",
		float cooldown = 0f )
	{
		ChatCommandRegistry.Add( command, callback, description, cooldown );
	}

	// ═══════════════════════════════════════════════════════════════════════════
	//  Уведомления
	// ═══════════════════════════════════════════════════════════════════════════

	/// <summary>
	/// Отправить уведомление игроку.
	/// Lua: DarkRP.notify(ply, type, duration, message)
	/// </summary>
	public static void Notify( Connection ply, NotifyType type, float duration, string message )
	{
		SendNotification( ply, message, type, duration );
	}

	[Rpc.Owner]
	internal static void SendNotification( Connection target, string message, NotifyType type, float duration )
	{
		// На клиенте — добавляем в очередь уведомлений для HUD
		NotificationQueue.Push( new Notification { Message = message, Type = type, Duration = duration } );
	}

	// ═══════════════════════════════════════════════════════════════════════════
	//  Деньги — утилиты
	// ═══════════════════════════════════════════════════════════════════════════

	/// <summary>
	/// Lua: DarkRP.formatMoney(amount) → "$1,500"
	/// </summary>
	public static string FormatMoney( int amount ) => $"${amount:N0}";

	/// <summary>
	/// Lua: DarkRP.payPlayer(from, to, amount)
	/// Перевод денег между игроками с проверкой.
	/// </summary>
	public static bool PayPlayer( Connection from, Connection to, int amount )
	{
		if ( amount <= 0 || !from.CanAfford( amount ) ) return false;
		from.AddMoney( -amount );
		to.AddMoney( amount );
		return true;
	}

	// ═══════════════════════════════════════════════════════════════════════════
	//  Логирование
	// ═══════════════════════════════════════════════════════════════════════════

	/// <summary>Lua: DarkRP.log(text, colour)</summary>
	public static void Log( string text, Color? color = null )
	{
		Sandbox.Log.Info( $"[DarkRP] {text}" );
		// TODO: записывать в файл через DataManager (phase-2)
	}

	// ═══════════════════════════════════════════════════════════════════════════
	//  Локализация
	// ═══════════════════════════════════════════════════════════════════════════

	/// <summary>
	/// Lua: DarkRP.getPhrase(key, ...)
	/// Временная заглушка — вернёт ключ пока не реализован language модуль (phase-2).
	/// </summary>
	public static string Translate( string key, params object[] args )
	{
		// TODO: реализовать через Language модуль (phase-2, module: language)
		return args.Length > 0 ? string.Format( key, args ) : key;
	}

	// ═══════════════════════════════════════════════════════════════════════════
	//  Инициализация
	// ═══════════════════════════════════════════════════════════════════════════

	/// <summary>
	/// Вызывается GamemodeSystem при старте.
	/// Регистрирует все [DarkRPHook] и [ChatCommand] из сборки.
	/// Lua: include() цепочка в init.lua / cl_init.lua
	/// </summary>
	internal static void Initialize()
	{
		Hook.AutoRegisterFromAssembly();
		ChatCommandRegistry.AutoRegisterFromAssembly();
		Hook.Run( "LoadCustomItems" );
		Sandbox.Log.Info( "[DarkRP] Initialized." );
	}
}

// ─── Заглушки данных (будут GameResource в phase-1 расширениях) ───────────────

/// <summary>Lua: DarkRP.createShipment — ящик с оружием</summary>
public sealed class Shipment
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public string Model { get; set; } = "";
	public string EntityClass { get; set; } = "";
	public int Price { get; set; } = 0;
	public int Amount { get; set; } = 1;
	public bool SeparatelyBuyable { get; set; } = false;
	public int SeparatePrice { get; set; } = 0;
	public List<string> AllowedJobs { get; set; } = new();
}

/// <summary>Lua: DarkRP.createEntity — покупаемая сущность</summary>
public sealed class BuyableEntity
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public string EntityClass { get; set; } = "";
	public string Model { get; set; } = "";
	public int Price { get; set; } = 0;
	public int MaxPerPlayer { get; set; } = 0;
	public string Command { get; set; } = "";
	public List<string> AllowedJobs { get; set; } = new();
}

/// <summary>Lua: DarkRP.createVehicle</summary>
public sealed class BuyableVehicle
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public string Model { get; set; } = "";
	public int Price { get; set; } = 0;
	public List<string> AllowedJobs { get; set; } = new();
}

/// <summary>Lua: DarkRP.createAgenda</summary>
public sealed class Agenda
{
	public string Title { get; set; } = "";
	public string ManagerJobId { get; set; } = "";
	public List<string> ListenerJobIds { get; set; } = new();
	public string Text { get; set; } = "";
}

/// <summary>Lua: DarkRP.createFood (hungermod)</summary>
public sealed class FoodItem
{
	public string Name { get; set; } = "";
	public string Model { get; set; } = "";
	public int HungerRestored { get; set; } = 0;
	public int Price { get; set; } = 0;
}

/// <summary>Lua: DarkRP.createAmmoType</summary>
public sealed class AmmoType
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public string Model { get; set; } = "";
	public int Price { get; set; } = 0;
	public int AmountGiven { get; set; } = 0;
}

/// <summary>
/// Клиентская очередь уведомлений для HUD.
/// </summary>
public static class NotificationQueue
{
	private static readonly List<Notification> _queue = new();

	public static void Push( Notification n )
	{
		n.ExpiresAt = n.Duration;
		_queue.Add( n );
	}

	public static IReadOnlyList<Notification> GetActive()
	{
		_queue.RemoveAll( n => !n.ExpiresAt );
		return _queue;
	}
}
