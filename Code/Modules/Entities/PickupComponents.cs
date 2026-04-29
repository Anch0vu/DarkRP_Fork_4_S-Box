// Source: entities/entities/spawned_money/, spawned_weapon/, spawned_shipment/, spawned_ammo/
// Lua: ENT:Use(activator) — игрок берёт предмет/деньги
using Sandbox;

namespace SboxDarkRP;

// ─── Деньги на полу ─────────────────────────────────────────────────────────

/// <summary>
/// Моник на полу. Lua: entities/entities/spawned_money/init.lua
/// Поднимается через Use (E) — добавляет деньги владельцу/первому подобравшему.
/// </summary>
public sealed class SpawnedMoneyComponent : Component, Component.IPressable
{
	[Sync] public int Amount { get; set; } = 0;

	public bool Press( IPressable.Event e )
	{
		if ( !Networking.IsHost ) return false;
		var ply = e.Source?.Network?.Owner;
		if ( ply is null ) return false;

		ply.AddMoney( Amount );
		ply.Notify( LanguageSystem.Get( "found_money", DarkRP.FormatMoney( Amount ) ), NotifyType.Info );
		GameObject.Destroy();
		return true;
	}
}

// ─── Оружие на полу ─────────────────────────────────────────────────────────

/// <summary>
/// Lua: entities/entities/spawned_weapon/init.lua
/// </summary>
public sealed class SpawnedWeaponComponent : Component, Component.IPressable
{
	[Sync] public string WeaponClass { get; set; } = "";
	[Sync] public int Price { get; set; } = 0;

	public bool Press( IPressable.Event e )
	{
		if ( !Networking.IsHost ) return false;
		var ply = e.Source?.Network?.Owner;
		if ( ply is null ) return false;

		// Lua: ply:Give(self.weapon)
		// TODO (phase-5): интеграция с реальной WeaponComponent — нужны .vmdl и WeaponDefinition
		ply.Notify( $"Подобрано: {WeaponClass}", NotifyType.Info );
		Hook.Run( "playerPickedUpWeapon", ply, WeaponClass );

		GameObject.Destroy();
		return true;
	}
}

// ─── Ящик с оружием ─────────────────────────────────────────────────────────

/// <summary>
/// Lua: entities/entities/spawned_shipment/init.lua
/// При Use выдаёт одно оружие и уменьшает Count. На 0 — удаляется.
/// </summary>
public sealed class SpawnedShipmentComponent : Component, Component.IPressable
{
	[Sync] public string ShipmentName { get; set; } = "";
	[Sync] public string WeaponClass { get; set; } = "";
	[Sync] public int Count { get; set; } = 10; // Lua: ENT.count = shipment.amount

	public bool Press( IPressable.Event e )
	{
		if ( !Networking.IsHost ) return false;
		var ply = e.Source?.Network?.Owner;
		if ( ply is null ) return false;

		var spawned = GameObject.GetComponent<SpawnedEntityComponent>();
		if ( spawned is not null && !spawned.CanTouch( ply ) )
		{
			ply.Notify( "Этот ящик не ваш!", NotifyType.Error );
			return false;
		}

		if ( Count <= 0 )
		{
			GameObject.Destroy();
			return false;
		}

		Count--;
		ply.Notify( $"Взято {WeaponClass} из ящика. Осталось: {Count}", NotifyType.Info );
		Hook.Run( "playerPickedUpWeapon", ply, WeaponClass );

		if ( Count <= 0 )
			GameObject.Destroy();

		return true;
	}
}

// ─── Патроны на полу ─────────────────────────────────────────────────────────

/// <summary>
/// Lua: entities/entities/spawned_ammo/init.lua
/// </summary>
public sealed class SpawnedAmmoComponent : Component, Component.IPressable
{
	[Sync] public string AmmoId { get; set; } = "";
	[Sync] public int Amount { get; set; } = 30;

	public bool Press( IPressable.Event e )
	{
		if ( !Networking.IsHost ) return false;
		var ply = e.Source?.Network?.Owner;
		if ( ply is null ) return false;

		// Lua: ply:GiveAmmo(self.amount, self.ammoType)
		ply.Notify( $"Подобрано {Amount} патронов ({AmmoId})", NotifyType.Info );
		GameObject.Destroy();
		return true;
	}
}
