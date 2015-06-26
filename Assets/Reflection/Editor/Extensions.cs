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
		// Used to print pretty type names for primities
		private static CSharpCodeProvider csharp = new CSharpCodeProvider();

		/// <summary>
		/// Returns the name for the given type where primitives are in their shortcut form.
		/// </summary>
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

		/// <summary>
		/// Splits a given property into each of its multiple values.
		/// If it has a single value, only the same property is returned.
		/// </summary>
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