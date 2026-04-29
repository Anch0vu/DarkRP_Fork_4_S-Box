// Source: gamemode/modules/base/sv_data.lua
// Source: gamemode/libraries/mysqlite/mysqlite.lua (DROP — заменён на HTTP)
// BLOCKER-1: РЕШЕНО — Node.js REST API → MySQL (см. Backend/)
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Абстракция доступа к данным.
/// Lua: gamemode/modules/base/sv_data.lua + libraries/mysqlite/mysqlite.lua
///
/// Реализация: HTTP запросы к Node.js REST API (Backend/server.js),
/// который работает с MySQL.
/// </summary>
public static class DataManager
{
	// URL Backend сервера. Задаётся через переменную окружения DARKRP_API_URL.
	// Пример: http://localhost:3000
	private static string ApiUrl =>
		Environment.GetEnvironmentVariable( "DARKRP_API_URL" ) ?? "http://localhost:3000";

	private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds( 10 ) };

	// ─── Инициализация ────────────────────────────────────────────────────────

	/// <summary>
	/// Вызывается при старте сервера — проверяет соединение с БД.
	/// Lua: hook.Run("DarkRPDBInitialized") в mysqlite.lua
	/// </summary>
	public static async Task OnGameStartAsync()
	{
		try
		{
			var resp = await _http.GetAsync( $"{ApiUrl}/health" );
			resp.EnsureSuccessStatusCode();
			Hook.Run( "DBInitialized" );
			Log.Info( "[DarkRP DB] Соединение с MySQL установлено." );
		}
		catch ( Exception ex )
		{
			Log.Error( $"[DarkRP DB] Не удалось подключиться к API: {ex.Message}" );
		}
	}

	// ─── Загрузка / Сохранение игрока ────────────────────────────────────────

	/// <summary>
	/// Загрузить данные игрока из MySQL при подключении.
	/// Lua: sv_data.lua loadPlayerData(ply)
	/// </summary>
	public static async Task LoadPlayerAsync( Connection conn )
	{
		try
		{
			var url = $"{ApiUrl}/player/{conn.SteamId}";
			var resp = await _http.GetAsync( url );

			if ( resp.StatusCode == System.Net.HttpStatusCode.NotFound )
			{
				// Новый игрок — создать запись
				await CreatePlayerAsync( conn );
				return;
			}

			resp.EnsureSuccessStatusCode();
			var json = await resp.Content.ReadAsStringAsync();
			var data = JsonSerializer.Deserialize<PlayerData>( json, JsonOptions );
			if ( data is null ) return;

			ApplyPlayerData( conn, data );
			Log.Info( $"[DarkRP DB] Загружен {conn.DisplayName} (${data.Money}, job={data.JobId})" );
		}
		catch ( Exception ex )
		{
			Log.Error( $"[DarkRP DB] LoadPlayer({conn.DisplayName}): {ex.Message}" );
		}
	}

	/// <summary>
	/// Сохранить данные игрока при отключении.
	/// Lua: sv_data.lua savePlayerData(ply)
	/// </summary>
	public static async Task SavePlayerAsync( Connection conn )
	{
		var comp = conn.GetDarkRPComponent();
		if ( comp is null ) return;

		try
		{
			var data = new PlayerData
			{
				SteamId = conn.SteamId,
				DisplayName = conn.DisplayName,
				Money = comp.Money,
				JobId = comp.JobId,
				HasGunLicense = comp.HasGunLicense,
				Hunger = comp.Hunger,
			};

			var json = JsonSerializer.Serialize( data, JsonOptions );
			var content = new StringContent( json, Encoding.UTF8, "application/json" );
			var resp = await _http.PutAsync( $"{ApiUrl}/player/{conn.SteamId}", content );
			resp.EnsureSuccessStatusCode();
		}
		catch ( Exception ex )
		{
			Log.Error( $"[DarkRP DB] SavePlayer({conn.DisplayName}): {ex.Message}" );
		}
	}

	// ─── Универсальный key-value ──────────────────────────────────────────────

	/// <summary>
	/// Сохранить произвольное значение для игрока.
	/// Lua: DarkRP.DB.SetPlayerData(steamId, key, value)
	/// </summary>
	public static async Task SetPlayerDataAsync<T>( ulong steamId, string key, T value )
	{
		try
		{
			var json = JsonSerializer.Serialize( new { value }, JsonOptions );
			var content = new StringContent( json, Encoding.UTF8, "application/json" );
			await _http.PutAsync( $"{ApiUrl}/player/{steamId}/kv/{key}", content );
		}
		catch ( Exception ex )
		{
			Log.Error( $"[DarkRP DB] SetPlayerData({steamId}, {key}): {ex.Message}" );
		}
	}

	/// <summary>
	/// Получить произвольное значение для игрока.
	/// Lua: DarkRP.DB.GetPlayerData(steamId, key)
	/// </summary>
	public static async Task<T?> GetPlayerDataAsync<T>( ulong steamId, string key )
	{
		try
		{
			var resp = await _http.GetAsync( $"{ApiUrl}/player/{steamId}/kv/{key}" );
			if ( !resp.IsSuccessStatusCode ) return default;
			var json = await resp.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<T>( json, JsonOptions );
		}
		catch
		{
			return default;
		}
	}

	// ─── Внутренние методы ───────────────────────────────────────────────────

	private static async Task CreatePlayerAsync( Connection conn )
	{
		var startingMoney = 500; // TODO: из Config
		var data = new PlayerData
		{
			SteamId = conn.SteamId,
			DisplayName = conn.DisplayName,
			Money = startingMoney,
			JobId = "citizen",
			HasGunLicense = false,
			Hunger = 100f,
		};

		var json = JsonSerializer.Serialize( data, JsonOptions );
		var content = new StringContent( json, Encoding.UTF8, "application/json" );
		await _http.PostAsync( $"{ApiUrl}/player", content );

		ApplyPlayerData( conn, data );
		Log.Info( $"[DarkRP DB] Создан новый игрок {conn.DisplayName}" );
	}

	private static void ApplyPlayerData( Connection conn, PlayerData data )
	{
		var comp = conn.GetDarkRPComponent();
		if ( comp is null ) return;
		comp.Money = data.Money;
		comp.JobId = data.JobId;
		comp.HasGunLicense = data.HasGunLicense;
		comp.Hunger = data.Hunger;
	}

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
	};

	// ─── DTO ─────────────────────────────────────────────────────────────────

	private sealed class PlayerData
	{
		public ulong SteamId { get; set; }
		public string DisplayName { get; set; } = "";
		public int Money { get; set; }
		public string JobId { get; set; } = "citizen";
		public bool HasGunLicense { get; set; }
		public float Hunger { get; set; } = 100f;
	}
}
