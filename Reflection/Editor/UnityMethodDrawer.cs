using System.Reflection;
using UnityEditor;

namespace Ludiq.Reflection
{
	[CustomPropertyDrawer(typeof(UnityMethod))]
	public class UnityMethodDrawer : UnityMemberDrawer
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
				return "Method";
			}
		}

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

			return valid;
		}
	}
}