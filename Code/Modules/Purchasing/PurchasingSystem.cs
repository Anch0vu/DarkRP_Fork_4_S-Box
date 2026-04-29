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

		// TODO: spawn spawned_weapon entity рядом с игроком (phase-4, нужна модель)
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

		// TODO: spawn spawned_shipment entity (phase-4)
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

		// TODO: spawn spawned_ammo entity (phase-4)
		DarkRP.Log( $"{ply.DisplayName} купил патроны {ammo.Name} за {DarkRP.FormatMoney( ammo.Price )}" );
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

		price = System.Math.Clamp( price, 0, 500 ); // TODO: Config.pricemin/pricecap (phase-2)
		// TODO: raycast к entity и установить цену (phase-4, нужен PlayerController)
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
