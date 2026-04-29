// Source: gamemode/modules/base/sh_createitems.lua (DarkRP.createJob)
// Source: gamemode/config/jobrelated.lua (job definitions)
using System.Collections.Generic;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Определение работы (профессии) в DarkRP.
/// Lua: DarkRP.createJob(name, { color, model, description, weapons, command, max, salary, cp, ... })
/// </summary>
[GameResource( "DarkRP Job", "job", "DarkRP job definition", Icon = "work" )]
public sealed class Job : GameResource
{
	/// <summary>Уникальный идентификатор. Lua: command</summary>
	[Property] public string Id { get; set; } = "";

	/// <summary>Отображаемое имя. Lua: name</summary>
	[Property] public string Name { get; set; } = "Citizen";

	/// <summary>Цвет в HUD и чате. Lua: color</summary>
	[Property] public Color Color { get; set; } = Color.White;

	/// <summary>Описание для F4-меню. Lua: description</summary>
	[Property, TextArea] public string Description { get; set; } = "";

	/// <summary>Путь к .vmdl модели игрока. Lua: model</summary>
	[Property] public List<string> Models { get; set; } = new();

	/// <summary>Стартовые оружия. Lua: weapons</summary>
	[Property] public List<string> Weapons { get; set; } = new();

	/// <summary>Зарплата в секунду*PaydayInterval. Lua: salary</summary>
	[Property] public int Salary { get; set; } = 45;

	/// <summary>Максимум игроков на работе. Lua: max</summary>
	[Property] public int MaxPlayers { get; set; } = 0;

	/// <summary>Требует голосования для смены. Lua: vote</summary>
	[Property] public bool VoteRequired { get; set; } = false;

	/// <summary>Только для admins. Lua: admin = 1/2</summary>
	[Property] public int AdminLevel { get; set; } = 0;

	/// <summary>Является ли CP (полиция/мэр/SWAT). Lua: cp = true</summary>
	[Property] public bool IsCP { get; set; } = false;

	/// <summary>Имеет лицензию на оружие по умолчанию. Lua: hasLicense</summary>
	[Property] public bool HasGunLicense { get; set; } = false;

	/// <summary>Можно ли выбрать через F4, или нужна команда. Lua: customCheck</summary>
	[Property] public bool CanBeSelectedInMenu { get; set; } = true;

	/// <summary>Является ли мэром (для системы законов). Lua: mayor = true в jobTable</summary>
	[Property] public bool IsMayor { get; set; } = false;

	/// <summary>Является ли шефом полиции. Lua: chief = true</summary>
	[Property] public bool IsChief { get; set; } = false;

	/// <summary>Является ли наёмным убийцей. Lua: hitman = true</summary>
	[Property] public bool IsHitman { get; set; } = false;

	/// <summary>Является ли медиком. Lua: medic = true</summary>
	[Property] public bool IsMedic { get; set; } = false;

	/// <summary>Является ли поваром. Lua: cook = true</summary>
	[Property] public bool IsCook { get; set; } = false;

	/// <summary>Команды чата для входа в работу. Lua: command</summary>
	[Property] public List<string> Commands { get; set; } = new();

	/// <summary>Работы, с которых нельзя перейти в эту. Lua: NeedToChangeFrom</summary>
	[Property] public List<string> NeedToChangeFrom { get; set; } = new();
}
