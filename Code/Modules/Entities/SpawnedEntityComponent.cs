// Source: gamemode/entities/entities/* (общая база для spawned_*)
// Lua: ENT.Owner / ENT:CPPIGetOwner() / FPP prop protection
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Базовый компонент для всех заспавненных DarkRP сущностей:
/// - принтеры денег
/// - оружие на полу (spawned_weapon)
/// - ящики (spawned_shipment)
/// - патроны (spawned_ammo)
/// - деньги (spawned_money)
///
/// Реализует владение, лимиты на игрока и prop protection (только владелец/admin).
/// Lua: ENT.Owner + FPP/CPPI hooks.
/// </summary>
public sealed class SpawnedEntityComponent : Component
{
	/// <summary>SteamID владельца. 0 = ничей.</summary>
	[Sync] public ulong OwnerSteamId { get; set; } = 0;

	/// <summary>Имя владельца (для UI). Lua: ENT:GetOwner():Nick()</summary>
	[Sync] public string OwnerName { get; set; } = "";

	/// <summary>Тип сущности (для статистики/UI). Например "money_printer", "spawned_weapon".</summary>
	[Sync] public string EntityType { get; set; } = "";

	/// <summary>Lua: ENT:CPPIGetOwner()</summary>
	public Connection? GetOwner()
	{
		if ( OwnerSteamId == 0 ) return null;
		return Connection.All.FirstOrDefault( c => c.SteamId == OwnerSteamId );
	}

	/// <summary>
	/// Lua: FPP.CanTouch(ply, ent) — может ли игрок взаимодействовать.
	/// Только владелец и админ.
	/// </summary>
	public bool CanTouch( Connection ply )
	{
		if ( OwnerSteamId == 0 ) return true; // Ничейное
		if ( OwnerSteamId == ply.SteamId ) return true;
		if ( ply.IsHost ) return true; // admin
		return false;
	}

	public void SetOwner( Connection owner )
	{
		if ( !Networking.IsHost ) return;
		OwnerSteamId = owner.SteamId;
		OwnerName = owner.DisplayName;
	}
}

/// <summary>
/// Реестр лимитов на спавн сущностей. Lua: GAMEMODE.Config.<entity>limit
/// </summary>
public static class EntityLimits
{
	private static readonly Dictionary<string, int> _limits = new()
	{
		["money_printer"] = 3,
		["meth_lab"] = 2,
		["drug_lab"] = 2,
		["microwave"] = 2,
		["spawned_weapon"] = 30,
		["spawned_shipment"] = 8,
		["spawned_ammo"] = 30,
	};

	public static int GetLimit( string type ) =>
		_limits.GetValueOrDefault( type, 10 );

	/// <summary>Сколько сущностей данного типа уже принадлежит игроку.</summary>
	public static int CountForPlayer( ulong steamId, string type ) =>
		Game.ActiveScene.GetAllComponents<SpawnedEntityComponent>()
			.Count( e => e.OwnerSteamId == steamId && e.EntityType == type );

	/// <summary>Можно ли заспавнить ещё одну?</summary>
	public static bool CanSpawn( ulong steamId, string type ) =>
		CountForPlayer( steamId, type ) < GetLimit( type );
}
