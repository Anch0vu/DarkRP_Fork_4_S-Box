// Source: gamemode/modules/money/sh_interface.lua (canAfford, getMoney)
// Source: gamemode/modules/money/sv_interface.lua (addMoney, setMoney)
// Source: gamemode/modules/police/sh_init.lua (isCP, isArrested, isWanted, isMayor, isChief)
// Source: gamemode/modules/hitmenu/sh_init.lua (isHitman)
// Source: gamemode/modules/medic/sh_init.lua (isMedic)
// Source: gamemode/modules/base/sh_createitems.lua (getJobTable)
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Extension methods на Connection, имитирующие Lua plyMeta методы из DarkRP.
/// Lua: ply:addMoney(amount), ply:getMoney(), ply:isCP(), и т.д.
/// </summary>
public static class PlayerExtensions
{
	// ────────────────────────────────── Деньги ──────────────────────────────────

	/// <summary>Lua: ply:addMoney(amount)</summary>
	public static void AddMoney( this Connection ply, int amount )
	{
		var comp = ply.GetDarkRPComponent();
		if ( comp is null ) return;
		comp.Money = Math.Max( 0, comp.Money + amount );
	}

	/// <summary>Lua: ply:getMoney()</summary>
	public static int GetMoney( this Connection ply ) =>
		ply.GetDarkRPComponent()?.Money ?? 0;

	/// <summary>Lua: ply:canAfford(amount)</summary>
	public static bool CanAfford( this Connection ply, int amount ) =>
		ply.GetMoney() >= amount;

	/// <summary>Lua: ply:setMoney(amount)</summary>
	public static void SetMoney( this Connection ply, int amount )
	{
		var comp = ply.GetDarkRPComponent();
		if ( comp is null ) return;
		comp.Money = Math.Max( 0, amount );
	}

	// ────────────────────────────────── Работа ──────────────────────────────────

	/// <summary>
	/// Lua: ply:getJobTable() — возвращает текущую работу игрока.
	/// </summary>
	public static Job? GetJob( this Connection ply )
	{
		var comp = ply.GetDarkRPComponent();
		if ( comp is null ) return null;
		return DarkRP.GetJob( comp.JobId );
	}

	/// <summary>
	/// Lua: ply:team() / team смена через sv_jobs.lua
	/// </summary>
	public static void SetJob( this Connection ply, string jobId )
	{
		var comp = ply.GetDarkRPComponent();
		if ( comp is null ) return;
		var oldJob = ply.GetJob();
		comp.JobId = jobId;
		var newJob = DarkRP.GetJob( jobId );
		Hook.Run( "PlayerChangedJob", ply, oldJob, newJob );
	}

	// ────────────────────────────────── Статусы (CP) ──────────────────────────

	/// <summary>Lua: ply:isCP() — полиция/мэр/SWAT</summary>
	public static bool IsCP( this Connection ply ) =>
		ply.GetJob()?.IsCP ?? false;

	/// <summary>Lua: ply:isMayor()</summary>
	public static bool IsMayor( this Connection ply ) =>
		ply.GetJob()?.IsMayor ?? false;

	/// <summary>Lua: ply:isChief()</summary>
	public static bool IsChief( this Connection ply ) =>
		ply.GetJob()?.IsChief ?? false;

	/// <summary>Lua: ply:isHitman()</summary>
	public static bool IsHitman( this Connection ply ) =>
		ply.GetJob()?.IsHitman ?? false;

	/// <summary>Lua: ply:isMedic()</summary>
	public static bool IsMedic( this Connection ply ) =>
		ply.GetJob()?.IsMedic ?? false;

	/// <summary>Lua: ply:isCook()</summary>
	public static bool IsCook( this Connection ply ) =>
		ply.GetJob()?.IsCook ?? false;

	// ────────────────────────────────── Арест / Розыск ────────────────────────

	/// <summary>Lua: ply:isArrested()</summary>
	public static bool IsArrested( this Connection ply ) =>
		ply.GetDarkRPComponent()?.IsArrested ?? false;

	/// <summary>Lua: ply:isWanted()</summary>
	public static bool IsWanted( this Connection ply ) =>
		ply.GetDarkRPComponent()?.IsWanted ?? false;

	// ────────────────────────────────── Уведомления ───────────────────────────

	/// <summary>
	/// Lua: DarkRP.notify(ply, type, duration, message)
	/// Отправляет уведомление конкретному игроку.
	/// </summary>
	public static void Notify( this Connection ply, string message,
		NotifyType type = NotifyType.Info, float duration = 5f )
	{
		DarkRP.SendNotification( ply, message, type, duration );
	}

	// ────────────────────────────────── Внутреннее ────────────────────────────

	internal static DarkRPPlayerComponent? GetDarkRPComponent( this Connection ply )
	{
		// Найти GameObject, принадлежащий этому соединению
		foreach ( var comp in Game.ActiveScene.GetAllComponents<DarkRPPlayerComponent>() )
		{
			if ( comp.Network.Owner == ply )
				return comp;
		}
		return null;
	}
}
