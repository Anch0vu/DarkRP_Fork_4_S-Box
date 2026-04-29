// Source: gamemode/modules/language/sh_english.lua
// Source: gamemode/modules/language/sh_language.lua
// Lua: DarkRP.getPhrase(key, ...) → DarkRP.Translate(key, args)
// Конвертирован из Lua таблицы в JSON + runtime загрузка
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Система локализации DarkRP.
/// Lua: gamemode/modules/language/sh_language.lua + sh_english.lua
///
/// Файлы фраз: Resources/Localization/{lang}.json
/// Lua-эквивалент: DarkRP.getPhrase(key, arg1, arg2, ...)
/// </summary>
public static class LanguageSystem
{
	private static Dictionary<string, string> _phrases = new( StringComparer.OrdinalIgnoreCase );
	private static string _currentLang = "en";

	/// <summary>
	/// Загрузить язык из JSON файла.
	/// Lua: GAMEMODE.Config.language
	/// </summary>
	public static void LoadLanguage( string lang = "en" )
	{
		_currentLang = lang;
		var path = $"Localization/{lang}.json";

		try
		{
			var json = FileSystem.Mounted.ReadAllText( path );
			_phrases = JsonSerializer.Deserialize<Dictionary<string, string>>( json )
				?? new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );
			Log.Info( $"[DarkRP Lang] Загружен язык '{lang}' ({_phrases.Count} фраз)." );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"[DarkRP Lang] Не удалось загрузить '{path}': {ex.Message}. Используется ключ как текст." );
		}
	}

	/// <summary>
	/// Получить переведённую фразу.
	/// Lua: DarkRP.getPhrase(key, arg1, arg2, ...)
	/// </summary>
	public static string Get( string key, params object[] args )
	{
		if ( !_phrases.TryGetValue( key, out var template ) )
			template = key;

		try
		{
			return args.Length > 0 ? string.Format( template, args ) : template;
		}
		catch
		{
			return template;
		}
	}
}
