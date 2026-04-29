// Source: gamemode/modules/voting/sv_votes.lua
// Source: gamemode/modules/jobs/sv_jobs.lua (demote vote)
// Lua: DarkRP.createVote, DarkRP.defineChatCommand("demote")
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Система голосований — /demote, /forcecancelvote.
/// Lua: gamemode/modules/voting/sv_votes.lua
/// </summary>
public static class VotingSystem
{
	private sealed class Vote
	{
		public string Id { get; init; } = "";
		public string Question { get; init; } = "";
		public Connection? Target { get; init; }
		public Connection? Source { get; init; }
		public string? Reason { get; init; }
		public int Yea { get; set; }
		public int Nay { get; set; }
		public HashSet<ulong> Voters { get; } = new();
		public HashSet<ulong> Excluded { get; } = new();
		public Action<int>? Callback { get; init; } // 1=yea, -1=nay, 0=tie
	}

	private static readonly Dictionary<string, Vote> _votes = new();
	private const float VoteDuration = 20f;

	// ─── /demote <name> <reason> ──────────────────────────────────────────────
	[ChatCommand( "/demote", Cooldown = 1.5f )]
	public static void CmdDemote( Connection ply, string[] args )
	{
		if ( args.Length < 2 )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		if ( Connection.All.Count() <= 1 )
		{
			ply.Notify( LanguageSystem.Get( "vote_alone" ), NotifyType.Error );
			return;
		}

		var target = FindPlayer( args[0] );
		if ( target is null )
		{
			ply.Notify( LanguageSystem.Get( "could_not_find", args[0] ), NotifyType.Error );
			return;
		}
		if ( target == ply ) return;

		var reason = string.Join( " ", args.Skip( 1 ) );
		if ( reason.Length > 64 )
		{
			ply.Notify( LanguageSystem.Get( "unable", "/demote" ), NotifyType.Error );
			return;
		}

		StartVote( new Vote
		{
			Id = $"demote_{target.SteamId}",
			Question = $"{target.DisplayName}:\n{LanguageSystem.Get( "demote_vote_text", reason )}",
			Target = target,
			Source = ply,
			Reason = reason,
			Callback = win =>
			{
				if ( !Connection.All.Contains( target ) ) return;
				if ( win == 1 )
				{
					// Демоут на работу citizen
					var comp = target.GetDarkRPComponent();
					if ( comp is not null )
					{
						Hook.Run( "PlayerChangedJob", target, comp.JobId, "citizen" );
						comp.JobId = "citizen";
					}
					foreach ( var c in Connection.All )
						c.Notify( LanguageSystem.Get( "job_has_become", target.DisplayName, "Citizen" ), NotifyType.Warning );
				}
				else
				{
					foreach ( var c in Connection.All )
						c.Notify( $"Голосование за демоут {target.DisplayName} провалено.", NotifyType.Info );
				}
			},
		}, excluded: new() { ply.SteamId, target.SteamId } );

		foreach ( var c in Connection.All )
			c.Notify( LanguageSystem.Get( "demote_vote_started", ply.DisplayName, target.DisplayName ), NotifyType.Warning );
	}

	// ─── /forcecancelvote — принудительно отменить (admin) ───────────────────
	[ChatCommand( "/forcecancelvote", Cooldown = 0.5f )]
	public static void CmdForceCancelVote( Connection ply, string[] args )
	{
		if ( !ply.IsHost )
		{
			ply.Notify( LanguageSystem.Get( "no_privilege" ), NotifyType.Error );
			return;
		}
		var first = _votes.Keys.FirstOrDefault();
		if ( first is null )
		{
			ply.Notify( "Нет активных голосований.", NotifyType.Error );
			return;
		}
		_votes.Remove( first );
		ply.Notify( "Голосование отменено.", NotifyType.Info );
	}

	// ─── /vote <id> yea/nay — проголосовать ──────────────────────────────────
	[ChatCommand( "/vote" )]
	public static void CmdVote( Connection ply, string[] args )
	{
		if ( args.Length < 2 )
		{
			ply.Notify( LanguageSystem.Get( "invalid_x", "arguments", "" ), NotifyType.Error );
			return;
		}

		var voteId = args[0];
		var choice = args[1].ToLowerInvariant();

		if ( !_votes.TryGetValue( voteId, out var vote ) )
		{
			ply.Notify( "Голосование не найдено.", NotifyType.Error );
			return;
		}

		if ( vote.Excluded.Contains( ply.SteamId ) || vote.Voters.Contains( ply.SteamId ) )
		{
			ply.Notify( LanguageSystem.Get( "you_cannot_vote" ), NotifyType.Error );
			return;
		}

		vote.Voters.Add( ply.SteamId );
		if ( choice == "yea" || choice == "yes" || choice == "y" )
			vote.Yea++;
		else
			vote.Nay++;

		var aliveCount = Connection.All.Count( c => !vote.Excluded.Contains( c.SteamId ) );
		if ( vote.Voters.Count >= aliveCount )
			FinishVote( voteId );
	}

	// ─── Внутренние ───────────────────────────────────────────────────────────

	private static void StartVote( Vote vote, HashSet<ulong>? excluded = null )
	{
		if ( _votes.ContainsKey( vote.Id ) ) return;

		if ( excluded is not null )
			foreach ( var id in excluded )
				vote.Excluded.Add( id );

		_votes[vote.Id] = vote;

		// Таймаут
		_ = EndVoteAfterDelay( vote.Id, VoteDuration );

		foreach ( var conn in Connection.All )
		{
			if ( vote.Excluded.Contains( conn.SteamId ) ) continue;
			conn.Notify( $"[ГОЛОСОВАНИЕ] {vote.Question} — /vote {vote.Id} yea/nay", NotifyType.Warning );
		}
	}

	private static async Task EndVoteAfterDelay( string voteId, float delay )
	{
		await Task.Delay( (int)(delay * 1000) );
		if ( _votes.ContainsKey( voteId ) )
			FinishVote( voteId );
	}

	private static void FinishVote( string voteId )
	{
		if ( !_votes.TryGetValue( voteId, out var vote ) ) return;
		_votes.Remove( voteId );

		var win = vote.Yea > vote.Nay ? 1 : vote.Nay > vote.Yea ? -1 : 0;
		vote.Callback?.Invoke( win );
	}

	private static Connection? FindPlayer( string name ) =>
		Connection.All.FirstOrDefault( c =>
			c.DisplayName.Contains( name, StringComparison.OrdinalIgnoreCase ) );
}
