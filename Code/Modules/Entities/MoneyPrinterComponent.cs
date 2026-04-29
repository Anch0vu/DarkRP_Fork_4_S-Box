// Source: entities/entities/money_printer/{init,shared}.lua
// Lua: ENT:CreateMoneybag(), GAMEMODE.Config.mprintamount
// Phase-4: упрощённая версия БЕЗ взрывов и пожаров — только печать денег.
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Принтер денег — генерирует SpawnedMoney через регулярные интервалы.
///
/// Lua: entities/entities/money_printer/init.lua
/// Phase-4 simplified: no overheat / no explosions / no fire (just prints).
/// </summary>
public sealed class MoneyPrinterComponent : Component
{
	/// <summary>Сумма за один цикл. Lua: GAMEMODE.Config.mprintamount</summary>
	[Property] public int Amount { get; set; } = 250;

	/// <summary>Минимум секунд между циклами. Lua: ENT.MinTimer = 100</summary>
	[Property] public float MinInterval { get; set; } = 100f;

	/// <summary>Максимум секунд между циклами. Lua: ENT.MaxTimer = 350</summary>
	[Property] public float MaxInterval { get; set; } = 350f;

	[Sync] public float NextPrintTime { get; set; }

	protected override void OnStart()
	{
		if ( !Networking.IsHost ) return;
		ScheduleNext();
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;
		if ( Time.Now < NextPrintTime ) return;

		PrintMoney();
		ScheduleNext();
	}

	private void ScheduleNext()
	{
		var delay = System.Random.Shared.NextSingle() * (MaxInterval - MinInterval) + MinInterval;
		NextPrintTime = Time.Now + delay;
	}

	private void PrintMoney()
	{
		// Lua: hook.Run("moneyPrinterPrintMoney", self, amount)
		var hookResult = Hook.Run( "moneyPrinterPrintMoney", this, Amount );
		if ( hookResult is false ) return;

		// Спавним моник прямо над принтером
		var spawnPos = GameObject.WorldPosition + Vector3.Up * 24f;
		var moneyGo = new GameObject
		{
			Name = $"spawned_money_{Amount}",
			WorldPosition = spawnPos,
		};

		var renderer = moneyGo.Components.Create<ModelRenderer>();
		renderer.Model = Model.Load( EntitySpawner.BoxModel );
		renderer.Tint = new Color( 0.2f, 0.8f, 0.2f );
		moneyGo.WorldScale = new Vector3( 0.3f, 0.5f, 0.05f );

		moneyGo.Components.Create<ModelCollider>().Model = renderer.Model;
		moneyGo.Components.Create<Rigidbody>().MassOverride = 1f;

		var pickup = moneyGo.Components.Create<SpawnedMoneyComponent>();
		pickup.Amount = Amount;

		moneyGo.NetworkSpawn();

		Hook.Run( "moneyPrinterPrinted", this, pickup );

		// Уведомить владельца
		var owner = GameObject.GetComponent<SpawnedEntityComponent>()?.GetOwner();
		owner?.Notify( $"Принтер напечатал {DarkRP.FormatMoney( Amount )}!", NotifyType.Info );
	}
}
