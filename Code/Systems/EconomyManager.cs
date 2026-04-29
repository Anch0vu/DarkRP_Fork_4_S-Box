// Source: gamemode/modules/money/sv_money.lua
// Source: gamemode/modules/base/sv_gamemode_functions.lua (зарплатный таймер)
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Менеджер экономики — зарплатный таймер, создание денежных стопок.
/// Lua: gamemode/modules/money/sv_money.lua
/// </summary>
public sealed class EconomyManager : GameObjectSystem
{
	// Интервал зарплаты в секундах. Lua: Config.payDayTimer (обычно 600s)
	private const float PayDayInterval = 600f;

	private TimeUntil _nextPayDay = PayDayInterval;

	public EconomyManager( Scene scene ) : base( scene ) { }

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;
		if ( !_nextPayDay ) return;

		_nextPayDay = PayDayInterval;
		PayAllPlayers();
	}

	// ─── Зарплата ─────────────────────────────────────────────────────────────

	/// <summary>
	/// Выплатить зарплату всем игрокам.
	/// Lua: PayDay() в sv_money.lua
	/// </summary>
	private static void PayAllPlayers()
	{
		foreach ( var conn in Connection.All )
		{
			var comp = conn.GetDarkRPComponent();
			if ( comp is null ) continue;

			// AFK не получают зарплату
			if ( comp.IsAFK ) continue;

			var job = conn.GetJob();
			int salary = job?.Salary ?? 0;

			// Хук может изменить зарплату
			// Lua: hook.Run("playerGetSalary", ply, salary)
			var hookResult = Hook.Run( "PlayerGetSalary", conn, salary );
			if ( hookResult is int modified )
				salary = modified;

			if ( salary > 0 )
			{
				conn.AddMoney( salary );
				conn.Notify(
					salary > 0
						? $"Зарплата: {DarkRP.FormatMoney( salary )}"
						: "Вы безработный.",
					NotifyType.Money,
					duration: 6f
				);
			}
		}

		DarkRP.Log( "PayDay выплачена." );
	}

	// ─── Монеты на полу ───────────────────────────────────────────────────────

	/// <summary>
	/// Spawned_money entity на полу.
	/// Lua: DarkRP.createMoneyBag(pos, amount) в sv_money.lua
	/// </summary>
	public static void SpawnMoney( Vector3 position, int amount )
	{
		if ( !Networking.IsHost ) return;
		if ( amount <= 0 ) return;

		var go = new GameObject
		{
			Name = $"spawned_money_{amount}",
			WorldPosition = position,
			WorldScale = new Vector3( 0.3f, 0.5f, 0.05f ),
		};

		var renderer = go.Components.Create<ModelRenderer>();
		renderer.Model = Model.Load( EntitySpawner.BoxModel );
		renderer.Tint = new Color( 0.2f, 0.8f, 0.2f );

		go.Components.Create<ModelCollider>().Model = renderer.Model;
		go.Components.Create<Rigidbody>().MassOverride = 1f;
		go.Components.Create<SpawnedMoneyComponent>().Amount = amount;

		go.NetworkSpawn();
	}

	// ─── Чат-команды экономики ───────────────────────────────────────────────

	/// <summary>Lua: /dropmoney, /moneydrop — бросить деньги на пол</summary>
	[ChatCommand( "/dropmoney", Cooldown = 0.3f )]
	[ChatCommand( "/moneydrop", Cooldown = 0.3f )]
	public static void CmdDropMoney( Connection ply, string[] args )
	{
		if ( args.Length == 0 || !int.TryParse( args[0], out var amount ) || amount < 1 )
		{
			ply.Notify( "Использование: /dropmoney <сумма>", NotifyType.Error );
			return;
		}

		if ( amount > 2_147_483_647 )
		{
			ply.Notify( "Слишком большая сумма.", NotifyType.Error );
			return;
		}

		if ( !ply.CanAfford( amount ) )
		{
			ply.Notify( "Недостаточно денег.", NotifyType.Error );
			return;
		}

		ply.AddMoney( -amount );

		var pos = ply.Pawn?.WorldPosition ?? Vector3.Zero;
		SpawnMoney( pos + Vector3.Up * 32f, amount );
		ply.Notify( $"Вы бросили {DarkRP.FormatMoney( amount )}.", NotifyType.Info );
	}

	/// <summary>Lua: /give — дать деньги другому игроку</summary>
	[ChatCommand( "/give", Cooldown = 0.2f )]
	public static void CmdGiveMoney( Connection ply, string[] args )
	{
		if ( args.Length < 2 || !int.TryParse( args[^1], out var amount ) )
		{
			ply.Notify( "Использование: /give <игрок> <сумма>", NotifyType.Error );
			return;
		}

		if ( amount < 1 )
		{
			ply.Notify( "Сумма должна быть >= 1.", NotifyType.Error );
			return;
		}

		var targetName = string.Join( " ", args.Take( args.Length - 1 ) );
		var target = Connection.All.FirstOrDefault( c =>
			c.DisplayName.Contains( targetName, System.StringComparison.OrdinalIgnoreCase ) );

		if ( target is null )
		{
			ply.Notify( $"Игрок '{targetName}' не найден.", NotifyType.Error );
			return;
		}

		if ( !DarkRP.PayPlayer( ply, target, amount ) )
		{
			ply.Notify( "Недостаточно денег.", NotifyType.Error );
			return;
		}

		ply.Notify( $"Вы отдали {DarkRP.FormatMoney( amount )} игроку {target.DisplayName}.", NotifyType.Info );
		target.Notify( $"{ply.DisplayName} дал вам {DarkRP.FormatMoney( amount )}.", NotifyType.Info );
	}
}

// SpawnedMoneyComponent перенесён в Code/Modules/Entities/PickupComponents.cs (phase-4)
