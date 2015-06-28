using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Ludiq.Reflection
{
	[Serializable]
	public class UnityMethod : UnityMember
	{
		/// <summary>
		/// The underlying reflected method.
		/// </summary>
		public MethodInfo methodInfo { get; private set; }
		
		#region Constructors

		public UnityMethod() { }
		public UnityMethod(string name) : base(name) { }
		public UnityMethod(string name, UnityObject target) : base(name, target) { }
		public UnityMethod(string component, string name) : base(component, name) { }
		public UnityMethod(string component, string name, UnityObject target) : base(component, name, target) { }

		#endregion

		/// <inheritdoc />
		public override void Reflect()
		{
			EnsureTargeted();

			Type type = reflectionTarget.GetType();
			MemberTypes types = MemberTypes.Method;
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

			MethodInfo[] methods = type.GetMember(name, types, flags).OfType<MethodInfo>().ToArray();

			if (methods.Length == 0)
			{
				throw new Exception(string.Format("No matching method found: '{0}.{1}'", type.FullName, name));
			}

			// TODO: Method overloading support.
			// Requires method signature distinction and serialization.
			if (methods.Length > 1)
			{
				Debug.LogWarningFormat("Multiple matching method names found: '{0}.{1}'\nThe first one will be used.", type.FullName, name);
			}

			methodInfo = methods[0];

			isReflected = true;
		}

		/// <summary>
		/// Invokes the method with any number of arguments of any type and returns its return value, or null if there isn't any (void).
		/// </summary>
		public object Invoke(params object[] parameters)
		{
			EnsureReflected();

			return methodInfo.Invoke(reflectionTarget, parameters);
		}

		/// <summary>
		/// Invokes the method with any number of arguments of any type and returns its return value casted to the specified type, or null if there isn't any (void).
		/// </summary>
		public T Invoke<T>(params object[] parameters)
		{
			return (T)Invoke(parameters);
		}

		/// <summary>
		/// The return type of the reflected method.
		/// </summary>
		public Type returnType
		{
			get
			{
				EnsureReflected();

				return methodInfo.ReturnType;
			}
		}
	}
}