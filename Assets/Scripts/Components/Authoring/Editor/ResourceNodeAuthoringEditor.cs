using UnityEditor;

[CustomEditor(typeof(ResourceNodeAuthoring))]
public class ResourceNodeAuthoringEditor : Editor
{
	public override void OnInspectorGUI()
	{
		ResourceNodeAuthoring resourceNodeAuthoring = target as ResourceNodeAuthoring;

		EditorGUI.BeginChangeCheck();

		resourceNodeAuthoring.resourceNode.resourceType = (ResourceType)EditorGUILayout.EnumPopup("Resource Type", resourceNodeAuthoring.resourceNode.resourceType);
		resourceNodeAuthoring.aiTarget.targetType = (AITargetType)resourceNodeAuthoring.resourceNode.resourceType;
		resourceNodeAuthoring.resourceNode.resourceAmount = EditorGUILayout.IntField("Resource Amount", resourceNodeAuthoring.resourceNode.resourceAmount);

		if (EditorGUI.EndChangeCheck())
			EditorUtility.SetDirty(resourceNodeAuthoring);
	}
}
