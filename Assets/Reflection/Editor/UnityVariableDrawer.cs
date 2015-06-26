using System.Reflection;
using UnityEditor;

namespace UnityEngine.Reflection
{
	[CustomPropertyDrawer(typeof(UnityVariable))]
	public class UnityVariableDrawer : UnityMemberDrawer
	{
		/// <inheritdoc />
		protected override FilterAttribute DefaultFilter()
		{
			FilterAttribute filter = base.DefaultFilter();

			// Override defaults here

			return filter;
		}

		/// <inheritdoc />
		protected override string memberLabel
		{
			get
			{
				return "Variable";
			}
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
	}
}