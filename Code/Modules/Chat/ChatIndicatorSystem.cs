// Source: gamemode/modules/chatindicator/cl_init.lua
// Lua: hook.Add("StartChat", "DarkRP_ChatIndicator", ...) — показать индикатор печати
// Клиент: Chat.razor вызывает SetTypingRpc при открытии/закрытии чата.
// Сервер: устанавливает IsTyping на DarkRPPlayerComponent → [Sync] → все клиенты.
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Индикатор печати: синхронизирует состояние "печатает" всем клиентам.
/// Lua: gamemode/modules/chatindicator/cl_init.lua
/// </summary>
public static class ChatIndicatorSystem
{
	/// <summary>
	/// Вызывается клиентом (Chat.razor) при открытии/закрытии поля ввода.
	/// Lua: hook.Add("StartChat", ...) / hook.Add("FinishChat", ...)
	/// </summary>
	[Rpc.Host]
	public static void SetTypingRpc( bool isTyping )
	{
		var sender = Rpc.Caller;
		if ( sender is null ) return;

		var comp = sender.GetDarkRPComponent();
		if ( comp is null ) return;

		comp.IsTyping = isTyping;
	}
}
