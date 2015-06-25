using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace UnityEngine.Reflection
{
	[CustomPropertyDrawer(typeof(UnityMethod))]
	public class UnityMethodDrawer : UnityMemberDrawer
	{
		protected override string memberLabel
		{
			get
			{
				return "Method";
			}
		}

		protected override MemberTypes validMemberTypes
		{
			get
			{
				return MemberTypes.Method;
			}
		}

		protected override bool ValidateMember(MemberInfo member)
		{
			bool valid = base.ValidateMember(member);

			MethodInfo method = (MethodInfo)member;

			valid &= ValidateMemberType(method.ReturnType);
			valid &= !method.IsSpecialName;

			return valid;
		}
	}
}