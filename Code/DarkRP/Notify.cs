// Source: gamemode/modules/base/sh_interface.lua (DarkRP.notify)
// Source: gamemode/modules/base/cl_drawfunctions.lua (HUD rendering)
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Тип уведомления. Lua: первый числовой аргумент DarkRP.notify(ply, TYPE, dur, msg)
/// </summary>
public enum NotifyType
{
	/// <summary>Lua: 0 — зелёное информационное сообщение</summary>
	Info = 0,
	/// <summary>Lua: 1 — красное сообщение об ошибке</summary>
	Error = 1,
	/// <summary>Lua: 2 — жёлтое предупреждение</summary>
	Warning = 2,
	/// <summary>Lua: 3 — фиолетовое уведомление</summary>
	Purple = 3,
	/// <summary>Lua: 4 — зарплата/деньги (зелёный, другой звук)</summary>
	Money = 4,
}

/// <summary>
/// Данные одного уведомления, отображаемого на экране.
/// </summary>
public sealed class Notification
{
	public string Message { get; init; } = "";
	public NotifyType Type { get; init; } = NotifyType.Info;
	public float Duration { get; init; } = 5f;
	public RealTimeUntil ExpiresAt { get; set; }

	public Color GetColor() => Type switch
	{
		NotifyType.Info    => new Color( 0.2f, 0.8f, 0.2f ),
		NotifyType.Error   => new Color( 0.9f, 0.2f, 0.2f ),
		NotifyType.Warning => new Color( 0.9f, 0.8f, 0.1f ),
		NotifyType.Purple  => new Color( 0.6f, 0.2f, 0.8f ),
		NotifyType.Money   => new Color( 0.1f, 0.9f, 0.4f ),
		_                  => Color.White,
	};
}
