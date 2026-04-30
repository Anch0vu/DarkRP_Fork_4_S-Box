// Source: gamemode/modules/base/sv_purchasing.lua
// Source: gamemode/modules/money/sv_money.lua (cheque)
// Lua: DarkRP.defineChatCommand("buy", BuyPistol)
//      DarkRP.defineChatCommand("buyshipment", BuyShipment)
//      DarkRP.defineChatCommand("buyvehicle", BuyVehicle)
//      DarkRP.defineChatCommand("buyammo", BuyAmmo)
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Система покупок — команды /buy, /buyshipment, /buyvehicle, /buyammo, /price.
/// Lua: gamemode/modules/base/sv_purchasing.lua
/// </summary>
public static class PurchasingSystem
{
	// ─── /buy — купить оружие по одному (separate=true) ──────────────────────
	// Lua: DarkRP.defineChatCommand("buy", BuyPistol, 0.2)
	[ChatCommand( "/buy", Cooldown = 0.2f )]
	public static void CmdBuy( Connection ply, string[] args )
	{
		if ( args.Length == 0 )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var name = string.Join( " ", args );
		var shipment = FindShipmentByName( name );

		if ( shipment is null || !shipment.SeparatelyBuyable )
		{
			ply.Notify( LanguageSystem.Get( "unavailable", LanguageSystem.Get( "weapon_" ) ), NotifyType.Error );
			return;
		}

		var jobId = ply.GetDarkRPComponent()?.JobId ?? "";
		if ( shipment.AllowedJobs.Count > 0 && !shipment.AllowedJobs.Contains( jobId ) )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", "/buy" ), NotifyType.Error );
			return;
		}

		if ( ply.IsArrested() )
		{
			ply.Notify( LanguageSystem.Get( "unable", "/buy" ), NotifyType.Error );
			return;
		}

		var hookResult = Hook.Run( "CanBuyPistol", ply, shipment );
		if ( hookResult is false ) return;

		var price = shipment.SeparatePrice;
		if ( !ply.CanAfford( price ) )
		{
			ply.Notify( LanguageSystem.Get( "cant_afford" ), NotifyType.Error );
			return;
		}

		ply.AddMoney( -price );
		ply.Notify( LanguageSystem.Get( "you_bought", name, DarkRP.FormatMoney( price ) ), NotifyType.Info );
		Hook.Run( "PlayerBoughtShipment", ply, shipment );

		// Phase-4: одиночное оружие как пикап перед игроком (примитив-куб)
		var pos = EntitySpawner.GetSpawnPositionInFrontOf( ply );
		var go = EntitySpawner.SpawnPrimitive( pos, "spawned_weapon", ply,
			new Color( 0.6f, 0.6f, 0.7f ), new Vector3( 0.4f, 0.1f, 0.1f ) );
		var weapon = go.Components.Create<SpawnedWeaponComponent>();
		weapon.WeaponClass = name;
		weapon.Price = price;

		DarkRP.Log( $"{ply.DisplayName} купил {name} за {DarkRP.FormatMoney( price )}" );
	}

	// ─── /buyshipment — купить ящик с оружием ───────────────────────────────
	// Lua: DarkRP.defineChatCommand("buyshipment", BuyShipment)
	[ChatCommand( "/buyshipment", Cooldown = 0.5f )]
	public static void CmdBuyShipment( Connection ply, string[] args )
	{
		if ( args.Length == 0 )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var name = string.Join( " ", args );
		var shipment = FindShipmentByName( name );

		if ( shipment is null )
		{
			ply.Notify( LanguageSystem.Get( "unavailable", LanguageSystem.Get( "shipment" ) ), NotifyType.Error );
			return;
		}

		var jobId = ply.GetDarkRPComponent()?.JobId ?? "";
		if ( shipment.AllowedJobs.Count > 0 && !shipment.AllowedJobs.Contains( jobId ) )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", "/buyshipment" ), NotifyType.Error );
			return;
		}

		if ( ply.IsArrested() )
		{
			ply.Notify( LanguageSystem.Get( "unable", "/buyshipment" ), NotifyType.Error );
			return;
		}

		var hookResult = Hook.Run( "CanBuyShipment", ply, shipment );
		if ( hookResult is false ) return;

		if ( !ply.CanAfford( shipment.Price ) )
		{
			ply.Notify( LanguageSystem.Get( "cant_afford" ), NotifyType.Error );
			return;
		}

		ply.AddMoney( -shipment.Price );
		ply.Notify( LanguageSystem.Get( "you_bought", name, DarkRP.FormatMoney( shipment.Price ) ), NotifyType.Info );
		Hook.Run( "PlayerBoughtShipment", ply, shipment );

		// Phase-4: спавн ящика перед игроком (большой деревянный куб)
		var pos = EntitySpawner.GetSpawnPositionInFrontOf( ply );
		var go = EntitySpawner.SpawnPrimitive( pos, "spawned_shipment", ply,
			new Color( 0.6f, 0.4f, 0.2f ), new Vector3( 0.6f, 0.6f, 0.4f ) );
		var crate = go.Components.Create<SpawnedShipmentComponent>();
		crate.ShipmentName = name;
		crate.WeaponClass = shipment.EntityClass;
		crate.Count = shipment.Amount;

		DarkRP.Log( $"{ply.DisplayName} купил ящик {name} за {DarkRP.FormatMoney( shipment.Price )}" );
	}

	// ─── /buyvehicle — купить транспорт ──────────────────────────────────────
	// Lua: DarkRP.defineChatCommand("buyvehicle", BuyVehicle)
	[ChatCommand( "/buyvehicle", Cooldown = 0.5f )]
	public static void CmdBuyVehicle( Connection ply, string[] args )
	{
		if ( args.Length == 0 )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var name = string.Join( " ", args );
		var vehicle = DarkRP.GetVehicles()
			.FirstOrDefault( v => v.Name.Equals( name, System.StringComparison.OrdinalIgnoreCase ) );

		if ( vehicle is null )
		{
			ply.Notify( LanguageSystem.Get( "unavailable", LanguageSystem.Get( "vehicle" ) ), NotifyType.Error );
			return;
		}

		var jobId = ply.GetDarkRPComponent()?.JobId ?? "";
		if ( vehicle.AllowedJobs.Count > 0 && !vehicle.AllowedJobs.Contains( jobId ) )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", "/buyvehicle" ), NotifyType.Error );
			return;
		}

		if ( ply.IsArrested() )
		{
			ply.Notify( LanguageSystem.Get( "unable", "/buyvehicle" ), NotifyType.Error );
			return;
		}

		var hookResult = Hook.Run( "CanBuyVehicle", ply, vehicle );
		if ( hookResult is false ) return;

		if ( !ply.CanAfford( vehicle.Price ) )
		{
			ply.Notify( LanguageSystem.Get( "cant_afford" ), NotifyType.Error );
			return;
		}

		ply.AddMoney( -vehicle.Price );
		ply.Notify( LanguageSystem.Get( "vehicle_bought", DarkRP.FormatMoney( vehicle.Price ), "" ), NotifyType.Info );
		Hook.Run( "PlayerBoughtVehicle", ply, vehicle );

		DarkRP.Log( $"{ply.DisplayName} купил транспорт {name} за {DarkRP.FormatMoney( vehicle.Price )}" );
	}

	// ─── /buyammo — купить патроны ───────────────────────────────────────────
	// Lua: DarkRP.defineChatCommand("buyammo", BuyAmmo, 1)
	[ChatCommand( "/buyammo", Cooldown = 1f )]
	public static void CmdBuyAmmo( Connection ply, string[] args )
	{
		if ( args.Length == 0 )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var name = string.Join( " ", args );
		var ammo = DarkRP.GetAmmoTypes()
			.FirstOrDefault( a => a.Name.Equals( name, System.StringComparison.OrdinalIgnoreCase )
			                   || a.Id.Equals( name, System.StringComparison.OrdinalIgnoreCase ) );

		if ( ammo is null )
		{
			ply.Notify( LanguageSystem.Get( "unavailable", LanguageSystem.Get( "ammo" ) ), NotifyType.Error );
			return;
		}

		if ( ply.IsArrested() )
		{
			ply.Notify( LanguageSystem.Get( "unable", "/buyammo" ), NotifyType.Error );
			return;
		}

		var hookResult = Hook.Run( "CanBuyAmmo", ply, ammo );
		if ( hookResult is false ) return;

		if ( !ply.CanAfford( ammo.Price ) )
		{
			ply.Notify( LanguageSystem.Get( "cant_afford" ), NotifyType.Error );
			return;
		}

		ply.AddMoney( -ammo.Price );
		ply.Notify( LanguageSystem.Get( "you_bought", ammo.Name, DarkRP.FormatMoney( ammo.Price ) ), NotifyType.Info );

		// Phase-4: спавн патронов перед игроком (жёлтый кубик)
		var pos = EntitySpawner.GetSpawnPositionInFrontOf( ply );
		var go = EntitySpawner.SpawnPrimitive( pos, "spawned_ammo", ply,
			new Color( 0.9f, 0.85f, 0.2f ), new Vector3( 0.2f, 0.2f, 0.15f ) );
		var pickup = go.Components.Create<SpawnedAmmoComponent>();
		pickup.AmmoId = ammo.Id;
		pickup.Amount = ammo.AmountGiven > 0 ? ammo.AmountGiven : 30;

		DarkRP.Log( $"{ply.DisplayName} купил патроны {ammo.Name} за {DarkRP.FormatMoney( ammo.Price )}" );
	}

	// ─── /buy <entity> — купить сущность из BuyableEntity (money_printer и т.д.)
	// Lua: DarkRP.defineChatCommand("buy<command>", ...)
	[ChatCommand( "/buyentity", Cooldown = 0.5f )]
	public static void CmdBuyEntity( Connection ply, string[] args )
	{
		if ( args.Length == 0 )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var name = string.Join( " ", args );
		var entity = DarkRP.GetAllBuyableEntities().Values
			.FirstOrDefault( e => e.Name.Equals( name, System.StringComparison.OrdinalIgnoreCase )
				|| e.Id.Equals( name, System.StringComparison.OrdinalIgnoreCase )
				|| e.Command.Equals( name, System.StringComparison.OrdinalIgnoreCase ) );

		if ( entity is null )
		{
			ply.Notify( LanguageSystem.Get( "unavailable", name ), NotifyType.Error );
			return;
		}

		var jobId = ply.GetDarkRPComponent()?.JobId ?? "";
		if ( entity.AllowedJobs.Count > 0 && !entity.AllowedJobs.Contains( jobId ) )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", $"/buy {name}" ), NotifyType.Error );
			return;
		}

		if ( ply.IsArrested() )
		{
			ply.Notify( LanguageSystem.Get( "unable", $"/buy {name}" ), NotifyType.Error );
			return;
		}

		if ( !ply.CanAfford( entity.Price ) )
		{
			ply.Notify( LanguageSystem.Get( "cant_afford" ), NotifyType.Error );
			return;
		}

		// Лимит на игрока
		if ( entity.MaxPerPlayer > 0 &&
			EntityLimits.CountForPlayer( ply.SteamId, entity.EntityClass ) >= entity.MaxPerPlayer )
		{
			ply.Notify( LanguageSystem.Get( "limit", entity.Name ), NotifyType.Error );
			return;
		}

		ply.AddMoney( -entity.Price );

		// Спавним сущность по EntityClass — для money_printer привязываем компонент-печатающий
		var pos = EntitySpawner.GetSpawnPositionInFrontOf( ply );
		var color = entity.EntityClass switch
		{
			"money_printer" => new Color( 0.3f, 0.3f, 0.4f ),
			"meth_lab" => new Color( 0.7f, 0.4f, 0.7f ),
			"drug_lab" => new Color( 0.4f, 0.7f, 0.4f ),
			_ => new Color( 0.5f, 0.5f, 0.5f ),
		};

		var go = EntitySpawner.SpawnPrimitive( pos, entity.EntityClass, ply,
			color, new Vector3( 0.4f, 0.4f, 0.3f ) );

		// Привязываем специализированные компоненты по EntityClass
		if ( entity.EntityClass == "money_printer" )
			go.Components.Create<MoneyPrinterComponent>();
		else if ( entity.EntityClass == "tip_jar" )
			go.Components.Create<TipJarComponent>().SetOwner( ply );

		ply.Notify( LanguageSystem.Get( "you_bought", entity.Name, DarkRP.FormatMoney( entity.Price ) ), NotifyType.Info );
		Hook.Run( "PlayerBoughtEntity", ply, entity );
		DarkRP.Log( $"{ply.DisplayName} купил {entity.Name} за {DarkRP.FormatMoney( entity.Price )}" );
	}

	// ─── /price — установить цену на свою сущность ─────────────────────────
	// Lua: DarkRP.defineChatCommand("price", SetPrice)
	[ChatCommand( "/price", Cooldown = 0.5f )]
	[ChatCommand( "/setprice", Cooldown = 0.5f )]
	public static void CmdSetPrice( Connection ply, string[] args )
	{
		if ( args.Length == 0 || !int.TryParse( args[0], out var price ) )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		price = System.Math.Clamp( price, 0, 500 );

		// Phase-4: вместо raycast (требует PlayerController) — берём ближайшую свою сущность
		var pawnPos = ply.Pawn?.WorldPosition ?? Vector3.Zero;
		var nearest = Game.ActiveScene.GetAllComponents<SpawnedEntityComponent>()
			.Where( e => e.OwnerSteamId == ply.SteamId )
			.OrderBy( e => (e.GameObject.WorldPosition - pawnPos).LengthSquared )
			.FirstOrDefault();

		if ( nearest is null || (nearest.GameObject.WorldPosition - pawnPos).Length > 200f )
		{
			ply.Notify( LanguageSystem.Get( "must_be_looking_at", "your entity" ), NotifyType.Error );
			return;
		}

		// На своих spawned_weapon можно установить цену для перепродажи
		var weapon = nearest.GameObject.GetComponent<SpawnedWeaponComponent>();
		if ( weapon is not null ) weapon.Price = price;

		ply.Notify( $"Цена установлена: {DarkRP.FormatMoney( price )}", NotifyType.Info );
	}

	// ─── /rpname, /name, /nick — RP имя ─────────────────────────────────────
	// Lua: DarkRP.declareChatCommand{command = "rpname", ...}
	[ChatCommand( "/rpname", Cooldown = 1.5f )]
	[ChatCommand( "/name", Cooldown = 1.5f )]
	[ChatCommand( "/nick", Cooldown = 1.5f )]
	public static void CmdRpName( Connection ply, string[] args )
	{
		if ( args.Length == 0 )
		{
			ply.Notify( "Использование: /rpname <имя>", NotifyType.Error );
			return;
		}

		var newName = string.Join( " ", args ).Trim();
		if ( newName.Length < 2 || newName.Length > 40 )
		{
			ply.Notify( "Имя должно быть от 2 до 40 символов.", NotifyType.Error );
			return;
		}

		// TODO: сохранить в DarkRPPlayerComponent.RpName + [Sync] (phase-2 расширение)
		ply.Notify( $"Ваше RP имя: {newName}", NotifyType.Info );
		DarkRP.Log( $"{ply.DisplayName} сменил RP имя на {newName}" );
	}

	// ─── Вспомогательный поиск shipment по имени ─────────────────────────────
	private static Shipment? FindShipmentByName( string name )
	{
		return DarkRP.GetAllShipments().Values
			.FirstOrDefault( s => s.Name.Equals( name, System.StringComparison.OrdinalIgnoreCase ) );
	}
}
