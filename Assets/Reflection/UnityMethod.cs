using System;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Reflection
{
	[Serializable]
	public class UnityMethod : UnityMember
	{
		public MethodInfo method { get; private set; }

		public override void Reflect()
		{
			EnsureTargeted();

			Type type = realTarget.GetType();
			MemberTypes types = MemberTypes.Method;
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

			MethodInfo[] methods = type.GetMember(name, types, flags).OfType<MethodInfo>().ToArray();

			if (methods.Length == 0)
			{
				throw new Exception(string.Format("No matching method found: '{0}.{1}'", type.FullName, name));
			}

			if (methods.Length > 1)
			{
				Debug.LogWarningFormat("Multiple matching method names found: '{0}.{1}'\nThe first one will be used.", type.FullName, name);
			}

			method = methods[0];

			isReflected = true;
		}

		public object Invoke(params object[] parameters)
		{
			EnsureReflected();

			return method.Invoke(realTarget, parameters);
		}

		public T Invoke<T>(params object[] parameters)
		{
			return (T)Invoke(parameters);
		}

		public Type returnType
		{
			get
			{
				EnsureReflected();

				return method.ReturnType;
			}
		}
	}
}