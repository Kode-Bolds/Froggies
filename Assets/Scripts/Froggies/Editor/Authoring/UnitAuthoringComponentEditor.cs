using Froggies;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Froggies.EditorScripts
{
	[CustomEditor(typeof(UnitAuthoringComponent))]
	public class UnitAuthoringComponentEditor : Editor
	{
		private static bool m_defensesFoldout;
		private static bool m_freezeRotationFoldout;
		private static bool m_movementFoldout;
		private static bool m_harvestingFoldout;
		private static bool m_combatFoldout;

		public override void OnInspectorGUI()
		{
			UnitAuthoringComponent unitAuthoring = target as UnitAuthoringComponent;

			EditorGUI.BeginChangeCheck();

			unitAuthoring.unitCentreTransform = (Transform)EditorGUILayout.ObjectField("Centre Transform", unitAuthoring.unitCentreTransform, typeof(Transform), true);
			unitAuthoring.unitType = (UnitType)EditorGUILayout.EnumFlagsField("Unit Type", unitAuthoring.unitType);
			unitAuthoring.isEnemy = EditorGUILayout.Toggle("Is Enemy", unitAuthoring.isEnemy);

			m_defensesFoldout = EditorGUILayout.Foldout(m_defensesFoldout, "Defenses");
			if (m_defensesFoldout)
			{
				unitAuthoring.health.health = EditorGUILayout.IntField("Health", unitAuthoring.health.health);
				unitAuthoring.resistances.armour = EditorGUILayout.IntField("Armour", unitAuthoring.resistances.armour);
				unitAuthoring.resistances.resistanceFlags = (DamageType)EditorGUILayout.EnumFlagsField("Resistances", unitAuthoring.resistances.resistanceFlags);
			}

			m_freezeRotationFoldout = EditorGUILayout.Foldout(m_freezeRotationFoldout, "Freeze Rotation");
			if (m_freezeRotationFoldout)
			{
				EditorGUI.indentLevel++;
				unitAuthoring.freezeRotation.x = EditorGUILayout.Toggle("X", unitAuthoring.freezeRotation.x);
				unitAuthoring.freezeRotation.y = EditorGUILayout.Toggle("Y", unitAuthoring.freezeRotation.y);
				unitAuthoring.freezeRotation.z = EditorGUILayout.Toggle("Z", unitAuthoring.freezeRotation.z);
				EditorGUI.indentLevel--;
			}

			m_movementFoldout = EditorGUILayout.Foldout(m_movementFoldout, "Movement");
			if (m_movementFoldout)
			{
				EditorGUI.indentLevel++;
				unitAuthoring.unitMove.moveSpeed = EditorGUILayout.FloatField("Movement Speed (m/s)", unitAuthoring.unitMove.moveSpeed);
				unitAuthoring.unitMove.turnRate = EditorGUILayout.FloatField("Turn Rate (°)", unitAuthoring.unitMove.turnRate);
				EditorGUI.indentLevel--;
			}

			if ((unitAuthoring.unitType & UnitType.Harvester) != 0)
			{
				m_harvestingFoldout = EditorGUILayout.Foldout(m_harvestingFoldout, "Harvesting");
				if (m_harvestingFoldout)
				{
					EditorGUI.indentLevel++;
					unitAuthoring.harvester.carryCapacity = EditorGUILayout.IntField("Carry Capacity", unitAuthoring.harvester.carryCapacity);
					unitAuthoring.harvester.harvestAmount = EditorGUILayout.IntField("Harvest Amount", unitAuthoring.harvester.harvestAmount);
					unitAuthoring.harvester.harvestRange = EditorGUILayout.FloatField("Harvest Range", unitAuthoring.harvester.harvestRange);
					unitAuthoring.harvester.harvestTickCooldown = EditorGUILayout.FloatField("Harvest Cooldown", unitAuthoring.harvester.harvestTickCooldown);
					unitAuthoring.harvester.harvestTickTimer = unitAuthoring.harvester.harvestTickCooldown;
					EditorGUI.indentLevel--;
				}
			}

			if ((unitAuthoring.unitType & (UnitType.Melee | UnitType.Ranged)) != 0)
			{
				m_combatFoldout = EditorGUILayout.Foldout(m_combatFoldout, "Combat");
				if (m_combatFoldout)
				{
					EditorGUI.indentLevel++;
					unitAuthoring.combatUnit.attackDamage = EditorGUILayout.IntField("Attack Damage", unitAuthoring.combatUnit.attackDamage);
					unitAuthoring.combatUnit.damageType = (DamageType)EditorGUILayout.EnumFlagsField("Damage Type", unitAuthoring.combatUnit.damageType);
					unitAuthoring.combatUnit.attackRange = EditorGUILayout.IntField("Attack Range", unitAuthoring.combatUnit.attackRange);
					unitAuthoring.combatUnit.attackSpeed = EditorGUILayout.FloatField("Attack Speed (a/s)", unitAuthoring.combatUnit.attackSpeed);

					//TODO: Implement ranged unit authoring.
					if ((unitAuthoring.unitType & UnitType.Ranged) != 0)
					{
						unitAuthoring.rangedUnit.accuracy = math.clamp(EditorGUILayout.FloatField("Accuracy", unitAuthoring.rangedUnit.accuracy), 0, 1);
						unitAuthoring.projectileGameObject = (ProjectileAuthoringComponent)EditorGUILayout.ObjectField("Projectile", unitAuthoring.projectileGameObject, typeof(ProjectileAuthoringComponent), false);
						unitAuthoring.projectileSpawnTransform = (Transform)EditorGUILayout.ObjectField("Projectile Spawn Transform", unitAuthoring.projectileSpawnTransform, typeof(Transform), true);
					}

					EditorGUI.indentLevel--;
				}

			}

			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(unitAuthoring);
		}
	}
}