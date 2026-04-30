// Source: gamemode/modules/playerscale/sv_playerscale.lua
// Lua: DarkRP.registerDarkRPVar("scale", ...) — масштаб модели игрока
//      GAMEMODE:PlayerSpawn → ply:SetModelScale(ply:getDarkRPVar("scale"))
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Система масштаба игрока.
/// Lua: gamemode/modules/playerscale/sv_playerscale.lua
/// [Sync] PlayerScale на DarkRPPlayerComponent → клиент применяет к Pawn.WorldScale.
/// </summary>
public static class PlayerScaleSystem
{
	private const float MinScale = 0.5f;
	private const float MaxScale = 3f;

	/// <summary>
	/// Применить масштаб к пешке.
	/// Lua: ply:SetModelScale(scale) в sv_playerscale.lua
	/// </summary>
	public static void ApplyScale( Connection ply )
	{
		var comp = ply.GetDarkRPComponent();
		if ( comp is null || ply.Pawn is null ) return;

		ply.Pawn.WorldScale = Vector3.One * comp.PlayerScale;
	}

	// ─── Хуки ─────────────────────────────────────────────────────────────────

	/// <summary>Lua: hook.Add("PlayerSpawn", "PlayerScale", function(ply) ply:SetModelScale(...) end)</summary>
	[DarkRPHook( "PlayerSpawn" )]
	public static void OnPlayerSpawn( Connection ply ) => ApplyScale( ply );

	// ─── Команды ──────────────────────────────────────────────────────────────

	/// <summary>Admin: /playerscale [player] [scale] — изменить масштаб игрока</summary>
	[ChatCommand( "/playerscale", Cooldown = 0.5f )]
	[ChatCommand( "/scale", Cooldown = 0.5f )]
	public static void CmdPlayerScale( Connection ply, string[] args )
	{
		if ( !ply.IsHost )
		{
			ply.Notify( "Только для администраторов.", NotifyType.Error );
			return;
		}

		if ( args.Length < 2 )
		{
			ply.Notify( "Использование: /playerscale <игрок> <масштаб>", NotifyType.Error );
			return;
		}

		if ( !float.TryParse( args[^1], System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out var scale ) )
		{
			ply.Notify( "Неверный масштаб.", NotifyType.Error );
			return;
		}

		scale = System.Math.Clamp( scale, MinScale, MaxScale );
		var targetName = string.Join( " ", args[..^1] );

		var target = Connection.All.FirstOrDefault( c =>
			c.DisplayName.Contains( targetName, System.StringComparison.OrdinalIgnoreCase ) );

		if ( target is null )
		{
			ply.Notify( $"Игрок '{targetName}' не найден.", NotifyType.Error );
			return;
		}

		var comp = target.GetDarkRPComponent();
		if ( comp is null ) return;

		comp.PlayerScale = scale;
		ApplyScale( target );

		ply.Notify( $"Масштаб {target.DisplayName} → {scale:F2}x", NotifyType.Info );
		target.Notify( $"Ваш масштаб изменён на {scale:F2}x", NotifyType.Info );

		DarkRP.Log( $"[Scale] {ply.DisplayName} изменил масштаб {target.DisplayName} на {scale:F2}" );
	}

	/// <summary>Сбросить свой масштаб обратно к 1.0</summary>
	[ChatCommand( "/resetscale", Cooldown = 0.5f )]
	public static void CmdResetScale( Connection ply, string[] args )
	{
		var comp = ply.GetDarkRPComponent();
		if ( comp is null ) return;

		comp.PlayerScale = 1f;
		ApplyScale( ply );
		ply.Notify( "Масштаб сброшен.", NotifyType.Info );
	}
}
