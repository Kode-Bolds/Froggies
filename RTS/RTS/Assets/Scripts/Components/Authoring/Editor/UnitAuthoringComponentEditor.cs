using UnityEditor;

[CustomEditor(typeof(UnitAuthoringComponent))]
public class UnitAuthoringComponentEditor : Editor
{
	private static bool m_FreezeRotationFoldout;
	private static bool m_MovementFoldout;
	private static bool m_HarvestingFoldout;

	public override void OnInspectorGUI()
	{
		UnitAuthoringComponent unitAuthoring = target as UnitAuthoringComponent;

		EditorGUI.BeginChangeCheck();

		unitAuthoring.unitType = (UnitType)EditorGUILayout.EnumFlagsField("Unit Type", unitAuthoring.unitType);
		unitAuthoring.isEnemy = EditorGUILayout.Toggle("Is Enemy", unitAuthoring.isEnemy);

		m_FreezeRotationFoldout = EditorGUILayout.Foldout(m_FreezeRotationFoldout, "Freeze Rotation");
		if(m_FreezeRotationFoldout)
		{
			EditorGUI.indentLevel++;
			unitAuthoring.freezeRotation.x = EditorGUILayout.Toggle("X", unitAuthoring.freezeRotation.x);
			unitAuthoring.freezeRotation.y = EditorGUILayout.Toggle("Y", unitAuthoring.freezeRotation.y);
			unitAuthoring.freezeRotation.z = EditorGUILayout.Toggle("Z", unitAuthoring.freezeRotation.z);
			EditorGUI.indentLevel--;
		}

		m_MovementFoldout = EditorGUILayout.Foldout(m_MovementFoldout, "Movement");
		if(m_MovementFoldout)
		{
			EditorGUI.indentLevel++;
			unitAuthoring.unitMove.moveSpeed = EditorGUILayout.FloatField("Movement Speed", unitAuthoring.unitMove.moveSpeed);
			EditorGUI.indentLevel--;
		}

		if((unitAuthoring.unitType & UnitType.Harvester) != 0)
		{
			m_HarvestingFoldout = EditorGUILayout.Foldout(m_HarvestingFoldout, "Harvesting");
			if (m_HarvestingFoldout)
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

		if (EditorGUI.EndChangeCheck())
			EditorUtility.SetDirty(unitAuthoring);
	}
}
