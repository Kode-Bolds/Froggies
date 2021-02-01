using Kodebolds.Core;
using UnityEditor;
using UnityEngine;

namespace Froggies.EditorScripts
{
	[CustomEditor(typeof(GameInit))]
	public class GameInitEditor : Editor
	{
		private bool m_kodeboldBehvioursFoldout = true;
		private bool m_otherDependencies = false; //TODO: Add capability for other dependencies such as SO's.

		public override void OnInspectorGUI()
		{
			GameInit gameInit = target as GameInit;

			EditorGUI.BeginChangeCheck();

			m_kodeboldBehvioursFoldout = EditorGUILayout.Foldout(m_kodeboldBehvioursFoldout, "Kodebold Behaviours");
			if (m_kodeboldBehvioursFoldout)
			{
				EditorGUI.indentLevel++;

				for (int behaviourIndex = 0; behaviourIndex < gameInit.KodeboldBehaviours.Count; behaviourIndex++)
				{
					EditorGUILayout.BeginHorizontal();

					if (GUILayout.Button("-"))
					{
						gameInit.KodeboldBehaviours.RemoveAt(behaviourIndex);

						if (behaviourIndex >= gameInit.KodeboldBehaviours.Count)
							continue;
					}

					if (GUILayout.Button("+"))
					{
						gameInit.KodeboldBehaviours.Add(null);
					}


					gameInit.KodeboldBehaviours[behaviourIndex] = EditorGUILayout.ObjectField(gameInit.KodeboldBehaviours[behaviourIndex], typeof(KodeboldBehaviour), false) as KodeboldBehaviour;

					EditorGUILayout.EndHorizontal();
				}

				if (gameInit.KodeboldBehaviours.Count == 0 && GUILayout.Button("Add New Behaviour"))
				{
					gameInit.KodeboldBehaviours.Add(null);
				}

				EditorGUI.indentLevel--;
			}

			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(gameInit);
		}
	}
}