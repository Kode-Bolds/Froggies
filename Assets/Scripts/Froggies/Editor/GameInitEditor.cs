using Kodebolds.Core;
using System.Collections.Generic;
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

				EditorGUILayout.LabelField("Initialisation Behaviours");
				DisplayBehaviours(gameInit.InitialisationKodeboldBehaviours);

				EditorGUILayout.LabelField("Update Behaviours");
				DisplayBehaviours(gameInit.UpdateKodeboldBehaviours);

				EditorGUI.indentLevel--;
			}

			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(gameInit);
		}

		private void DisplayBehaviours(List<KodeboldBehaviour> behaviourList)
		{
			for (int behaviourIndex = 0; behaviourIndex < behaviourList.Count; behaviourIndex++)
			{
				EditorGUILayout.BeginHorizontal();

				if (GUILayout.Button("-"))
				{
					behaviourList.RemoveAt(behaviourIndex);

					if (behaviourIndex >= behaviourList.Count)
						continue;
				}

				if (GUILayout.Button("+"))
				{
					behaviourList.Add(null);
				}


				behaviourList[behaviourIndex] = EditorGUILayout.ObjectField(behaviourList[behaviourIndex], typeof(KodeboldBehaviour), false) as KodeboldBehaviour;

				EditorGUILayout.EndHorizontal();
			}

			if (behaviourList.Count == 0 && GUILayout.Button("Add New Behaviour"))
			{
				behaviourList.Add(null);
			}
		}
	}
}