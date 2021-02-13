using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Froggies
{
	[Serializable]
	public struct Projectile : IComponentData
	{
		public float projectileSpeed;
		[HideInInspector] public int damage;
		[HideInInspector] public DamageType damageType;
		[HideInInspector] public Entity targetEntity;
		[HideInInspector] public float3 targetPos;
	}
}