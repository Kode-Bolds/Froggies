using UnityEngine;

public abstract class KodeboldBehaviour : MonoBehaviour, IDependant, IDependency
{
	public void GetDependencies(Dependencies dependencies)
	{
		GetBehaviourDependencies(dependencies);
	}

	public abstract void GetBehaviourDependencies(Dependencies dependencies);

	public void Init()
	{
		InitBehaviour();
	}

	public abstract void InitBehaviour();

	public abstract void UpdateBehaviour();

	public void Free()
	{
		FreeBehaviour();
	}

	public abstract void FreeBehaviour();
}
