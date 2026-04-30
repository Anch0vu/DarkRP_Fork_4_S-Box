// Source: gamemode/modules/deathpov/cl_init.lua
// Lua: hook.Add("CalcView", "rp_deathPOV", ...) — POV из ragdoll головы при смерти
// Упрощённый порт: при смерти выставляем флаг IsDead, клиент покажет оверлей.
// Реальная камера-из-ragdoll в S&Box будет в phase-7 (требует кастомного PlayerController).
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Death POV: маркер смерти для клиентского оверлея и камеры.
/// Lua: gamemode/modules/deathpov/cl_init.lua
/// </summary>
public static class DeathPOVSystem
{
	/// <summary>Lua: hook.Add("PlayerDeath", ...)</summary>
	[DarkRPHook( "PlayerDeath" )]
	public static void OnPlayerDeath( Connection victim, Connection? attacker )
	{
		var comp = victim.GetDarkRPComponent();
		if ( comp is null ) return;

		comp.IsDead = true;
		comp.DeathPosition = victim.Pawn?.WorldPosition ?? Vector3.Zero;

		Hook.Run( "playerDeathPOVStart", victim );
	}

	/// <summary>Lua: hook.Add("PlayerSpawn", "DeathPOVReset", ...)</summary>
	[DarkRPHook( "PlayerSpawn" )]
	public static void OnPlayerSpawn( Connection ply )
	{
		var comp = ply.GetDarkRPComponent();
		if ( comp is null ) return;

		if ( comp.IsDead )
		{
			comp.IsDead = false;
			Hook.Run( "playerDeathPOVEnd", ply );
		}
	}
}
