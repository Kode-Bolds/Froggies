using Kodebolds.Core;
using UnityEngine;
using Unity.Mathematics;

namespace Froggies
{
	public class SelectionBoxRenderer : KodeboldBehaviour
	{
		[SerializeField] private Texture m_selectionBoxTexture;

		private float2 m_selectionBoxStartPos;
		private float2 m_selectionBoxEndPos;
		private bool _draw;
		private InputManagementSystem m_inputManagementSystem;

		protected override GameState ActiveGameState => GameState.Updating;

		public override void GetBehaviourDependencies(Dependencies dependencies)
		{
			m_inputManagementSystem = dependencies.GetDependency<InputManagementSystem>();
		}

		public override void InitBehaviour()
		{

		}

		public override void UpdateBehaviour()
		{
			if (m_inputManagementSystem.InputData.mouseInput.leftClickPressed)
			{
				m_selectionBoxStartPos = m_inputManagementSystem.InputData.mouseInput.mouseScreenPos;
				_draw = true;
			}

			if (m_inputManagementSystem.InputData.mouseInput.leftClickDown)
			{
				m_selectionBoxEndPos = m_inputManagementSystem.InputData.mouseInput.mouseScreenPos;
			}

			if (m_inputManagementSystem.InputData.mouseInput.leftClickReleased)
			{
				_draw = false;
			}
		}

		private void OnGUI()
		{
			if (_draw)
			{
				GUI.DrawTexture(
					new Rect(
						m_selectionBoxStartPos.x,
						Screen.height - m_selectionBoxStartPos.y,
						m_selectionBoxEndPos.x - m_selectionBoxStartPos.x,
					   -1 * ((Screen.height - m_selectionBoxStartPos.y) - (Screen.height - m_selectionBoxEndPos.y))),
					m_selectionBoxTexture);
			}
		}

		public override void FreeBehaviour()
		{

		}
	}
}
