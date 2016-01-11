using System;
using System.Linq;
using System.Reflection;
using Ludiq.Controls.Editor;
using UnityEditor;

namespace Ludiq.Reflection.Editor
{
	[CustomPropertyDrawer(typeof(UnityMethod))]
	public class UnityMethodDrawer : UnityMemberDrawer<UnityMethod>
	{
		#region Filtering

		/// <inheritdoc />
		protected override FilterAttribute DefaultFilter()
		{
			FilterAttribute filter = base.DefaultFilter();

			// Override defaults here

			return filter;
		}

		// Do not edit below

		/// <inheritdoc />
		protected override MemberTypes validMemberTypes
		{
			get
			{
				return MemberTypes.Method;
			}
		}

		/// <inheritdoc />
		protected override bool ValidateMember(MemberInfo member)
		{
			bool valid = base.ValidateMember(member);

			MethodInfo method = (MethodInfo)member;

			// Validate type based on return type
			valid &= ValidateMemberType(method.ReturnType);

			// Exclude special compiler methods
			valid &= !method.IsSpecialName;

			// Exclude generic methods
			// TODO: Generic type (de)serialization
			valid &= !method.ContainsGenericParameters;

			return valid;
		}

		#endregion

		#region Fields

		/// <summary>
		/// The UnityMethod.parameterTypes of the inspected property, of type Type[].
		/// </summary>
		protected SerializedProperty parameterTypesProperty;

		protected override void Update(SerializedProperty property)
		{
			base.Update(property);

			parameterTypesProperty = property.FindPropertyRelative("_parameterTypes");
		}

		#endregion

		#region Value

		/// <inheritdoc />
		protected override void SetValue(UnityMethod value)
		{
			base.SetValue(value);

			// Assign the parameter types to their underlying properties

			if (value == null || value.parameterTypes == null)
			{
				parameterTypesProperty.arraySize = 0;
			}
			else
			{
				parameterTypesProperty.arraySize = value.parameterTypes.Length;

				for (int i = 0; i < parameterTypesProperty.arraySize; i++)
				{
					SerializedProperty parameterTypeProperty = parameterTypesProperty.GetArrayElementAtIndex(i);

					parameterTypeProperty.stringValue = TypeSerializer.Serialize(value.parameterTypes[i]);
				}
			}
		}

		/// <inheritdoc />
		protected override UnityMethod BuildValue(string component, string name)
		{
			// Fetch the parameter types from their underlying properties

			Type[] parameterTypes = new Type[parameterTypesProperty.arraySize];

			for (int i = 0; i < parameterTypesProperty.arraySize; i++)
			{
				SerializedProperty parameterTypeProperty = parameterTypesProperty.GetArrayElementAtIndex(i);

				parameterTypes[i] = TypeSerializer.Deserialize(parameterTypeProperty.stringValue);
			}

			return new UnityMethod(component, name, parameterTypes);
		}

		/// <inheritdoc />
		protected override bool hasMultipleDifferentValues
		{
			get
			{
				if (base.hasMultipleDifferentValues)
				{
					return true;
				}

				string[] last = null;

				foreach (SerializedProperty arrayProperty in parameterTypesProperty.Multiple())
				{
					string[] current = new string[arrayProperty.arraySize];

					for (int i = 0; i < arrayProperty.arraySize; i++)
					{
						SerializedProperty parameterTypeProperty = arrayProperty.GetArrayElementAtIndex(i);
						current[i] = parameterTypeProperty.stringValue;
					}

					if (last != null && !current.SequenceEqual(last))
					{
						return true;
					}

					last = current;
				}

				return false;
			}
		}

		#endregion

		#region Reflection

		protected override PopupOption<UnityMethod> GetMemberOption(MemberInfo member, string component)
		{
			UnityMethod value;
			string label;

			if (member is MethodInfo)
			{
				MethodInfo method = (MethodInfo)member;

				ParameterInfo[] parameters = method.GetParameters();

				value = new UnityMethod(component, member.Name, parameters.Select(p => p.ParameterType).ToArray());

				string parameterString = string.Join(", ", parameters.Select(p => p.ParameterType.PrettyName()).ToArray());

				label = string.Format("{0} {1} ({2})", method.ReturnType.PrettyName(), member.Name, parameterString);
			}
			else
			{
				throw new ArgumentException("Invalid member information type.");
			}

			return new PopupOption<UnityMethod>(value, label);
		}

		#endregion

		/// <inheritdoc />
		protected override string memberLabel
		{
			get
			{
				return "Method";
			}
		}
	}
}