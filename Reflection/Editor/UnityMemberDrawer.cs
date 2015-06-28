using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Ludiq.Reflection
{
	[CustomPropertyDrawer(typeof(UnityMember))]
	public abstract class UnityMemberDrawer : TargetedDrawer
	{
		#region Fields

		/// <summary>
		/// The filter attribute on the inspected field.
		/// </summary>
		protected FilterAttribute filter;

		/// <summary>
		/// The inspected property, of type UnityMember.
		/// </summary>
		protected SerializedProperty property;

		/// <summary>
		/// The UnityMember.component of the inspected property, of type string.
		/// </summary>
		protected SerializedProperty componentProperty;

		/// <summary>
		/// The UnityMember.name of the inspected property, of type string.
		/// </summary>
		protected SerializedProperty nameProperty;

		/// <summary>
		/// The targeted Unity Objects.
		/// </summary>
		protected UnityObject[] targets;

		/// <summary>
		/// The type of targeted objects.
		/// </summary>
		protected UnityObjectType targetType;

		#endregion

		/// <inheritdoc />
		protected override void Update(SerializedProperty property)
		{
			// Update the targeted drawer
			base.Update(property);

			// Assign the property and sub-properties
			this.property = property;
			componentProperty = property.FindPropertyRelative("_component");
			nameProperty = property.FindPropertyRelative("_name");

			// Fetch the filter
			filter = (FilterAttribute)fieldInfo.GetCustomAttributes(typeof(FilterAttribute), true).FirstOrDefault() ?? DefaultFilter();

			// Find the targets
			targets = FindTargets();
			targetType = DetermineTargetType();
		}

		/// <inheritdoc />
		protected override void RenderMemberControl(Rect position)
		{
			// Other Targets
			// Some Unity Objects, like Assets, are not supported by the drawer. 
			// Just display an error message to let the user change their target.

			if (targetType == UnityObjectType.Other)
			{
				EditorGUI.HelpBox(position, "Unsupported Unity Object type.", MessageType.None);
				return;
			}

			// Display a list of all available reflected members in a popup.

			var options = new List<PopupOption<string>>();

			// The selected option
			// TODO: Figure out a way to display the label instead of the popup selection
			// Forum: http://forum.unity3d.com/threads/336305/
			PopupOption<string> selectedOption = null;

			if (targetType == UnityObjectType.GameObject)
			{
				// Check if all targets have a GameObject (none are empty).
				// If they do, display all members of the GameObject type.

				if (HasSharedGameObject())
				{
					var gameObjectOptions = GetMemberOptions(typeof(GameObject));

					foreach (var gameObjectOption in gameObjectOptions)
					{
						// Prefix label by GameObject for popup clarity.

						gameObjectOption.label = string.Format("GameObject/{0}", gameObjectOption.label);

						options.Add(gameObjectOption);
					}
				}

				// Find all shared component types across targets.
				// Display all members of each one found.

				foreach (Type componentType in GetSharedComponentTypes())
				{
					var componentOptions = GetMemberOptions(componentType);

					foreach (var componentOption in componentOptions)
					{
						// Prefix label and option by component type for clear distinction.

						componentOption.value = string.Format("{0}.{1}", componentType.Name, componentOption.value);
						componentOption.label = string.Format("{0}/{1}", componentType.Name, componentOption.label);

						options.Add(componentOption);
					}
				}

				// Determine which option is currently selected.
				// If no component is specified, the current member is directly on the GameObject.
				// If a component is specified, the current member is on that component.
				// Adapt the prefixes to match our hidden values defined earlier.

				if (!string.IsNullOrEmpty(nameProperty.stringValue))
				{
					if (string.IsNullOrEmpty(componentProperty.stringValue))
					{
						string value = nameProperty.stringValue;
						string label = string.Format("GameObject.{0}", nameProperty.stringValue);
						selectedOption = new PopupOption<string>(value, label);
					}
					else
					{
						selectedOption = new PopupOption<string>(string.Format("{0}.{1}", componentProperty.stringValue, nameProperty.stringValue));
					}
				}
			}
			else if (targetType == UnityObjectType.ScriptableObject)
			{
				// ScriptableObject Target
				// Make sure all targets share the same ScriptableObject Type.
				// If they do, display all members of that type.

				Type scriptableObjectType = GetSharedScriptableObjectType();

				if (scriptableObjectType != null)
				{
					options.AddRange(GetMemberOptions(scriptableObjectType));

					// Determine which option is currently selected.
					// Since the component property is ignored on ScriptableObjects,
					// it should always be equal to the name property.

					if (!string.IsNullOrEmpty(nameProperty.stringValue))
					{
						selectedOption = new PopupOption<string>(nameProperty.stringValue);
					}
				}
			}

			// Make sure the callback uses the property of this drawer, not at its later value.
			var propertyNow = property;

			PopupGUI<string>.Render
			(
				value => UpdateMember(propertyNow, value),
				position, 
				options, 
				selectedOption, 
				componentProperty.hasMultipleDifferentValues || nameProperty.hasMultipleDifferentValues, 
				string.Format("No {0}", memberLabel),
				targetType != UnityObjectType.None
			);
		}

		protected void UpdateMember(SerializedProperty property, string value)
		{
			Update(property);

			if (value == null)
			{
				// No Value
				// Set the properties to null.

				componentProperty.stringValue = null;
				nameProperty.stringValue = null;
			}
			else if (targetType == UnityObjectType.GameObject)
			{
				// GameObject Target
				// Check if the new value is in dot-notation, which would indicate
				// it refers to a component and a name. Otherwise, it only refers
				// to a name.

				if (value.Contains('.'))
				{
					string[] newOptionParts = value.Split('.');

					componentProperty.stringValue = newOptionParts[0];
					nameProperty.stringValue = newOptionParts[1];
				}
				else
				{
					componentProperty.stringValue = null;
					nameProperty.stringValue = value;
				}
			}
			else if (targetType == UnityObjectType.ScriptableObject)
			{
				// ScriptableObject Target
				// The component property is always ignored, therefore only
				// set the name property to the new value.

				componentProperty.stringValue = null;
				nameProperty.stringValue = value;
			}

			property.serializedObject.ApplyModifiedProperties();
		}

		#region Targeting

		/// <summary>
		/// Get the list of targets on the inspected objects.
		/// </summary>
		protected UnityObject[] FindTargets()
		{
			if (isSelfTargeted)
			{
				// In self targeting mode, the targets are the inspected objects themselves.

				return property.serializedObject.targetObjects;
			}
			else
			{
				// In manual targeting mode, the targets the values of each target property.

				return targetProperty.Multiple().Select(p => p.objectReferenceValue).ToArray();
			}
		}

		/// <summary>
		/// Determine the Unity type of the targets.
		/// </summary>
		protected UnityObjectType DetermineTargetType()
		{
			UnityObjectType unityObjectType = UnityObjectType.None;

			foreach (UnityObject targetObject in targets)
			{
				// Null (non-specified) targets don't affect the type
				// If no non-null target is specified, the type will be None
				// as the loop will simply exit.

				if (targetObject == null)
				{
					continue;
				}

				if (targetObject is GameObject || targetObject is Component)
				{
					// For GameObjects and Components, the target is either the
					// GameObject itself, or the one to which the Component belongs.
					
					// If a ScriptableObject target was previously found,
					// return that the targets are of mixed types.

					if (unityObjectType == UnityObjectType.ScriptableObject)
					{
						return UnityObjectType.Mixed;
					}

					unityObjectType = UnityObjectType.GameObject;
				}
				else if (targetObject is ScriptableObject)
				{
					// For ScriptableObjects, the target is simply the
					// ScriptableObject itself.

					// If a GameObject target was previously found,
					// return that the targets are of mixed types.

					if (unityObjectType == UnityObjectType.GameObject)
					{
						return UnityObjectType.Mixed;
					}

					unityObjectType = UnityObjectType.ScriptableObject;
				}
				else
				{
					// Other target types

					return UnityObjectType.Other;
				}
			}

			return unityObjectType;
		}

		/// <summary>
		/// Determines if the targets all share a GameObject.
		/// </summary>
		public bool HasSharedGameObject()
		{
			return !targets.Contains(null);
		}

		/// <summary>
		/// Determines which types of Components are shared on all GameObject targets.
		/// </summary>
		protected IEnumerable<Type> GetSharedComponentTypes()
		{
			if (targets.Contains(null))
			{
				return Enumerable.Empty<Type>();
			}

			var childrenComponents = targets.OfType<GameObject>().Select(gameObject => gameObject.GetComponents<Component>());
			var siblingComponents = targets.OfType<Component>().Select(component => component.GetComponents<Component>());

			return childrenComponents.Concat(siblingComponents)
				.Select(components => components.Select(component => component.GetType()))
				.IntersectAll()
				.Distinct();
		}

		/// <summary>
		/// Determines which type of ScriptableObject is shared across targets.
		/// Returns null if none are shared.
		/// </summary>
		protected Type GetSharedScriptableObjectType()
		{
			if (targets.Contains(null))
			{
				return null;
			}

			return targets
				.OfType<ScriptableObject>()
				.Select(scriptableObject => scriptableObject.GetType())
				.Distinct()
				.SingleOrDefault(); // Null (default) if multiple or zero
		}

		#endregion

		#region Reflection

		/// <summary>
		/// Gets the list of members available on a type as popup options.
		/// </summary>
		protected List<PopupOption<string>> GetMemberOptions(Type type)
		{
			var options = new List<PopupOption<string>>();

			var members = type
				.GetMembers(validBindingFlags)
				.Where(member => validMemberTypes.HasFlag(member.MemberType))
				.Where(ValidateMember);

			var memberNames = new List<string>();

			foreach (MemberInfo member in members)
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

				options.Add(new PopupOption<string>(value, label));
				memberNames.Add(member.Name);
			}

			// Alphabetic sort: 
			// options.Sort((o1, o2) => o1.value.CompareTo(o2.value));

			return options;
		}

		#endregion

		#region Filtering

		/// <summary>
		/// The label of a member, displayed in options.
		/// </summary>
		protected virtual string memberLabel
		{
			get
			{
				return "Member";
			}
		}

		/// <summary>
		/// The default applied filter attribute if none is specified.
		/// </summary>
		protected virtual FilterAttribute DefaultFilter()
		{
			return new FilterAttribute();
		}

		/// <summary>
		/// The valid BindingFlags when looking for reflected members.
		/// </summary>
		protected virtual BindingFlags validBindingFlags
		{
			get
			{
				// Build the flags from the filter attribute

				BindingFlags flags = (BindingFlags)0;

				if (filter.Public) flags |= BindingFlags.Public;
				if (filter.NonPublic) flags |= BindingFlags.NonPublic;
				if (filter.Instance) flags |= BindingFlags.Instance;
				if (filter.Static) flags |= BindingFlags.Static;
				if (!filter.Inherited) flags |= BindingFlags.DeclaredOnly;

				return flags;
			}
		}

		/// <summary>
		/// The valid MemberTypes when looking for reflected members.
		/// </summary>
		protected virtual MemberTypes validMemberTypes
		{
			get
			{
				return MemberTypes.All;
			}
		}

		/// <summary>
		/// Determines whether a given MemberInfo should be included in the options.
		/// This check follows the BindingFlags and MemberTypes filtering. 
		/// </summary>
		protected virtual bool ValidateMember(MemberInfo member)
		{
			return true;
		}

		/// <summary>
		/// Determines whether a MemberInfo of the given type should be included in the options.
		/// </summary>
		protected virtual bool ValidateMemberType(Type type)
		{
			bool validFamily = false;
			bool validType;

			// Allow type families based on the filter attribute

			TypeFamily families = filter.TypeFamilies;

			if (families.HasFlag(TypeFamily.Array)) validFamily |= type.IsArray;
			if (families.HasFlag(TypeFamily.Class)) validFamily |= type.IsClass;
			if (families.HasFlag(TypeFamily.Enum)) validFamily |= type.IsEnum;
			if (families.HasFlag(TypeFamily.Interface)) validFamily |= type.IsInterface;
			if (families.HasFlag(TypeFamily.Primitive)) validFamily |= type.IsPrimitive;
			if (families.HasFlag(TypeFamily.Reference)) validFamily |= !type.IsValueType;
			if (families.HasFlag(TypeFamily.Value)) validFamily |= type.IsValueType && type != typeof(void);

			// Allow types based on the filter attribute
			// If no filter types are specified, all types are allowed.

			if (filter.Types.Count > 0)
			{
				validType = false;

				foreach (Type allowedType in filter.Types)
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

		#endregion
	}
}