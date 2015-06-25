using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace UnityEngine.Reflection
{
	[CustomPropertyDrawer(typeof(UnityMember))]
	public abstract class UnityMemberDrawer : PropertyDrawer
	{
		protected enum UnityObjectType
		{
			None,
			Mixed,
			GameObject,
			ScriptableObject,
			Other
		}

		protected struct Option<T>
		{
			public static readonly Option<T> separator = new Option<T>(default(T), "");

			public T value;
			public string label;

			public Option(T value, string label)
			{
				this.value = value;
				this.label = label;
			}
		}

		protected ReflectionAttribute reflectionAttribute;
		protected bool selfTargeted;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			reflectionAttribute = (ReflectionAttribute)fieldInfo.GetCustomAttributes(typeof(ReflectionAttribute), true).FirstOrDefault() ?? DefaultReflectionAttribute();
			selfTargeted = Attribute.IsDefined(fieldInfo, typeof(SelfTargetedAttribute));

			// Properties

			SerializedProperty targetProperty = property.FindPropertyRelative("_target");
			SerializedProperty componentProperty = property.FindPropertyRelative("_component");
			SerializedProperty nameProperty = property.FindPropertyRelative("_name");

			// Type

			IEnumerable<Object> targetObjects;

			if (selfTargeted)
			{
				targetObjects = property.serializedObject.targetObjects;
			}
			else
			{
				targetObjects = targetProperty.Multiple().Select(p => p.objectReferenceValue);
			}

			UnityObjectType unityObjectType = DetermineUnityObjectType(targetObjects);

			// Mixed

			if (unityObjectType == UnityObjectType.Mixed)
			{
				// When selections are mixed, the editor doesn't display the inspector anyway.
				// It displays a selection refinement instead. Stop just in case.
				return;
			}

			// Positioning

			EditorGUI.BeginProperty(position, label, property);

			position = EditorGUI.IndentedRect(position);
			int oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			position.height = base.GetPropertyHeight(property, label);
			position.y += EditorGUI.PrefixLabel(position, label).height + LabelPadding;

			Rect targetPosition = position;
			Rect memberPosition = position;

			if (!selfTargeted || true)
			{
				targetPosition.width *= (1f / 3f);
				targetPosition.width -= (InnerPadding / 2);

				memberPosition.width *= (2f / 3f);
				memberPosition.width -= (InnerPadding / 2);
				memberPosition.x = targetPosition.xMax + InnerPadding;
			}

			// Target

			if (selfTargeted)
			{
				foreach (var singleTargetProperty in targetProperty.Multiple())
				{
					singleTargetProperty.objectReferenceValue = singleTargetProperty.serializedObject.targetObject;
				}

				EditorGUI.BeginDisabledGroup(true); // TODO:  Remove debug field.
				EditorGUI.PropertyField(targetPosition, targetProperty, GUIContent.none);
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				EditorGUI.PropertyField(targetPosition, targetProperty, GUIContent.none);
			}

			if (unityObjectType == UnityObjectType.Other)
			{
				EditorGUI.HelpBox(memberPosition, "Unsupported Unity Object type.", MessageType.None);
				return;
			}

			// Member

			var options = new List<Option<string>>();

			string multipleValues = "#_MULTIPLE";

			options.Add(new Option<string>(null, string.Format("No {0}", memberLabel)));

			string selectedValue = null;
			//string selectedLabel = null; // TODO: Figure out a way to display that instead when the popup isn't focused

			if (unityObjectType == UnityObjectType.GameObject)
			{
				if (HasSharedGameObject(targetObjects))
				{
					var gameObjectOptions = GetMemberOptions(typeof(GameObject));

					foreach (var gameObjectOption in gameObjectOptions)
					{
						var prefixedOption = gameObjectOption;
						prefixedOption.label = string.Format("GameObject/{0}", gameObjectOption.label);
						options.Add(prefixedOption);
					}
				}

				foreach (Type componentType in GetSharedComponentTypes(targetObjects))
				{
					var componentOptions = GetMemberOptions(componentType);

					foreach (var componentOption in componentOptions)
					{
						var prefixedOption = componentOption;
						prefixedOption.value = string.Format("{0}.{1}", componentType.Name, componentOption.value);
						prefixedOption.label = string.Format("{0}/{1}", componentType.Name, componentOption.label);
						options.Add(prefixedOption);
					}
				}

				if (string.IsNullOrEmpty(componentProperty.stringValue))
				{
					selectedValue = nameProperty.stringValue;
					//selectedLabel = string.format("GameObject.{0}", nameProperty.stringValue);
				}
				else
				{
					selectedValue = string.Format("{0}.{1}", componentProperty.stringValue, nameProperty.stringValue);
					//selectedLabel = selectedValue;
				}
			}
			else if (unityObjectType == UnityObjectType.ScriptableObject)
			{
				Type scriptableObjectType = GetSharedScriptableObjectType(targetObjects);

				if (scriptableObjectType != null)
				{
					options.AddRange(GetMemberOptions(scriptableObjectType));

					selectedValue = componentProperty.stringValue;
					//selectedLabel = selectedValue;
				}
			}

			if (componentProperty.hasMultipleDifferentValues || nameProperty.hasMultipleDifferentValues)
			{
				options.Insert(0, new Option<string>(multipleValues, "—"));
				selectedValue = multipleValues;
				options.Insert(1, Option<string>.separator);
			}

			if (options.Count > (selectedValue == multipleValues ? 3 : 1))
			{
				options.Insert(selectedValue == multipleValues ? 3 : 1, Option<string>.separator);
			}

			int selectedIndex = Mathf.Max(0, options.FindIndex(option => option.value == selectedValue));

			string[] optionLabels = options.Select(option => option.label).ToArray();

			if (unityObjectType == UnityObjectType.None) EditorGUI.BeginDisabledGroup(true);

			int newIndex = EditorGUI.Popup
			(
				memberPosition,
				selectedIndex,
				optionLabels
			);

			if (unityObjectType == UnityObjectType.None) EditorGUI.EndDisabledGroup();

			string newOption = options[newIndex].value;


			if (newOption == multipleValues)
			{
				// Nothing to do
			}
			else if (newOption == null)
			{
				componentProperty.stringValue = null;
				nameProperty.stringValue = null;
			}
			else if (unityObjectType == UnityObjectType.GameObject)
			{
				if (newOption.Contains('.'))
				{
					string[] newOptionParts = newOption.Split('.');

					componentProperty.stringValue = newOptionParts[0];
					nameProperty.stringValue = newOptionParts[1];
				}
				else
				{
					componentProperty.stringValue = null;
					nameProperty.stringValue = newOption;
				}
			}
			else if (unityObjectType == UnityObjectType.ScriptableObject)
			{
				componentProperty.stringValue = null;
				nameProperty.stringValue = newOption;
			}

			EditorGUI.indentLevel = oldIndent;

			EditorGUI.EndProperty();
		}

		protected UnityObjectType DetermineUnityObjectType(IEnumerable<Object> targetObjects)
		{
			UnityObjectType unityObjectType = UnityObjectType.None;

			foreach (Object targetObject in targetObjects)
			{
				if (targetObject == null)
				{
					continue;
				}

				if (targetObject is GameObject || targetObject is Component)
				{
					if (unityObjectType == UnityObjectType.ScriptableObject)
					{
						return UnityObjectType.Mixed;
					}

					unityObjectType = UnityObjectType.GameObject;
				}
				else if (targetObject is ScriptableObject)
				{
					if (unityObjectType == UnityObjectType.GameObject)
					{
						return UnityObjectType.Mixed;
					}

					unityObjectType = UnityObjectType.ScriptableObject;
				}
				else
				{
					return UnityObjectType.Other;
				}
			}

			return unityObjectType;
		}

		public bool HasSharedGameObject(IEnumerable<Object> targetObjects)
		{
			return !targetObjects.Contains(null);
		}

		protected IEnumerable<Type> GetSharedComponentTypes(IEnumerable<Object> targetObjects)
		{
			if (targetObjects.Contains(null))
			{
				return Enumerable.Empty<Type>();
			}

			var childrenComponents = targetObjects.OfType<GameObject>().Select(gameObject => gameObject.GetComponents<Component>());
			var siblingComponents = targetObjects.OfType<Component>().Select(component => component.GetComponents<Component>());

			return childrenComponents
				.Concat(siblingComponents)
				.Select(components => components.Select(component => component.GetType()))
				.IntersectAll()
				.Distinct();
		}

		protected Type GetSharedScriptableObjectType(IEnumerable<Object> targetObjects)
		{
			if (targetObjects.Contains(null))
			{
				return null;
			}

			return targetObjects
				.OfType<ScriptableObject>()
				.Select(scriptableObject => scriptableObject.GetType())
				.Distinct()
				.SingleOrDefault();
		}

		protected List<Option<string>> GetMemberOptions(Type type)
		{
			var options = new List<Option<string>>();

			var memberNames = new List<string>(); ;

			foreach(MemberInfo member in 
				type
				.GetMembers(validBindingFlags)
				.Where(member => validMemberTypes.HasFlag(member.MemberType))
				.Where(ValidateMember))
			{
				if (memberNames.Contains(member.Name))
				{
					options.RemoveAll(o => o.value == member.Name); // Remove duplicate
					continue;
				}

				string value = member.Name;
				string label = member.Name;

				if (member is FieldInfo)
				{
					label = string.Format("{0} {1}", ((FieldInfo)member).FieldType.PrettyName(), member.Name);
				}
				else if (member is PropertyInfo)
				{
					label = string.Format("{0} {1}", ((PropertyInfo)member).PropertyType.PrettyName(), member.Name);
				}
				else if (member is MethodInfo)
				{
					label = string.Format("{0} {1} ()", ((MethodInfo)member).ReturnType.PrettyName(), member.Name);
				}

				options.Add(new Option<string>(value, label));
				memberNames.Add(member.Name);
			}

			// Alphabetic sort
			// options.Sort((o1, o2) => o1.value.CompareTo(o2.value));

			return options;
		}

		protected const float LabelPadding = 2;
		protected const float BottomPadding = 7;
		protected const float InnerPadding = 5;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return base.GetPropertyHeight(property, label) * 2 + LabelPadding + BottomPadding;
		}

		protected virtual string memberLabel
		{
			get
			{
				return "Member";
			}
		}

		protected virtual ReflectionAttribute DefaultReflectionAttribute()
		{
			return new ReflectionAttribute();
		}

		protected virtual BindingFlags validBindingFlags
		{
			get
			{
				BindingFlags flags = (BindingFlags)0;

				if (reflectionAttribute.Public) flags |= BindingFlags.Public;
				if (reflectionAttribute.NonPublic) flags |= BindingFlags.NonPublic;
				if (reflectionAttribute.Instance) flags |= BindingFlags.Instance;
				if (reflectionAttribute.Static) flags |= BindingFlags.Static;
				if (!reflectionAttribute.Inherited) flags |= BindingFlags.DeclaredOnly;

				return flags;
			}
		}

		protected virtual MemberTypes validMemberTypes
		{
			get
			{
				return MemberTypes.All;
			}
		}

		protected virtual bool ValidateMember(MemberInfo member)
		{
			return true;
		}

		protected virtual bool ValidateMemberType(Type type)
		{
			bool validFamily = false;
			bool validType;

			TypeFamily families = reflectionAttribute.TypeFamilies;

			if (families.HasFlag(TypeFamily.Array)) validFamily |= type.IsArray;
			if (families.HasFlag(TypeFamily.Class)) validFamily |= type.IsClass;
			if (families.HasFlag(TypeFamily.Enum)) validFamily |= type.IsEnum;
			if (families.HasFlag(TypeFamily.Interface)) validFamily |= type.IsInterface;
			if (families.HasFlag(TypeFamily.Primitive)) validFamily |= type.IsPrimitive;
			if (families.HasFlag(TypeFamily.Reference)) validFamily |= !type.IsValueType;
			if (families.HasFlag(TypeFamily.Value)) validFamily |= type.IsValueType && type != typeof(void);

			if (reflectionAttribute.Types.Length > 0)
			{
				validType = false;

				foreach (Type allowedType in reflectionAttribute.Types)
				{
					if (allowedType.IsAssignableFrom(type))
					{
						validType = true;
						break;
					}
				}
			}
			else
			{
				validType = true;
			}

			return validFamily && validType;
		}
	}
}