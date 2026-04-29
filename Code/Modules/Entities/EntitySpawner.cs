// Source: gamemode/modules/base/sv_purchasing.lua (spawn helpers)
// Lua: ents.Create(...) + DarkRP.placeWeapon(...)
// S&Box: GameObject + ModelRenderer + ModelCollider + Rigidbody (примитивы из Sandbox.Game)
using System;
using Sandbox;

namespace SboxDarkRP;

/// <summary>
/// Хелпер для спавна DarkRP-сущностей с встроенными примитивами S&Box.
///
/// Использует встроенные models/dev/box.vmdl, models/dev/sphere.vmdl и т.п. как placeholder.
/// Цвет и масштаб настраиваются на ModelRenderer.Tint / WorldScale.
///
/// Phase-4: модели = primitive prefabs.
/// Phase-5+: заменить ModelPath на реальные .vmdl из Resources/Models/.
/// </summary>
public static class EntitySpawner
{
	public const string BoxModel = "models/dev/box.vmdl";
	public const string SphereModel = "models/dev/sphere.vmdl";
	public const string PlaneModel = "models/dev/plane.vmdl";

	/// <summary>
	/// Создаёт GameObject c ModelRenderer + ModelCollider + Rigidbody + SpawnedEntityComponent.
	/// </summary>
	public static GameObject SpawnPrimitive(
		Vector3 position,
		string entityType,
		Connection? owner,
		Color tint,
		Vector3 scale,
		string modelPath = BoxModel,
		bool gravity = true )
	{
		if ( !Networking.IsHost )
			throw new InvalidOperationException( "EntitySpawner может работать только на хосте." );

		var go = new GameObject
		{
			Name = entityType,
			WorldPosition = position + Vector3.Up * 16f, // приподнять чтобы не влипало
			WorldScale = scale,
		};

		// Визуал (примитив)
		var renderer = go.Components.Create<ModelRenderer>();
		renderer.Model = Model.Load( modelPath );
		renderer.Tint = tint;

		// Коллизия + физика
		go.Components.Create<ModelCollider>().Model = renderer.Model;

		var body = go.Components.Create<Rigidbody>();
		body.Gravity = gravity;
		body.MassOverride = 50f;

		// Метаданные владения
		var spawned = go.Components.Create<SpawnedEntityComponent>();
		spawned.EntityType = entityType;
		if ( owner is not null )
			spawned.SetOwner( owner );

		go.NetworkSpawn();
		return go;
	}

	/// <summary>
	/// Проверка лимита и быстрый спавн с уведомлением.
	/// Возвращает GameObject или null если лимит превышен.
	/// </summary>
	public static GameObject? TrySpawnFor( Connection owner, string entityType,
		Vector3 position, Color tint, Vector3 scale, string modelPath = BoxModel )
	{
		if ( !EntityLimits.CanSpawn( owner.SteamId, entityType ) )
		{
			owner.Notify( LanguageSystem.Get( "limit", entityType ), NotifyType.Error );
			return null;
		}
		return SpawnPrimitive( position, entityType, owner, tint, scale, modelPath );
	}

	/// <summary>Получить позицию перед игроком (для спавна купленного предмета).</summary>
	public static Vector3 GetSpawnPositionInFrontOf( Connection ply )
	{
		var pawn = ply.Pawn;
		if ( pawn is null ) return Vector3.Zero;

		var forward = pawn.WorldRotation.Forward;
		return pawn.WorldPosition + forward * 60f;
	}
}
