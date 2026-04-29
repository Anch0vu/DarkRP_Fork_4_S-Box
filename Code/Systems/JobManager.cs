// Source: gamemode/modules/jobs/sv_jobs.lua
// Source: gamemode/modules/jobs/sv_interface.lua
// Source: gamemode/modules/jobs/sh_interface.lua
using System.Linq;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Менеджер работ — обрабатывает смену профессий, проверку слотов.
/// Lua: gamemode/modules/jobs/sv_jobs.lua
///
/// GameObjectSystem автоматически создаётся S&Box при загрузке сцены.
/// </summary>
public sealed class JobManager : GameObjectSystem
{
	public JobManager( Scene scene ) : base( scene ) { }

	// ─── Смена работы ────────────────────────────────────────────────────────

	/// <summary>
	/// Попытаться сменить работу игрока.
	/// Lua: darkrp_changeteam команда в sv_jobs.lua
	/// </summary>
	public static bool TryChangeJob( Connection ply, string jobId )
	{
		if ( !Networking.IsHost ) return false;

		var job = DarkRP.GetJob( jobId );
		if ( job is null )
		{
			ply.Notify( DarkRP.Translate( "job_doesnt_exist" ), NotifyType.Error );
			return false;
		}

		// Проверка adminLevel
		if ( job.AdminLevel > 0 )
		{
			// TODO: проверить права через S&Box permission system (phase-2)
			ply.Notify( "Эта работа только для администраторов.", NotifyType.Error );
			return false;
		}

		// Проверка: AFK не может менять работу
		var hookResult = Hook.Run( "PlayerCanChangeJob", ply, job );
		if ( hookResult is false )
			return false;

		// Проверка слотов
		if ( job.MaxPlayers > 0 )
		{
			var count = CountPlayersOnJob( jobId );
			if ( count >= job.MaxPlayers )
			{
				ply.Notify( $"Работа {job.Name} заполнена ({count}/{job.MaxPlayers}).", NotifyType.Error );
				return false;
			}
		}

		// Проверка NeedToChangeFrom
		if ( job.NeedToChangeFrom.Count > 0 )
		{
			var currentJobId = ply.GetDarkRPComponent()?.JobId ?? "citizen";
			if ( !job.NeedToChangeFrom.Contains( currentJobId ) )
			{
				ply.Notify( "Сначала нужно сменить работу.", NotifyType.Error );
				return false;
			}
		}

		ply.SetJob( jobId );
		ply.Notify( $"Вы теперь {job.Name}.", NotifyType.Info );

		DarkRP.Log( $"{ply.DisplayName} сменил работу на {job.Name}" );
		return true;
	}

	/// <summary>
	/// Количество игроков на указанной работе.
	/// Lua: #fn.Filter(fn.Curry(fn.GetValue, 2)(team), player.GetAll())
	/// </summary>
	public static int CountPlayersOnJob( string jobId )
	{
		return Game.ActiveScene
			.GetAllComponents<DarkRPPlayerComponent>()
			.Count( c => c.JobId == jobId );
	}

	/// <summary>Lua: /job команда чата</summary>
	[ChatCommand( "/job" )]
	public static void CmdJob( Connection ply, string[] args )
	{
		if ( args.Length == 0 )
		{
			ply.Notify( "Использование: /job <id>", NotifyType.Error );
			return;
		}
		TryChangeJob( ply, string.Join( " ", args ) );
	}
}
