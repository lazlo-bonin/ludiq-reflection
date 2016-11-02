using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Ludiq.Reflection
{
	[Serializable]
	public class UnityMethod : UnityMember, ISerializationCallbackReceiver
	{
		/// <summary>
		/// The underlying reflected method.
		/// </summary>
		public MethodInfo methodInfo { get; private set; }

		/// <summary>
		/// Whether the reflected method is an extension method.
		/// </summary>
		private bool isExtension;

		[SerializeField]
		private string[] _parameterTypes;
		private Type[] __parameterTypes;
		/// <summary>
		/// The types of the method's parameters.
		/// </summary>
		public Type[] parameterTypes
		{
			get { return __parameterTypes; }
			set { __parameterTypes = value; isReflected = false; }
		}

		public void OnAfterDeserialize()
		{
			if (_parameterTypes != null)
			{
				parameterTypes = _parameterTypes.Select(typeName => TypeSerializer.Deserialize(typeName)).ToArray();
			}
		}

		public void OnBeforeSerialize()
		{
			if (parameterTypes != null)
			{
				_parameterTypes = parameterTypes.Select(type => TypeSerializer.Serialize(type)).ToArray();
			}
		}

		#region Constructors

		public UnityMethod() { }

		public UnityMethod(string name) : base(name) { }
		public UnityMethod(string name, UnityObject target) : base(name, target) { }
		public UnityMethod(string component, string name) : base(component, name) { }
		public UnityMethod(string component, string name, UnityObject target) : base(component, name, target) { }

		public UnityMethod(string name, Type[] parameterTypes) : base(name) { this.parameterTypes = parameterTypes; }
		public UnityMethod(string name, Type[] parameterTypes, UnityObject target) : this(name, parameterTypes) { this.target = target; Reflect(); }
		public UnityMethod(string component, string name, Type[] parameterTypes) : base(component, name) { this.parameterTypes = parameterTypes; }
		public UnityMethod(string component, string name, Type[] parameterTypes, UnityObject target) : this(component, name, parameterTypes) { this.target = target; Reflect(); }

		#endregion

		/// <inheritdoc />
		public override void Reflect()
		{
#if !NETFX_CORE
			isExtension = false;

			if (!isAssigned)
			{
				throw new Exception("Method name not specified.");
			}

			EnsureTargeted();

			Type type = reflectionTarget.GetType();
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

			if (parameterTypes != null) // Explicit matching
			{
				methodInfo = type.GetMethod(name, flags, null, parameterTypes, null);

				if (methodInfo == null)
				{
					methodInfo = type.GetExtensionMethods()
						.Where(extension => extension.Name == name)
						.Where(extension => Enumerable.SequenceEqual(extension.GetParameters().Select(paramInfo => paramInfo.ParameterType), parameterTypes))
						.FirstOrDefault();

					if (methodInfo != null)
					{
						isExtension = true;
					}
				}

				if (methodInfo == null)
				{
					throw new Exception(string.Format("No matching method found: '{0}.{1} ({2})'", type.Name, name, string.Join(", ", parameterTypes.Select(t => t.Name).ToArray())));
				}
			}
			else // Implicit matching
			{
				var normalMethods = type.GetMember(name, MemberTypes.Method, flags).OfType<MethodInfo>().ToList();
				var extensionMethods = type.GetExtensionMethods().Where(extension => extension.Name == name).ToList();
				var methods = new List<MethodInfo>();
				methods.AddRange(normalMethods);
				methods.AddRange(extensionMethods);

				if (methods.Count == 0)
				{
					throw new Exception(string.Format("No matching method found: '{0}.{1}'", type.Name, name));
				}

				if (methods.Count > 1)
				{
					throw new Exception(string.Format("Multiple method signatures found for '{0}.{1}'\nSpecify the parameter types explicitly.", type.FullName, name));
				}
				
				methodInfo = methods[0];

				if (extensionMethods.Contains(methodInfo))
				{
					isExtension = true;
				}
			}

			isReflected = true;
#else
			throw new Exception("UnityMethod is not supported in .NET Core.");
#endif
		}

		/// <summary>
		/// Invokes the method with any number of arguments of any type and returns its return value, or null if there isn't any (void).
		/// </summary>
		public object Invoke(params object[] parameters)
		{
			EnsureReflected();

			if (isExtension)
			{
				var fullParameters = new object[parameters.Length + 1];
				fullParameters[0] = reflectionTarget;
				Array.Copy(parameters, 0, fullParameters, 1, parameters.Length);
				parameters = fullParameters;
			}

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

		public override bool Corresponds(UnityMember other)
		{
			return 
				other is UnityMethod && 
				base.Corresponds(other) && 
				this.parameterTypes.SequenceEqual(((UnityMethod)other).parameterTypes);
		}
	}
}