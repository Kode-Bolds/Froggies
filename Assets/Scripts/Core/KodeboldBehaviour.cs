using System.Collections;
using System.Collections.Generic;
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

	public void Free()
	{
		FreeBehaviour();
	}

	public abstract void FreeBehaviour();
}
