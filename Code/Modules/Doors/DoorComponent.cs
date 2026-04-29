// Source: gamemode/modules/doorsystem/sh_interface.lua
// Source: gamemode/modules/doorsystem/sv_interface.lua
// Source: gamemode/modules/doorsystem/sv_doorvars.lua
// Lua: ent:keysOwn, ent:keysUnOwn, ent:isLocked, ent:setKeysTitle и т.д.
using System.Collections.Generic;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Компонент двери — синхронизирует данные владения и замка.
/// Lua: DarkRP.registerDoorVar(...) + ent:keysOwn/keysUnOwn/keysLock/keysUnLock
///
/// Прикрепляется к каждому GameObject двери при загрузке карты.
/// </summary>
public sealed class DoorComponent : Component
{
	// ─── Синхронизированные данные двери (аналог DarkRP.registerDoorVar) ────

	/// <summary>Lua: DarkRP.registerDoorVar("owner", ...)</summary>
	[Sync] public ulong OwnerSteamId { get; set; } = 0;

	/// <summary>Имя владельца для отображения в UI.</summary>
	[Sync] public string OwnerName { get; set; } = "";

	/// <summary>Lua: DarkRP.registerDoorVar("nonOwnable", net.WriteBool, net.ReadBool)</summary>
	[Sync] public bool NonOwnable { get; set; } = false;

	/// <summary>Lua: DarkRP.registerDoorVar("title", net.WriteString, net.ReadString)</summary>
	[Sync] public string Title { get; set; } = "";

	/// <summary>Lua: door:IsLocked() — нужен ключ/лок-пик для открытия.</summary>
	[Sync] public bool IsLocked { get; set; } = false;

	/// <summary>
	/// Дополнительные совладельцы (extraOwners).
	/// Lua: DarkRP.registerDoorVar("extraOwners", writeNumBoolTbl, readNumBoolTbl)
	/// </summary>
	[Sync( SyncFlags.FromHost )] public List<ulong> ExtraOwners { get; set; } = new();

	/// <summary>
	/// Работы, которым разрешено владеть этой дверью.
	/// Lua: DarkRP.registerDoorVar("teamOwn", writeNumBoolTbl, readNumBoolTbl)
	/// </summary>
	[Sync( SyncFlags.FromHost )] public List<string> TeamOwn { get; set; } = new();

	/// <summary>Группа для блокировки по группе. Lua: DarkRP.registerDoorVar("groupOwn", ...)</summary>
	[Sync] public string GroupOwn { get; set; } = "";

	// ─── Производные свойства ────────────────────────────────────────────────

	public bool IsOwned => OwnerSteamId != 0;

	/// <summary>Lua: ent:isKeysOwned()</summary>
	public bool IsKeysOwned() => IsOwned;

	/// <summary>Lua: ent:isKeysOwnable()</summary>
	public bool IsKeysOwnable() => !NonOwnable;

	/// <summary>Lua: ent:getDoorOwner() — возвращает Connection владельца или null</summary>
	public Connection? GetOwner()
	{
		if ( OwnerSteamId == 0 ) return null;
		foreach ( var conn in Connection.All )
			if ( conn.SteamId == OwnerSteamId ) return conn;
		return null;
	}

	/// <summary>Lua: ent:isMasterOwner(ply)</summary>
	public bool IsMasterOwner( Connection ply ) => OwnerSteamId == ply.SteamId;

	/// <summary>Lua: ent:isKeysOwnedBy(ply) — мастер-владелец или совладелец</summary>
	public bool IsOwnedBy( Connection ply ) =>
		IsMasterOwner( ply ) || ExtraOwners.Contains( ply.SteamId );

	// ─── Серверные методы управления ─────────────────────────────────────────

	/// <summary>
	/// Установить владельца двери.
	/// Lua: ent:keysOwn(ply)
	/// </summary>
	public void Own( Connection ply )
	{
		if ( !Networking.IsHost ) return;
		OwnerSteamId = ply.SteamId;
		OwnerName = ply.DisplayName;
		Hook.Run( "DoorOwned", this, ply );
		_ = DoorManager.SaveDoorAsync( this );
	}

	/// <summary>
	/// Снять владение.
	/// Lua: ent:keysUnOwn(ply)
	/// </summary>
	public void UnOwn()
	{
		if ( !Networking.IsHost ) return;
		OwnerSteamId = 0;
		OwnerName = "";
		ExtraOwners.Clear();
		Title = "";
		Hook.Run( "DoorUnowned", this );
		_ = DoorManager.SaveDoorAsync( this );
	}

	/// <summary>Lua: ent:keysLock()</summary>
	public void Lock()
	{
		if ( !Networking.IsHost ) return;
		IsLocked = true;
		Hook.Run( "onKeysLocked", this );
	}

	/// <summary>Lua: ent:keysUnLock()</summary>
	public void Unlock()
	{
		if ( !Networking.IsHost ) return;
		IsLocked = false;
		Hook.Run( "onKeysUnlocked", this );
	}

	/// <summary>Lua: ent:addKeysDoorOwner(ply)</summary>
	public void AddOwner( Connection ply )
	{
		if ( !Networking.IsHost ) return;
		if ( !ExtraOwners.Contains( ply.SteamId ) )
			ExtraOwners.Add( ply.SteamId );
		_ = DoorManager.SaveDoorAsync( this );
	}

	/// <summary>Lua: ent:removeKeysDoorOwner(ply)</summary>
	public void RemoveOwner( Connection ply )
	{
		if ( !Networking.IsHost ) return;
		ExtraOwners.Remove( ply.SteamId );
		_ = DoorManager.SaveDoorAsync( this );
	}

	/// <summary>Lua: ent:setKeysNonOwnable(bool)</summary>
	public void SetNonOwnable( bool value )
	{
		if ( !Networking.IsHost ) return;
		NonOwnable = value;
		_ = DoorManager.SaveDoorAsync( this );
	}

	/// <summary>Lua: ent:setKeysTitle(string)</summary>
	public void SetTitle( string title )
	{
		if ( !Networking.IsHost ) return;
		Title = title;
		_ = DoorManager.SaveDoorAsync( this );
	}

	/// <summary>Lua: ent:isDoor()</summary>
	public static bool IsDoor( GameObject go ) => go.GetComponent<DoorComponent>() is not null;
}
