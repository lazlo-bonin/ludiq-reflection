using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace UnityEngine.Reflection
{
	[CustomPropertyDrawer(typeof(UnityVariable))]
	public class UnityVariableDrawer : UnityMemberDrawer
	{
		protected override string memberLabel
		{
			get
			{
				return "Variable";
			}
		}

		protected override MemberTypes validMemberTypes
		{
			get
			{
				return MemberTypes.Field | MemberTypes.Property;
			}
		}

		protected override bool ValidateMember(MemberInfo member)
		{
			bool valid = base.ValidateMember(member);

			FieldInfo field = member as FieldInfo;
			PropertyInfo property = member as PropertyInfo;

			if (field != null)
			{
				valid &= ValidateMemberType(field.FieldType);

				if (!reflectionAttribute.ReadOnly) valid &= !field.IsLiteral || !field.IsInitOnly;
			}

			if (property != null)
			{
				valid &= ValidateMemberType(property.PropertyType);

				if (!reflectionAttribute.ReadOnly) valid &= property.CanWrite;
				if (!reflectionAttribute.WriteOnly) valid &= property.CanRead;
			}

			return valid;
		}
	}
}