using System;
using System.Reflection;
using UnityObject = UnityEngine.Object;

namespace Ludiq.Reflection
{
	[Serializable]
	public class UnityVariable : UnityMember
	{
		/// <summary>
		/// The underlying reflected field, or null if the variable is a property.
		/// </summary>
		public FieldInfo fieldInfo { get; private set; }

		/// <summary>
		/// The underlying property field, or null if the variable is a field.
		/// </summary>
		public PropertyInfo propertyInfo { get; private set; }
		
		#region Constructors

		public UnityVariable() { }
		public UnityVariable(string name) : base(name) { }
		public UnityVariable(string name, UnityObject target) : base(name, target) { }
		public UnityVariable(string component, string name) : base(component, name) { }
		public UnityVariable(string component, string name, UnityObject target) : base(component, name, target) { }

		#endregion

		/// <inheritdoc />
		public override void Reflect()
		{
#if !NETFX_CORE
			if (!isAssigned)
			{
				throw new Exception("Field or property name not specified.");
			}

			EnsureTargeted();

			Type type = reflectionTarget.GetType();
			MemberTypes types = MemberTypes.Property | MemberTypes.Field;
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

			MemberInfo[] variables = type.GetMember(name, types, flags);

			if (variables.Length == 0)
			{
				throw new Exception(string.Format("No matching field or property found: '{0}.{1}'", type.Name, name));
			}

			MemberInfo variable = variables[0]; // Safe, because there can't possibly be more than one variable of the same name

			fieldInfo = variable as FieldInfo;
			propertyInfo = variable as PropertyInfo;

			isReflected = true;
#else
			throw new Exception("UnityVariable is not supported in .NET Core.");
#endif
		}

		/// <summary>
		/// Retrieves the value of the variable.
		/// </summary>
		public object Get()
		{
			EnsureReflected();

			if (fieldInfo != null)
			{
				return fieldInfo.GetValue(reflectionTarget);
			}

			if (propertyInfo != null)
			{
				return propertyInfo.GetValue(reflectionTarget, null);
			}

			throw new InvalidOperationException();
		}

		/// <summary>
		/// Retrieves the value of the variable casted to the specified type.
		/// </summary>
		public T Get<T>()
		{
			return (T)Get();
		}

		/// <summary>
		/// Assigns a new value to the variable.
		/// </summary>
		public void Set(object value)
		{
			EnsureReflected();

			if (fieldInfo != null)
			{
				fieldInfo.SetValue(reflectionTarget, value);
				return;
			}

			if (propertyInfo != null)
			{
				propertyInfo.SetValue(reflectionTarget, value, null);
				return;
			}

			throw new InvalidOperationException();
		}

		/// <summary>
		/// The type of the reflected field or property.
		/// </summary>
		public Type type
		{
			get
			{
				EnsureReflected();

				if (fieldInfo != null)
				{
					return fieldInfo.FieldType;
				}

				if (propertyInfo != null)
				{
					return propertyInfo.PropertyType;
				}

				throw new InvalidOperationException();
			}
		}

		public override bool Corresponds(UnityMember other)
		{
			return other is UnityVariable && base.Corresponds(other);
		}
	}
}