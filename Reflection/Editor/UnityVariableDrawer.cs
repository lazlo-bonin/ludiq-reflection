using System;
using System.Reflection;
using Ludiq.Controls;
using UnityEditor;

namespace Ludiq.Reflection.Editor
{
	[CustomPropertyDrawer(typeof(UnityVariable))]
	public class UnityVariableDrawer : UnityMemberDrawer<UnityVariable>
	{
		#region Filtering

		/// <inheritdoc />
		protected override FilterAttribute DefaultFilter()
		{
			FilterAttribute filter = base.DefaultFilter();

			// Override defaults here

			return filter;
		}

		/// <inheritdoc />
		protected override MemberTypes validMemberTypes
		{
			get
			{
				return MemberTypes.Field | MemberTypes.Property;
			}
		}

		/// <inheritdoc />
		protected override bool ValidateMember(MemberInfo member)
		{
			bool valid = base.ValidateMember(member);

			FieldInfo field = member as FieldInfo;
			PropertyInfo property = member as PropertyInfo;

			if (field != null) // Member is a field
			{
				// Validate type based on field type
				valid &= ValidateMemberType(field.FieldType);

				// Exclude constants (literal) and readonly (init) fields if
				// the filter rejects read-only fields.
				if (!filter.ReadOnly) valid &= !field.IsLiteral || !field.IsInitOnly;
			}
			else if (property != null) // Member is a method
			{
				// Validate type based on property type
				valid &= ValidateMemberType(property.PropertyType);

				// Exclude read-only and write-only properties
				if (!filter.ReadOnly) valid &= property.CanWrite;
				if (!filter.WriteOnly) valid &= property.CanRead;
			}

			return valid;
		}

		// Do not edit below

		#endregion

		#region Value

		/// <inheritdoc />
		protected override UnityVariable BuildValue(string component, string name)
		{
			return new UnityVariable(component, name);
		}

		#endregion

		#region Reflection

		protected override PopupOption<UnityVariable> GetMemberOption(MemberInfo member, string component)
		{
			UnityVariable value;
			string label;

			if (member is FieldInfo)
			{
				FieldInfo field = (FieldInfo)member;

				value = new UnityVariable(component, field.Name);
				label = string.Format("{0} {1}", field.FieldType.PrettyName(), field.Name);
			}
			else if (member is PropertyInfo)
			{
				PropertyInfo property = (PropertyInfo)member;

				value = new UnityVariable(component, property.Name);
				label = string.Format("{0} {1}", property.PropertyType.PrettyName(), property.Name);
			}
			else
			{
				throw new ArgumentException("Invalid member information type.");
			}

			return new PopupOption<UnityVariable>(value, label);
		}

		#endregion

		/// <inheritdoc />
		protected override string memberLabel
		{
			get
			{
				return "Variable";
			}
		}
	}
}