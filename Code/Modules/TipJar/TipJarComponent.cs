// Source: gamemode/modules/tipjar/sv_communication.lua
// Source: gamemode/modules/tipjar/cl_frame.lua
// Lua: net.Receive("DarkRP_TipJarDonate") — игрок жертвует деньги в баночку владельца
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

// ─── Компонент баночки ───────────────────────────────────────────────────────

/// <summary>
/// TipJar entity — IPressable мировой объект для пожертвований.
/// Lua: entities/entities/tipjar/ (prop_physics с custom Use)
/// Размещается через /buyentity tip_jar (BuyableEntity с EntityClass="tip_jar").
/// </summary>
public sealed class TipJarComponent : Component, Component.IPressable
{
	[Sync] public ulong OwnerSteamId { get; set; } = 0;
	[Sync] public string OwnerName { get; set; } = "";

	/// <summary>Lua: tipjar.Total — сумма всех пожертвований за сессию</summary>
	[Sync] public int TotalDonated { get; set; } = 0;

	public void SetOwner( Connection owner )
	{
		OwnerSteamId = owner.SteamId;
		OwnerName = owner.DisplayName;
	}

	/// <summary>
	/// Нажатие E — показать информацию о баночке.
	/// Lua: ENT:Use(activator) в cl_frame.lua открывает DermaFrame
	/// </summary>
	public bool Press( IPressable.Event e )
	{
		if ( !Networking.IsHost ) return false;
		var ply = e.Source?.Network?.Owner;
		if ( ply is null ) return false;

		ply.Notify(
			$"Баночка {OwnerName} | Собрано: {DarkRP.FormatMoney( TotalDonated )}. " +
			$"Используйте /donate <сумма> для пожертвования.",
			NotifyType.Info, 6f );

		return true;
	}
}

// ─── Система баночек ─────────────────────────────────────────────────────────

/// <summary>
/// Обработка команды /donate и обновление данных баночки.
/// Lua: net.Receive("DarkRP_TipJarDonate") в sv_communication.lua
/// </summary>
public static class TipJarSystem
{
	private const float MaxDonateDistance = 100f;

	/// <summary>
	/// Пожертвовать в ближайшую баночку.
	/// Lua: net.Receive("DarkRP_TipJarDonate", function(_, ply) ... end)
	/// </summary>
	[ChatCommand( "/donate", Cooldown = 0.2f )]
	public static void CmdDonate( Connection ply, string[] args )
	{
		if ( args.Length == 0 || !int.TryParse( args[0], out var amount ) || amount < 1 )
		{
			ply.Notify( "Использование: /donate <сумма>", NotifyType.Error );
			return;
		}

		var pawn = ply.Pawn;
		if ( pawn is null ) return;

		// Найти ближайшую чужую баночку в радиусе MaxDonateDistance
		var jars = Game.ActiveScene.GetAllComponents<TipJarComponent>();
		TipJarComponent? nearest = null;
		float nearestDist = MaxDonateDistance * MaxDonateDistance;

		foreach ( var jar in jars )
		{
			if ( jar.OwnerSteamId == ply.SteamId ) continue; // нельзя донатить самому себе
			var dist = jar.GameObject.WorldPosition.DistanceSquared( pawn.WorldPosition );
			if ( dist < nearestDist )
			{
				nearestDist = dist;
				nearest = jar;
			}
		}

		if ( nearest is null )
		{
			ply.Notify( "Поблизости нет баночки для пожертвований.", NotifyType.Error );
			return;
		}

		if ( !ply.CanAfford( amount ) )
		{
			ply.Notify( LanguageSystem.Get( "cant_afford", DarkRP.FormatMoney( amount ) ), NotifyType.Error );
			return;
		}

		// Lua: DarkRP.payPlayer(ply, owner, amount)
		var owner = Connection.All.FirstOrDefault( c => c.SteamId == nearest.OwnerSteamId );
		if ( owner is null )
		{
			ply.Notify( "Владелец баночки не в сети.", NotifyType.Error );
			return;
		}

		if ( !DarkRP.PayPlayer( ply, owner, amount ) )
		{
			ply.Notify( LanguageSystem.Get( "cant_afford", DarkRP.FormatMoney( amount ) ), NotifyType.Error );
			return;
		}

		nearest.TotalDonated += amount;

		// Lua: DarkRP.notify(ply, 3, 4, DarkRP.getPhrase("you_donated", strAmount, owner:Nick()))
		ply.Notify( $"Вы пожертвовали {DarkRP.FormatMoney( amount )} в баночку {nearest.OwnerName}", NotifyType.Money );
		owner.Notify( $"{ply.DisplayName} пожертвовал {DarkRP.FormatMoney( amount )} в вашу баночку!", NotifyType.Money );

		Hook.Run( "playerDonatedToTipJar", ply, owner, amount, nearest );
		DarkRP.Log( $"{ply.DisplayName} пожертвовал {DarkRP.FormatMoney( amount )} в баночку {nearest.OwnerName}" );
	}
}
