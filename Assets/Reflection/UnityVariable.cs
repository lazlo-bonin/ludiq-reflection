using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEngine.Reflection
{
	[Serializable]
	public class UnityVariable : UnityMember
	{
		public FieldInfo field { get; private set; }
		public PropertyInfo property { get; private set; }

		public override void Reflect()
		{
			EnsureTargeted();

			Type type = realTarget.GetType();
			MemberTypes types = MemberTypes.Property | MemberTypes.Field;
			BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

			MemberInfo[] variables = type.GetMember(name, types, flags);

			if (variables.Length == 0)
			{
				throw new Exception(string.Format("No matching field or property found: '{0}.{1}'", type.FullName, name));
			}

			MemberInfo variable = variables[0]; // Safe, because there can't possibly be more than one variable of the same name

			field = variable as FieldInfo;
			property = variable as PropertyInfo;

			isReflected = true;
		}

		public object Get()
		{
			EnsureReflected();

			if (field != null)
			{
				return field.GetValue(realTarget);
			}

			if (property != null)
			{
				return property.GetValue(realTarget, null);
			}

			throw new InvalidOperationException();
		}

		public T Get<T>()
		{
			return (T)Get();
		}

		public void Set(object value)
		{
			EnsureReflected();

			if (field != null)
			{
				field.SetValue(realTarget, value);
			}

			if (property != null)
			{
				property.SetValue(realTarget, value, null);
			}

			throw new InvalidOperationException();
		}

		public Type variableType
		{
			get
			{
				EnsureReflected();

				if (field != null)
				{
					return field.FieldType;
				}

				if (property != null)
				{
					return property.PropertyType;
				}

				throw new InvalidOperationException();
			}
		}
	}
}