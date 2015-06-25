using UnityEngine;
using System.Collections;
using System;

namespace UnityEngine.Reflection
{
	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
	public sealed class SelfTargetedAttribute : Attribute
	{

	}
}