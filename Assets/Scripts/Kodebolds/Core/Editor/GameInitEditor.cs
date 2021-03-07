using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Kodebolds.Core.EditorScripts
{
	[CustomEditor(typeof(GameInit), true)]
	public class GameInitEditor : Editor
	{
		private bool m_kodeboldBehvioursFoldout = true;
		private bool m_kodeboldSOsFoldout = true;

		public override void OnInspectorGUI()
		{
			GameInit gameInit = target as GameInit;

			EditorGUI.BeginChangeCheck();
			
			gameInit.BehaviourContainer = EditorGUILayout.ObjectField(gameInit.BehaviourContainer, typeof(GameObject), true) as GameObject;
			
			m_kodeboldBehvioursFoldout = EditorGUILayout.Foldout(m_kodeboldBehvioursFoldout, "Kodebold Behaviours");
			if (m_kodeboldBehvioursFoldout)
			{
				EditorGUI.indentLevel++;

				EditorGUILayout.LabelField("Initialisation Behaviours");
				DisplayObjectList(gameInit.InitialisationKodeboldBehaviours);

				EditorGUILayout.LabelField("Update Behaviours");
				DisplayObjectList(gameInit.UpdateKodeboldBehaviours);

				EditorGUI.indentLevel--;
			}

			m_kodeboldSOsFoldout = EditorGUILayout.Foldout(m_kodeboldSOsFoldout, "Kodebold Scriptable Objects");
			if(m_kodeboldSOsFoldout)
			{
				EditorGUI.indentLevel++;
 
				DisplayObjectList(gameInit.KodeboldScriptableObjects);

				EditorGUI.indentLevel--;
			}

			if (EditorGUI.EndChangeCheck())
				EditorUtility.SetDirty(gameInit);
		}

		private void DisplayObjectList<T>(List<T> objectList) where T : Object
		{
			for (int objectIndex = 0; objectIndex < objectList.Count; objectIndex++)
			{
				EditorGUILayout.BeginHorizontal();

				if (GUILayout.Button("-"))
				{
					objectList.RemoveAt(objectIndex);

					if (objectIndex >= objectList.Count)
						continue;
				}

				if (GUILayout.Button("+"))
				{
					objectList.Add(null);
				}


				objectList[objectIndex] = EditorGUILayout.ObjectField(objectList[objectIndex], typeof(T), false) as T;

				EditorGUILayout.EndHorizontal();
			}

			if (objectList.Count == 0 && GUILayout.Button("Add New Object"))
			{
				objectList.Add(null);
			}
		}
	}
}