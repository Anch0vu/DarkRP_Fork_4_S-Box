// Source: gamemode/modules/base/sv_gamemode_functions.lua (Armor refresh)
// S&Box не имеет встроенного ArmorComponent — реализуем минимальный аналог.
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Бронежилет игрока. Lua: ply:Armor() / ply:SetArmor()
/// Хранит armor 0..100 с автосинком всем клиентам.
/// </summary>
public sealed class ArmorComponent : Component
{
	[Sync] public int Armor { get; set; } = 0;

	[Sync] public int MaxArmor { get; set; } = 100;

	/// <summary>Lua: ply:SetArmor(amount)</summary>
	public void SetArmor( int amount )
	{
		Armor = System.Math.Clamp( amount, 0, MaxArmor );
	}

	/// <summary>Lua: ply:AddArmor(amount)</summary>
	public void AddArmor( int amount ) => SetArmor( Armor + amount );

	/// <summary>
	/// Поглощение урона: возвращает оставшийся урон после брони.
	/// Lua: classic Source — armor поглощает 80% урона до 0.
	/// </summary>
	public float AbsorbDamage( float damage )
	{
		if ( Armor <= 0 ) return damage;

		var absorbed = System.Math.Min( damage * 0.8f, Armor );
		Armor -= (int)absorbed;
		return damage - absorbed;
	}
}

/// <summary>
/// Команды покупки брони.
/// </summary>
public static class ArmorPurchase
{
	private const int FullArmorPrice = 250;

	/// <summary>/buyarmor — купить полный комплект брони (100).</summary>
	[ChatCommand( "/buyarmor", Cooldown = 1f )]
	public static void CmdBuyArmor( Connection ply, string[] args )
	{
		if ( ply.IsArrested() )
		{
			ply.Notify( LanguageSystem.Get( "arrest_cant_do" ), NotifyType.Error );
			return;
		}

		if ( !ply.CanAfford( FullArmorPrice ) )
		{
			ply.Notify( LanguageSystem.Get( "cant_afford", DarkRP.FormatMoney( FullArmorPrice ) ), NotifyType.Error );
			return;
		}

		var current = ply.GetArmor();
		if ( current >= 100 )
		{
			ply.Notify( "У вас уже максимальная броня.", NotifyType.Info );
			return;
		}

		ply.AddMoney( -FullArmorPrice );
		ply.SetArmor( 100 );
		ply.Notify( $"Куплена броня (100 AP) за {DarkRP.FormatMoney( FullArmorPrice )}.", NotifyType.Info );
		Hook.Run( "playerBoughtArmor", ply );
	}
}
