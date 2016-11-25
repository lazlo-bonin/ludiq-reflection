using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Ludiq.Reflection.Internal;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Ludiq.Reflection
{
	[Serializable]
	public class UnityMember : ISerializationCallbackReceiver
	{
		public enum SourceType
		{
			Unknown,
			Field,
			Property,
			Method
		}

		public SourceType sourceType { get; private set; }

		/// <summary>
		/// The underlying reflected field, or null if the getter is a property or method.
		/// </summary>
		public FieldInfo fieldInfo { get; private set; }

		/// <summary>
		/// The underlying property field, or null if the getter is a field or method.
		/// </summary>
		public PropertyInfo propertyInfo { get; private set; }

		/// <summary>
		/// The underlying reflected method, or null if the getter is a field or property.
		/// </summary>
		public MethodInfo methodInfo { get; private set; }

		/// <summary>
		/// Whether the reflected method is an extension method.
		/// </summary>
		public bool isExtension { get; private set; }

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

		[SerializeField]
		private UnityObject _target;
		/// <summary>
		/// The object containing the member.
		/// </summary>
		public UnityObject target
		{
			get { return _target; }
			set { _target = value; isTargeted = false; }
		}

		[SerializeField]
		private string _component;
		/// <summary>
		/// The name of the component containing the member, if the target is a GameObject.
		/// </summary>
		public string component
		{
			get { return _component; }
			set { _component = value; isTargeted = false; isReflected = false; }
		}

		[SerializeField]
		private string _name;
		/// <summary>
		/// The name of the member.
		/// </summary>
		public string name
		{
			get { return _name; }
			set { _name = value; isReflected = false; }
		}

		/// <summary>
		/// Indicates whether the reflection target has been found and cached.
		/// </summary>
		protected bool isTargeted { get; private set; }

		/// <summary>
		/// Indicates whether the reflection data has been found and cached.
		/// </summary>
		public bool isReflected { get; protected set; }

		/// <summary>
		/// Indicates whether the member has been properly assigned.
		/// </summary>
		public bool isAssigned
		{
			get
			{
				return target != null && !string.IsNullOrEmpty(name);
			}
		}

		/// <summary>
		/// The object on which to perform reflection.
		/// For GameObjects and Component targets, this is the component of type <see cref="UnityMember.component"/> or the object itself if null.
		/// For ScriptableObjects targets, this is the object itself. 
		/// Other Unity Objects are not supported.
		/// </summary>
		protected UnityObject reflectionTarget { get; private set; }

		#region Constructors

		public UnityMember() { }

		public UnityMember(string name)
		{
			this.name = name;
		}

		public UnityMember(string name, UnityObject target)
		{
			this.name = name;
			this.target = target;

			Reflect();
		}

		public UnityMember(string component, string name)
		{
			this.component = component;
			this.name = name;
		}

		public UnityMember(string component, string name, UnityObject target)
		{
			this.component = component;
			this.name = name;
			this.target = target;

			Reflect();
		}

		// TODO: Expand for clarity instead of inheriting : this
		public UnityMember(string name, Type[] parameterTypes) : this(name) { this.parameterTypes = parameterTypes; }
		public UnityMember(string name, Type[] parameterTypes, UnityObject target) : this(name, parameterTypes) { this.target = target; Reflect(); }
		public UnityMember(string component, string name, Type[] parameterTypes) : this(component, name) { this.parameterTypes = parameterTypes; }
		public UnityMember(string component, string name, Type[] parameterTypes, UnityObject target) : this(component, name, parameterTypes) { this.target = target; Reflect(); }

		#endregion

		/// <summary>
		/// Gathers and caches the reflection target for the member.
		/// </summary>
		protected void Target()
		{
			if (target == null)
			{
				throw new NullReferenceException("Target has not been specified.");
			}

			GameObject targetAsGameObject = target as GameObject;
			Component targetAsComponent = target as Component;

			if (targetAsGameObject != null || targetAsComponent != null)
			{
				// The target is a GameObject or a Component.

				if (!string.IsNullOrEmpty(component))
				{
					// If a component is specified, look for it on the target.

					Component componentObject;

					if (targetAsGameObject != null)
					{
						componentObject = targetAsGameObject.GetComponent(component);
					}
					else // if (targetAsComponent != null)
					{
						componentObject = targetAsComponent.GetComponent(component);
					}

					if (componentObject == null)
					{
						throw new UnityReflectionException(string.Format("Target object does not contain a component of type '{0}'.", component));
					}

					reflectionTarget = componentObject;
					return;
				}
				else
				{
					// Otherwise, return the GameObject itself.

					if (targetAsGameObject != null)
					{
						reflectionTarget = targetAsGameObject;
					}
					else // if (targetAsComponent != null)
					{
						reflectionTarget = targetAsComponent.gameObject;
					}

					return;
				}
			}

			ScriptableObject scriptableObject = target as ScriptableObject;

			if (scriptableObject != null)
			{
				// The real target is directly the ScriptableObject target.

				reflectionTarget = scriptableObject;
				return;
			}

			throw new UnityReflectionException("Target should be a GameObject, a Component or a ScriptableObject.");
		}

		private const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

		/// <summary>
		/// Gathers and caches the reflection data for the member.
		/// </summary>
		public void Reflect()
		{
#if !NETFX_CORE
			EnsureAssigned();
			EnsureTargeted();

			fieldInfo = null;
			propertyInfo = null;
			methodInfo = null;
			sourceType = SourceType.Unknown;

			Type type = reflectionTarget.GetType();
			MemberTypes types = MemberTypes.Property | MemberTypes.Field | MemberTypes.Method;

			MemberInfo[] members = type.GetMember(name, types, bindingFlags);

			if (members.Length == 0)
			{
				throw new UnityReflectionException(string.Format("No matching field, property or method found: '{0}.{1}'", type.Name, name));
			}

			var memberType = members[0].MemberType;

			switch (memberType)
			{
				case MemberTypes.Field: sourceType = SourceType.Field; break;
				case MemberTypes.Property: sourceType = SourceType.Property; break;
				case MemberTypes.Method: sourceType = SourceType.Method; break;
				default: throw new UnityReflectionException();
			}

			switch (sourceType)
			{
				case SourceType.Field: fieldInfo = (FieldInfo)members[0]; break;
				case SourceType.Property: propertyInfo = (PropertyInfo)members[0]; break;
				case SourceType.Method: ReflectMethod(type); break;
				default: throw new UnityReflectionException();
			}

			isReflected = true;
#else
			throw new Exception("Reflection is not supported in .NET Core.");
#endif
		}

		private void ReflectMethod(Type type)
		{
#if !NETFX_CORE
			if (parameterTypes != null) // Explicit matching
			{
				methodInfo = type.GetMethod(name, bindingFlags, null, parameterTypes, null);

				if (methodInfo == null)
				{
					methodInfo = type.GetExtensionMethods()
						.Where(extension => extension.Name == name)
						.Where(extension => Enumerable.SequenceEqual(extension.GetParameters().Select(paramInfo => paramInfo.ParameterType), parameterTypes))
						.FirstOrDefault();
				}

				if (methodInfo == null)
				{
					throw new UnityReflectionException(string.Format("No matching method found: '{0}.{1} ({2})'", type.Name, name, string.Join(", ", parameterTypes.Select(t => t.Name).ToArray())));
				}
			}
			else // Implicit matching
			{
				var normalMethods = type.GetMember(name, MemberTypes.Method, bindingFlags).Cast<MethodInfo>().ToList();
				var extensionMethods = type.GetExtensionMethods().Where(extension => extension.Name == name).ToList();
				var methods = new List<MethodInfo>();
				methods.AddRange(normalMethods);
				methods.AddRange(extensionMethods);

				if (methods.Count == 0)
				{
					throw new UnityReflectionException(string.Format("No matching method found: '{0}.{1}'", type.Name, name));
				}

				if (methods.Count > 1)
				{
					throw new UnityReflectionException(string.Format("Multiple method signatures found for '{0}.{1}'\nSpecify the parameter types explicitly.", type.FullName, name));
				}

				methodInfo = methods[0];
			}

			isExtension = methodInfo.IsDefined(typeof(ExtensionAttribute), true);
#else
			throw new Exception("Reflection is not supported in .NET Core.");
#endif
		}

		/// <summary>
		/// Gathers the reflection data if it is not alreadypresent.
		/// </summary>
		public void EnsureReflected()
		{
			if (!isReflected)
			{
				Reflect();
			}
		}

		/// <summary>
		/// Gathers the reflection target if it is not already present.
		/// </summary>
		public void EnsureTargeted()
		{
			if (!isTargeted)
			{
				Target();
			}
		}

		/// <summary>
		/// Asserts that the member has been properly assigned.
		/// </summary>
		public void EnsureAssigned()
		{
			if (!isAssigned)
			{
				throw new UnityReflectionException("Member hasn't been properly assigned.");
			}
		}

		/// <summary>
		/// Retrieves the value of the field or property.
		/// </summary>
		public object Get()
		{
			EnsureReflected();

			switch (sourceType)
			{
				case SourceType.Field: return fieldInfo.GetValue(reflectionTarget);
				case SourceType.Property: return propertyInfo.GetValue(reflectionTarget, null);
				case SourceType.Method: throw new UnityReflectionException("Member is a method. Consider using 'GetOrInvoke' instead.");
				default: throw new UnityReflectionException();
			}
		}

		/// <summary>
		/// Retrieves the value of the member casted to the specified type.
		/// </summary>
		public T Get<T>()
		{
			return (T)Get();
		}

		/// <summary>
		/// Assigns a new value to the field or property.
		/// </summary>
		public void Set(object value)
		{
			EnsureReflected();

			switch (sourceType)
			{
				case SourceType.Field: fieldInfo.SetValue(reflectionTarget, value); break;
				case SourceType.Property: propertyInfo.SetValue(reflectionTarget, value, null); break;
				case SourceType.Method: throw new UnityReflectionException("Member is a method. Consider using 'InvokeOrSet' instead.");
				default: throw new UnityReflectionException();
			}
		}

		/// <summary>
		/// Invokes the method with any number of arguments of any type and returns its return value, or null if there isn't any (void).
		/// </summary>
		public object Invoke(params object[] arguments)
		{
			EnsureReflected();

			if (sourceType == SourceType.Field || sourceType == SourceType.Property)
			{
				throw new UnityReflectionException("Member is a field or property. Consider using 'InvokeOrSet' or 'GetOrInvoke' instead.");
			}
			else if (sourceType == SourceType.Method)
			{
				if (isExtension)
				{
					var fullParameters = new object[arguments.Length + 1];
					fullParameters[0] = reflectionTarget;
					Array.Copy(arguments, 0, fullParameters, 1, arguments.Length);
					arguments = fullParameters;
				}

				return methodInfo.Invoke(reflectionTarget, arguments);
			}
			else
			{
				throw new UnityReflectionException();
			}
		}

		/// <summary>
		/// Invokes the method with any number of arguments of any type and returns its return value casted to the specified type, or null if there isn't any (void).
		/// </summary>
		public T Invoke<T>(params object[] arguments)
		{
			return (T)Invoke(arguments);
		}

		/// <summary>
		/// If the member is a method, invokes it with any number of arguments of any type and returns its return value, or null if there isn't any (void).
		/// If the member is a field or property, sets its value to the first argument and returns null.
		/// </summary>
		public object InvokeOrSet(params object[] argumentsOrValue)
		{
			EnsureReflected();

			switch (sourceType)
			{
				case SourceType.Method:
					return Invoke(argumentsOrValue);

				case SourceType.Field:
				case SourceType.Property:
					if (argumentsOrValue.Length != 1)
					{
						throw new ArgumentOutOfRangeException("One argument must be provided when setting.");
					}

					Set(argumentsOrValue[0]);

					return null;

				default: throw new UnityReflectionException();
			}
		}

		/// <summary>
		/// If the member is a method, invokes it with any number of arguments of any type and returns its return value, or null if there isn't any (void).
		/// If the member is a field or property, sets its value to the first argument and returns null.
		/// </summary>
		public T InvokeOrSet<T>(params object[] arguments)
		{
			return (T)InvokeOrSet(arguments);
		}

		/// <summary>
		/// If the member is a field or property, retrieves its value.
		/// If the member is a method, invokes it with any number of arguments of any type and returns its return value, or null if there isn't any (void).
		/// </summary>
		public object GetOrInvoke(params object[] arguments)
		{
			EnsureReflected();

			switch (sourceType)
			{
				case SourceType.Method:
					return Invoke(arguments);

				case SourceType.Field:
				case SourceType.Property:
					if (arguments.Length != 0)
					{
						throw new ArgumentOutOfRangeException("Zero argument must be provided when getting.");
					}

					Get();

					return null;

				default: throw new UnityReflectionException();
			}
		}

		/// <summary>
		/// If the member is a field or property, retrieves its value.
		/// If the member is a method, invokes it with any number of arguments of any type and returns its return value, or null if there isn't any (void).
		/// </summary>
		public T GetOrInvoke<T>(params object[] arguments)
		{
			return (T)GetOrInvoke(arguments);
		}

		/// <summary>
		/// The type of the reflected field or property or return type of the reflected method.
		/// </summary>
		public Type type
		{
			get
			{
				EnsureReflected();

				switch (sourceType)
				{
					case SourceType.Field: return fieldInfo.FieldType;
					case SourceType.Property: return propertyInfo.PropertyType;
					case SourceType.Method: return methodInfo.ReturnType;
					default: throw new UnityReflectionException();
				}
			}
		}

		// Overriden for comparison in the dropdowns
		public override bool Equals(object obj)
		{
			var other = obj as UnityMember;

			return
				other != null &&
				target == other.target &&
				component == other.component &&
				name == other.name; 
			
			// TODO: Compare parameter types
		}
	}
}