// Source: gamemode/modules/doorsystem/sv_doors.lua
// Source: gamemode/modules/doorsystem/sv_dooradministration.lua
// Source: gamemode/modules/doorsystem/sh_doors.lua
// Lua: DarkRP.defineChatCommand("toggleown", OwnDoor), DarkRP.defineChatCommand("addowner", ...)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Менеджер системы дверей.
/// Lua: gamemode/modules/doorsystem/sv_doors.lua
/// </summary>
public sealed class DoorManager : GameObjectSystem
{
	private static string ApiUrl =>
		Environment.GetEnvironmentVariable( "DARKRP_API_URL" ) ?? "http://localhost:3000";

	private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds( 10 ) };
	private static string _currentMap = "unknown";

	public DoorManager( Scene scene ) : base( scene ) { }

	protected override void OnStart()
	{
		_currentMap = Game.ActiveScene.Title ?? "unknown";
		if ( Networking.IsHost )
			_ = LoadAllDoorsAsync();
	}

	// ─── Загрузка/Сохранение ──────────────────────────────────────────────────

	/// <summary>
	/// Загрузить все двери карты из БД.
	/// Lua: hook.Add("InitPostEntity", "Load door privileges", ...)
	/// </summary>
	private static async Task LoadAllDoorsAsync()
	{
		try
		{
			var resp = await _http.GetAsync( $"{ApiUrl}/doors/{Uri.EscapeDataString( _currentMap )}" );
			if ( !resp.IsSuccessStatusCode ) return;

			var json = await resp.Content.ReadAsStringAsync();
			var records = JsonSerializer.Deserialize<List<DoorRecord>>( json, JsonOpts );
			if ( records is null ) return;

			foreach ( var record in records )
				ApplyDoorRecord( record );

			Log.Info( $"[DarkRP Doors] Загружено {records.Count} дверей для карты '{_currentMap}'." );
			Hook.Run( "DoorsLoaded" );
		}
		catch ( Exception ex )
		{
			Log.Error( $"[DarkRP Doors] LoadDoors: {ex.Message}" );
		}
	}

	/// <summary>
	/// Сохранить дверь в БД.
	/// Lua: onKeyValue / door ownership change → SQLite write
	/// </summary>
	public static async Task SaveDoorAsync( DoorComponent door )
	{
		try
		{
			var index = door.GameObject.GetComponent<DoorIndexComponent>()?.Index ?? -1;
			if ( index < 0 ) return;

			var data = new DoorRecord
			{
				DoorIndex = index,
				SteamId = door.OwnerSteamId > 0 ? door.OwnerSteamId.ToString() : null,
				Title = door.Title,
				NonOwnable = door.NonOwnable,
			};

			var json = JsonSerializer.Serialize( data, JsonOpts );
			var content = new StringContent( json, Encoding.UTF8, "application/json" );
			await _http.PutAsync(
				$"{ApiUrl}/doors/{Uri.EscapeDataString( _currentMap )}/{index}", content );
		}
		catch ( Exception ex )
		{
			Log.Error( $"[DarkRP Doors] SaveDoor: {ex.Message}" );
		}
	}

	private static void ApplyDoorRecord( DoorRecord record )
	{
		var door = Game.ActiveScene
			.GetAllComponents<DoorIndexComponent>()
			.FirstOrDefault( d => d.Index == record.DoorIndex )
			?.GameObject.GetComponent<DoorComponent>();

		if ( door is null ) return;
		if ( record.SteamId is not null && ulong.TryParse( record.SteamId, out var sid ) )
		{
			door.OwnerSteamId = sid;
			door.OwnerName = record.OwnerName ?? "";
		}
		door.Title = record.Title ?? "";
		door.NonOwnable = record.NonOwnable;
	}

	// ─── Чат-команды ─────────────────────────────────────────────────────────

	/// <summary>
	/// Lua: DarkRP.defineChatCommand("toggleown", OwnDoor)
	/// Купить/продать дверь перед игроком.
	/// </summary>
	[ChatCommand( "/toggleown", Cooldown = 0.5f )]
	public static void CmdToggleOwn( Connection ply, string[] args )
	{
		var door = GetLookedAtDoor( ply );
		if ( door is null )
		{
			ply.Notify( LanguageSystem.Get( "must_be_looking_at", LanguageSystem.Get( "door_or_vehicle" ) ), NotifyType.Error );
			return;
		}

		if ( door.NonOwnable )
		{
			ply.Notify( LanguageSystem.Get( "door_unownable" ), NotifyType.Error );
			return;
		}

		if ( ply.IsArrested() )
		{
			ply.Notify( LanguageSystem.Get( "door_unown_arrested" ), NotifyType.Error );
			return;
		}

		if ( door.IsOwned )
		{
			if ( !door.IsMasterOwner( ply ) )
			{
				ply.Notify( LanguageSystem.Get( "door_already_owned" ), NotifyType.Error );
				return;
			}
			// Продать
			var refund = GetDoorPrice( door ) / 2; // Config.DoorReduction
			ply.AddMoney( refund );
			door.UnOwn();
			ply.Notify( LanguageSystem.Get( "door_sold", DarkRP.FormatMoney( refund ) ), NotifyType.Info );
		}
		else
		{
			// Купить
			var price = GetDoorPrice( door );

			var hookResult = Hook.Run( "PlayerBuyDoor", ply, door );
			if ( hookResult is false ) return;

			if ( !ply.CanAfford( price ) )
			{
				ply.Notify( LanguageSystem.Get( "door_cannot_afford" ), NotifyType.Error );
				return;
			}

			ply.AddMoney( -price );
			door.Own( ply );
			ply.Notify( LanguageSystem.Get( "door_bought", DarkRP.FormatMoney( price ), "" ), NotifyType.Info );
		}
	}

	/// <summary>Lua: DarkRP.defineChatCommand("unownalldoors", UnOwnAll)</summary>
	[ChatCommand( "/unownalldoors", Cooldown = 1f )]
	[ChatCommand( "/sellalldoors", Cooldown = 1f )]
	public static void CmdUnOwnAll( Connection ply, string[] args )
	{
		var doors = Game.ActiveScene.GetAllComponents<DoorComponent>()
			.Where( d => d.IsMasterOwner( ply ) )
			.ToList();

		if ( doors.Count == 0 )
		{
			ply.Notify( LanguageSystem.Get( "no_doors_owned" ), NotifyType.Error );
			return;
		}

		var total = doors.Count * GetDoorPrice( null ) / 2;
		foreach ( var d in doors ) d.UnOwn();
		ply.AddMoney( total );
		ply.Notify( LanguageSystem.Get( "sold_x_doors", doors.Count, DarkRP.FormatMoney( total ) ), NotifyType.Warning );
	}

	/// <summary>Lua: DarkRP.defineChatCommand("addowner", AddDoorOwner)</summary>
	[ChatCommand( "/addowner", Cooldown = 0.5f )]
	[ChatCommand( "/ao", Cooldown = 0.5f )]
	public static void CmdAddOwner( Connection ply, string[] args )
	{
		var door = GetLookedAtDoor( ply );
		if ( door is null )
		{
			ply.Notify( LanguageSystem.Get( "must_be_looking_at", LanguageSystem.Get( "door_or_vehicle" ) ), NotifyType.Error );
			return;
		}
		if ( !door.IsMasterOwner( ply ) )
		{
			ply.Notify( LanguageSystem.Get( "do_not_own_ent" ), NotifyType.Error );
			return;
		}

		var targetName = string.Join( " ", args );
		var target = Connection.All.FirstOrDefault( c =>
			c.DisplayName.Contains( targetName, StringComparison.OrdinalIgnoreCase ) );

		if ( target is null )
		{
			ply.Notify( LanguageSystem.Get( "could_not_find", targetName ), NotifyType.Error );
			return;
		}
		if ( door.ExtraOwners.Contains( target.SteamId ) )
		{
			ply.Notify( LanguageSystem.Get( "rp_addowner_already_owns_door", target.DisplayName ), NotifyType.Error );
			return;
		}

		door.AddOwner( target );
		ply.Notify( $"Добавлен совладелец: {target.DisplayName}", NotifyType.Info );
	}

	/// <summary>Lua: DarkRP.defineChatCommand("removeowner", RemoveDoorOwner)</summary>
	[ChatCommand( "/removeowner", Cooldown = 0.5f )]
	[ChatCommand( "/ro", Cooldown = 0.5f )]
	public static void CmdRemoveOwner( Connection ply, string[] args )
	{
		var door = GetLookedAtDoor( ply );
		if ( door is null )
		{
			ply.Notify( LanguageSystem.Get( "must_be_looking_at", LanguageSystem.Get( "door_or_vehicle" ) ), NotifyType.Error );
			return;
		}
		if ( !door.IsMasterOwner( ply ) )
		{
			ply.Notify( LanguageSystem.Get( "do_not_own_ent" ), NotifyType.Error );
			return;
		}

		var targetName = string.Join( " ", args );
		var target = Connection.All.FirstOrDefault( c =>
			c.DisplayName.Contains( targetName, StringComparison.OrdinalIgnoreCase ) );

		if ( target is null )
		{
			ply.Notify( LanguageSystem.Get( "could_not_find", targetName ), NotifyType.Error );
			return;
		}

		door.RemoveOwner( target );
		ply.Notify( $"Совладелец удалён: {target.DisplayName}", NotifyType.Info );
	}

	/// <summary>Lua: DarkRP.defineChatCommand("title", SetDoorTitle)</summary>
	[ChatCommand( "/title", Cooldown = 0.5f )]
	public static void CmdTitle( Connection ply, string[] args )
	{
		var door = GetLookedAtDoor( ply );
		if ( door is null )
		{
			ply.Notify( LanguageSystem.Get( "must_be_looking_at", LanguageSystem.Get( "door_or_vehicle" ) ), NotifyType.Error );
			return;
		}
		if ( !door.IsMasterOwner( ply ) )
		{
			ply.Notify( LanguageSystem.Get( "do_not_own_ent" ), NotifyType.Error );
			return;
		}

		var title = string.Join( " ", args );
		door.SetTitle( title );
		ply.Notify( $"Название двери: {title}", NotifyType.Info );
	}

	// ─── Вспомогательные методы ───────────────────────────────────────────────

	/// <summary>
	/// Raycast от взгляда игрока — найти дверь перед ним.
	/// Lua: util.TraceLine + tr.Entity
	/// TODO: подключить к реальному PlayerController.EyeRay (phase-2)
	/// </summary>
	private static DoorComponent? GetLookedAtDoor( Connection ply ) =>
		null; // TODO: реализовать через PlayerController.EyeRay (phase-2)

	private static int GetDoorPrice( DoorComponent? _ ) =>
		500; // TODO: Config.DoorPrice (phase-2)

	private static readonly JsonSerializerOptions JsonOpts = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	private sealed class DoorRecord
	{
		public int DoorIndex { get; set; }
		public string? SteamId { get; set; }
		public string? OwnerName { get; set; }
		public string? Title { get; set; }
		public bool NonOwnable { get; set; }
	}
}

/// <summary>
/// Индекс двери в сцене — уникальный ID для сохранения в БД.
/// Расставляется на карте маппером или автогенерируется.
/// </summary>
public sealed class DoorIndexComponent : Component
{
	[Property] public int Index { get; set; } = -1;
}
