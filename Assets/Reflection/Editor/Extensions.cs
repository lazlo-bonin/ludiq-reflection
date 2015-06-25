using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEngine.Reflection
{
	public static class Extensions
	{
		private static CSharpCodeProvider csharp = new CSharpCodeProvider();

		public static string PrettyName(this Type type)
		{
			string cSharpOutput = csharp.GetTypeOutput(new CodeTypeReference(type));

			if (cSharpOutput.Contains('.'))
			{
				return cSharpOutput.Substring(cSharpOutput.LastIndexOf('.') + 1);
			}
			else
			{
				return cSharpOutput;
			}
		}

		public static IEnumerable<SerializedProperty> Multiple(this SerializedProperty property)
		{
			if (property.hasMultipleDifferentValues)
			{
				return property.serializedObject.targetObjects.Select(o => new SerializedObject(o).FindProperty(property.propertyPath));
			}
			else
			{
				return new[] { property };
			}
		}
	}
}