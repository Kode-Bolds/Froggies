using Froggies;
using UnityEditor;

namespace Froggies.EditorScripts
{
	[CustomEditor(typeof(ResourceNodeAuthoringComponent))]
	public class ResourceNodeAuthoringEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			ResourceNodeAuthoringComponent resourceNodeAuthoring = target as ResourceNodeAuthoringComponent;

			EditorGUI.BeginChangeCheck();

			resourceNodeAuthoring.resourceNode.resourceType = (ResourceType)EditorGUILayout.EnumPopup("Resource Type", resourceNodeAuthoring.resourceNode.resourceType);
			resourceNodeAuthoring.aiTarget.targetType = (AITargetType)resourceNodeAuthoring.resourceNode.resourceType;
			resourceNodeAuthoring.resourceNode.resourceAmount = EditorGUILayout.IntField("Resource Amount", resourceNodeAuthoring.resourceNode.resourceAmount);

			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(resourceNodeAuthoring);
		}
	}
}