// Source: gamemode/modules/darkrpmessages/cl_darkrpmessage.lua
// Lua: timer.Simple(5, showMOTD) — показ MOTD через 5 секунд после подключения
//      concommand.Add("DarkRP_motd", showMOTD)
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Сообщение дня (MOTD).
/// Lua: gamemode/modules/darkrpmessages/cl_darkrpmessage.lua
/// </summary>
public static class MotdSystem
{
	private const string DefaultMotd =
		"Добро пожаловать на сервер DarkRP!\n" +
		"• F1 — справка по правилам и командам\n" +
		"• F4 — меню работ и покупок\n" +
		"• /help — список всех команд\n" +
		"Соблюдайте правила и приятной игры!";

	private static string _motd = DefaultMotd;

	/// <summary>Установить текст MOTD (на старте сервера).</summary>
	public static void SetMotd( string text ) => _motd = text;

	public static string GetMotd() => _motd;

	// ─── Хуки ─────────────────────────────────────────────────────────────────

	/// <summary>Lua: timer.Simple(5, showMOTD) — после подключения.</summary>
	[DarkRPHook( "PlayerInitialSpawn" )]
	public static void OnInitialSpawn( Connection ply )
	{
		ShowMotdToPlayer( ply );
	}

	private static void ShowMotdToPlayer( Connection ply )
	{
		// Lua: drawMOTD(html) → открыть панель на клиенте
		ShowMotdRpc( ply );
	}

	/// <summary>Открыть MotdPanel.razor на клиенте.</summary>
	[Rpc.Owner]
	public static void ShowMotdRpc( Connection target )
	{
		MotdPanel.Open();
	}

	// ─── Команды ──────────────────────────────────────────────────────────────

	/// <summary>Lua: concommand.Add("DarkRP_motd", showMOTD)</summary>
	[ChatCommand( "/motd", Cooldown = 5f )]
	public static void CmdMotd( Connection ply, string[] args ) => ShowMotdToPlayer( ply );

	/// <summary>Admin: установить MOTD на лету.</summary>
	[ChatCommand( "/setmotd", Cooldown = 1f )]
	public static void CmdSetMotd( Connection ply, string[] args )
	{
		if ( !ply.IsHost )
		{
			ply.Notify( "Только для администраторов.", NotifyType.Error );
			return;
		}
		if ( args.Length == 0 )
		{
			ply.Notify( "Использование: /setmotd <текст>", NotifyType.Error );
			return;
		}

		_motd = string.Join( " ", args );
		ply.Notify( "MOTD обновлён.", NotifyType.Info );
		DarkRP.Log( $"[MOTD] {ply.DisplayName} обновил MOTD" );
	}
}
