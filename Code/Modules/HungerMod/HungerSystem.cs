// Source: gamemode/modules/hungermod/sv_hungermod.lua
// Source: gamemode/modules/hungermod/sh_commands.lua
// Lua: timer.Create("HMThink", 10, 0, HMThink) + DarkRP.defineChatCommand("buyfood")
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Система голода: уменьшение сытости со временем, урон при 0, покупка еды.
/// Lua: gamemode/modules/hungermod/sv_hungermod.lua
/// </summary>
public sealed class HungerSystem : GameObjectSystem
{
	// Lua: timer.Create("HMThink", 10, 0, ...) — интервал тика
	private const float HungerTickInterval = 10f;

	// Lua: энергия уменьшается на ~2 за 10 секунд для среднего темпа
	private const float HungerDecayPerTick = 2f;

	// Урон при нулевой сытости
	private const int StarvingDamage = 5;

	private TimeUntil _nextTick = HungerTickInterval;

	public HungerSystem( Scene scene ) : base( scene ) { }

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;
		if ( !_nextTick ) return;
		_nextTick = HungerTickInterval;

		foreach ( var conn in Connection.All )
		{
			var comp = conn.GetDarkRPComponent();
			if ( comp is null ) continue;

			// AFK — голод не изменяется (Lua: hook.Add("playerSetAFK", "Hungermod", ...))
			if ( comp.IsAFK ) continue;

			comp.Hunger = System.Math.Max( 0f, comp.Hunger - HungerDecayPerTick );

			if ( comp.Hunger <= 0f )
			{
				// Lua: ply:TakeDamage(5, ...) — урон голодом
				// TODO (phase-5+): вызвать HealthComponent.TakeDamage когда будет PlayerController
				conn.Notify( "Вы умираете от голода!", NotifyType.Error, 4f );
				Hook.Run( "playerStarving", conn );
			}
		}
	}

	// ─── Восстановление при спавне ────────────────────────────────────────────

	/// <summary>Lua: hook.Add("PlayerSpawn", "HMPlayerSpawn", function(ply) ply:setSelfDarkRPVar("Energy", 100) end)</summary>
	[DarkRPHook( "PlayerSpawn" )]
	public static void OnPlayerSpawn( Connection ply )
	{
		var comp = ply.GetDarkRPComponent();
		if ( comp is not null )
			comp.Hunger = 100f;
	}

	// ─── Команды ──────────────────────────────────────────────────────────────

	/// <summary>
	/// Купить еду у повара.
	/// Lua: DarkRP.defineChatCommand("buyfood", BuyFood) в sh_commands.lua
	/// </summary>
	[ChatCommand( "/buyfood", Cooldown = 1.5f )]
	public static void CmdBuyFood( Connection ply, string[] args )
	{
		if ( ply.IsArrested() )
		{
			ply.Notify( LanguageSystem.Get( "arrest_cant_do" ), NotifyType.Error );
			return;
		}

		if ( args.Length == 0 )
		{
			ply.Notify( "Использование: /buyfood <название>", NotifyType.Error );
			return;
		}

		var name = string.Join( " ", args );
		var food = DarkRP.GetFoodItems().FirstOrDefault( f =>
			string.Equals( f.Name, name, System.StringComparison.OrdinalIgnoreCase ) );

		if ( food is null )
		{
			var list = string.Join( ", ", DarkRP.GetFoodItems().Select( f => f.Name ) );
			ply.Notify( $"Еда '{name}' не найдена. Доступно: {(string.IsNullOrEmpty( list ) ? "нет" : list)}", NotifyType.Error );
			return;
		}

		if ( !ply.CanAfford( food.Price ) )
		{
			ply.Notify( LanguageSystem.Get( "cant_afford", DarkRP.FormatMoney( food.Price ) ), NotifyType.Error );
			return;
		}

		ply.AddMoney( -food.Price );

		var comp = ply.GetDarkRPComponent();
		if ( comp is not null )
			comp.Hunger = System.Math.Min( 100f, comp.Hunger + food.HungerRestored );

		var hunger = (int)(comp?.Hunger ?? 100f);
		ply.Notify( $"Вы съели {food.Name} (+{food.HungerRestored}). Сытость: {hunger}%", NotifyType.Info );

		Hook.Run( "playerBoughtFood", ply, food );
		DarkRP.Log( $"{ply.DisplayName} купил еду '{food.Name}' за {DarkRP.FormatMoney( food.Price )}" );
	}
}
