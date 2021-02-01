using System;
using System.Collections.Generic;

namespace Kodebolds.Core
{
	public interface IDependency
	{
		void Free();
	}

	public interface IDependant
	{
		void GetDependencies(Dependencies dependencies);
		void Init(); //This function is for any initialisation in the dependants that needs to happen after the dependencies have been gathered.
	}

	public class Dependencies
	{
		private List<IDependency> _dependencies;

		public Dependencies(List<IDependency> dependencies)
		{
			_dependencies = dependencies;
		}

		public T GetDependency<T>() where T : IDependency
		{
			int dependencyCount = _dependencies.Count;
			for (int dependencyIndex = 0; dependencyIndex < dependencyCount; dependencyIndex++)
				if (_dependencies[dependencyIndex] is T dependency)
					return dependency;

			throw new Exception("Dependency does not exist!");
		}
	}
}