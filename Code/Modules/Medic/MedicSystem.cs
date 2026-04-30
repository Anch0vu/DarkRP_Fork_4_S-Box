// Source: gamemode/modules/medic/sh_init.lua + sh_interface.lua
// Lua: plyMeta.isMedic — уже есть в PlayerExtensions.cs
// Здесь — дополнительные команды и логика медика.
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Система медика — команда лечения.
/// Lua: gamemode/modules/medic/sh_init.lua
/// </summary>
public static class MedicSystem
{
	private const int HealAmount = 50;
	private const int HealPrice = 25;
	private const float HealRange = 200f;

	/// <summary>
	/// /heal — медик лечит ближайшего игрока (или себя).
	/// Lua: оригинал не имеет команды, но это типичная реализация.
	/// </summary>
	[ChatCommand( "/heal", Cooldown = 1.5f )]
	public static void CmdHeal( Connection ply, string[] args )
	{
		if ( !ply.IsMedic() )
		{
			ply.Notify( LanguageSystem.Get( "incorrect_job", "/heal" ), NotifyType.Error );
			return;
		}

		Connection? target = ply;

		if ( args.Length > 0 )
		{
			var name = string.Join( " ", args );
			target = Connection.All.FirstOrDefault( c =>
				c.DisplayName.Contains( name, System.StringComparison.OrdinalIgnoreCase ) );

			if ( target is null )
			{
				ply.Notify( $"Игрок '{name}' не найден.", NotifyType.Error );
				return;
			}
		}

		// Цель должна быть в радиусе
		if ( target != ply )
		{
			var src = ply.Pawn?.WorldPosition ?? Vector3.Zero;
			var dst = target.Pawn?.WorldPosition ?? Vector3.Zero;
			if ( src.DistanceSquared( dst ) > HealRange * HealRange )
			{
				ply.Notify( "Слишком далеко.", NotifyType.Error );
				return;
			}
		}

		// Цель платит медику
		if ( target != ply )
		{
			if ( !target.CanAfford( HealPrice ) )
			{
				ply.Notify( $"{target.DisplayName} не может оплатить лечение.", NotifyType.Error );
				return;
			}
			DarkRP.PayPlayer( target, ply, HealPrice );
			target.Notify( $"Вы заплатили {DarkRP.FormatMoney( HealPrice )} за лечение.", NotifyType.Money );
		}

		// TODO (phase-7+): реальный HealthComponent.Health += HealAmount
		// Пока — нотификация
		target.Notify( $"Вы вылечены на {HealAmount} HP медиком {ply.DisplayName}.", NotifyType.Info );
		ply.Notify( $"Вы вылечили {target.DisplayName} (+{HealAmount} HP).", NotifyType.Info );

		Hook.Run( "playerHealed", ply, target, HealAmount );
	}
}
