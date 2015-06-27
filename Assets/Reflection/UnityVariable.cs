using System;
using System.Reflection;

namespace UnityEngine.Reflection
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
		public UnityVariable(string name, Object target) : base(name, target) { }
		public UnityVariable(string component, string name) : base(component, name) { }
		public UnityVariable(string component, string name, Object target) : base(component, name, target) { }

		#endregion

		/// <inheritdoc />
		public override void Reflect()
		{
			EnsureTargeted();

			Type type = reflectionTarget.GetType();
			MemberTypes types = MemberTypes.Property | MemberTypes.Field;
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

			MemberInfo[] variables = type.GetMember(name, types, flags);

			if (variables.Length == 0)
			{
				throw new Exception(string.Format("No matching field or property found: '{0}.{1}'", type.FullName, name));
			}

			MemberInfo variable = variables[0]; // Safe, because there can't possibly be more than one variable of the same name

			fieldInfo = variable as FieldInfo;
			propertyInfo = variable as PropertyInfo;

			isReflected = true;
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
			}

			if (propertyInfo != null)
			{
				propertyInfo.SetValue(reflectionTarget, value, null);
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
	}
}