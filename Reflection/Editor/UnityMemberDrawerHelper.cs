using System;
using System.Linq;
using Ludiq.Controls.Editor;
using Ludiq.Reflection.Internal;
using UnityEditor;

namespace Ludiq.Reflection.Editor
{
	public static class UnityMemberDrawerHelper
	{
		/// <summary>
		/// Assign the parameter types to their underlying properties
		/// </summary>
		public static void SerializeParameterTypes(SerializedProperty parameterTypesProperty, Type[] parameterTypes)
		{
			if (parameterTypes == null)
			{
				parameterTypesProperty.arraySize = 0;
			}
			else
			{
				parameterTypesProperty.arraySize = parameterTypes.Length;

				for (int i = 0; i < parameterTypesProperty.arraySize; i++)
				{
					SerializedProperty parameterTypeProperty = parameterTypesProperty.GetArrayElementAtIndex(i);

					parameterTypeProperty.stringValue = TypeSerializer.Serialize(parameterTypes[i]);
				}
			}
		}

		/// <summary>
		/// Fetch the parameter types from their underlying properties
		/// </summary>
		public static Type[] DeserializeParameterTypes(SerializedProperty parameterTypesProperty)
		{
			Type[] parameterTypes = new Type[parameterTypesProperty.arraySize];

			for (int i = 0; i < parameterTypesProperty.arraySize; i++)
			{
				SerializedProperty parameterTypeProperty = parameterTypesProperty.GetArrayElementAtIndex(i);

				parameterTypes[i] = TypeSerializer.Deserialize(parameterTypeProperty.stringValue);
			}

			return parameterTypes;
		}

		public static bool ParameterTypesHasMultipleValues(SerializedProperty parameterTypesProperty)
		{
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
}