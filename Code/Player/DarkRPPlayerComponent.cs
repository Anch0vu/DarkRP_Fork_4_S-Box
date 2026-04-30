// Source: gamemode/modules/base/sh_entityvars.lua (DarkRP.registerDarkRPVar)
// Source: gamemode/modules/base/sv_entityvars.lua (net sync)
// Source: gamemode/modules/money/sh_money.lua
// Source: gamemode/modules/police/sh_init.lua
// Содержит все [Sync] переменные игрока, аналог DarkRPVars в Lua.
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Компонент, хранящий все DarkRP-данные игрока с автоматической сетевой синхронизацией.
/// Аналог Lua DarkRPVars (DarkRP.registerDarkRPVar / setDarkRPVar / getDarkRPVar).
///
/// Прикрепляется к GameObject игрока при подключении.
/// </summary>
public sealed class DarkRPPlayerComponent : Component
{
	// ────────────────────── Экономика ──────────────────────
	// Lua: DarkRP.registerDarkRPVar("money", ...)
	/// <summary>Текущий баланс кошелька. Lua: ply:getMoney()</summary>
	[Sync] public int Money { get; set; } = 0;

	// ────────────────────── Работа ─────────────────────────
	// Lua: DarkRP.registerDarkRPVar("job", ...)  /  DarkRP.registerDarkRPVar("jobid", ...)
	/// <summary>ID текущей работы. Lua: ply:Team() → RPExtraTeams index</summary>
	[Sync] public string JobId { get; set; } = "citizen";

	// ────────────────────── Арест / Розыск ─────────────────
	// Lua: DarkRP.registerDarkRPVar("Arrested", ...)
	/// <summary>Lua: ply:isArrested()</summary>
	[Sync] public bool IsArrested { get; set; } = false;

	// Lua: DarkRP.registerDarkRPVar("Wanted", ...)
	/// <summary>Lua: ply:isWanted()</summary>
	[Sync] public bool IsWanted { get; set; } = false;

	/// <summary>Причина розыска. Lua: ply:getDarkRPVar("WantedReason")</summary>
	[Sync] public string WantedReason { get; set; } = "";

	/// <summary>Оставшееся время ареста в секундах. Lua: ply:getDarkRPVar("ArrestedBy")</summary>
	[Sync] public float ArrestTimeRemaining { get; set; } = 0f;

	// ────────────────────── Лицензия ───────────────────────
	// Lua: DarkRP.registerDarkRPVar("HasGunlicense", ...)
	/// <summary>Lua: ply:getDarkRPVar("HasGunlicense")</summary>
	[Sync] public bool HasGunLicense { get; set; } = false;

	// ────────────────────── AFK ────────────────────────────
	// Lua: DarkRP.registerDarkRPVar("isAFK", ...)
	/// <summary>Lua: playerSetAFK hook</summary>
	[Sync] public bool IsAFK { get; set; } = false;

	// ────────────────────── Сон ────────────────────────────
	// Lua: player.Sleeping / DarkRP.toggleSleep
	/// <summary>Игрок спит — замёрзшая модель, скрытый ввод. Lua: player.Sleeping</summary>
	[Sync] public bool IsSleeping { get; set; } = false;

	// ────────────────────── Индикатор печати ───────────────
	// Lua: hook.Add("StartChat", "DarkRP_ChatIndicator", ...)
	/// <summary>Игрок печатает в чат. Lua: cl_init.lua chatindicator</summary>
	[Sync] public bool IsTyping { get; set; } = false;

	// ────────────────────── Голод (hungermod) ──────────────
	// Lua: DarkRP.registerDarkRPVar("Energy", ...)
	/// <summary>Уровень сытости 0-100. Lua: ply:getDarkRPVar("Energy")</summary>
	[Sync] public float Hunger { get; set; } = 100f;

	// ────────────────────── Повестка (agenda) ──────────────
	// Lua: DarkRP.registerDarkRPVar("agenda", ...)
	/// <summary>Текущая повестка мэра. Lua: ply:getAgenda()</summary>
	[Sync] public string Agenda { get; set; } = "";

	// ────────────────────── Масштаб ────────────────────────
	// Lua: playerScale hook / DarkRP.registerDarkRPVar("scale", ...)
	/// <summary>Масштаб модели игрока. Lua: ply:getDarkRPVar("scale")</summary>
	[Sync] public float PlayerScale { get; set; } = 1f;

	protected override void OnStart()
	{
		// Инициализация стартовых данных при спавне
		if ( Network.Owner is null ) return;

		var job = Network.Owner.GetJob();
		if ( job is not null )
			HasGunLicense = job.HasGunLicense;
	}

	protected override void OnFixedUpdate()
	{
		// Обратный отсчёт времени ареста (server-side)
		if ( !Networking.IsHost ) return;
		if ( IsArrested && ArrestTimeRemaining > 0f )
		{
			ArrestTimeRemaining -= Time.Delta;
			if ( ArrestTimeRemaining <= 0f )
			{
				IsArrested = false;
				ArrestTimeRemaining = 0f;
				Hook.Run( "PlayerUnArrested", Network.Owner, null );
			}
		}
	}
}
