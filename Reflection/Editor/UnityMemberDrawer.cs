using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ludiq.Controls.Editor;
using Ludiq.Reflection.Internal;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Ludiq.Reflection.Editor
{
	[CustomPropertyDrawer(typeof(UnityMember))]
	public class UnityMemberDrawer : TargetedDrawer
	{
		internal static FilterAttribute filterOverride;
		internal static bool? labelTypeAfterOverride;

		#region Fields

		/// <summary>
		/// The filter attribute on the inspected field.
		/// </summary>
		protected FilterAttribute filter;

		/// <summary>
		/// Whether to display the label type after the name.
		/// </summary>
		private bool labelTypeAfter = false;

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
		/// The UnityMethod.parameterTypes of the inspected property, of type Type[].
		/// </summary>
		protected SerializedProperty parameterTypesProperty;

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
			parameterTypesProperty = property.FindPropertyRelative("_parameterTypes");

			// Fetch the filter
			filter = filterOverride ?? (FilterAttribute)fieldInfo.GetCustomAttributes(typeof(FilterAttribute), true).FirstOrDefault() ?? new FilterAttribute();

			// Check for the label type after attribute
			labelTypeAfter = labelTypeAfterOverride ?? fieldInfo.IsDefined(typeof(LabelTypeAfterAttribute), true);

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

			UnityMember value = GetValue();

			DropdownOption<UnityMember> selectedOption = null;

			if (value != null)
			{
				if (targetType == UnityObjectType.GameObject)
				{
					string label;

					if (value.component == null)
					{
						label = string.Format("GameObject.{0}", value.name);
					}
					else
					{
						label = string.Format("{0}.{1}", value.component, value.name);
					}

					// There seems to be no way of differentiating between null parameter types
					// (fields, properties and implicitly typed methods) and zero parameter types
					// because Unity's serialized property array cannot be assigned to null, only
					// given an array size of zero.

					// TODO: The solution would be to use a single comma-separated
					// string instead of an array of strings, which we could differentiate manually.
					if (value.parameterTypes != null && value.parameterTypes.Length > 0)
					{
						string parameterString = string.Join(", ", value.parameterTypes.Select(t => t.PrettyName()).ToArray());

						label += string.Format(" ({0})", parameterString);
					}

					selectedOption = new DropdownOption<UnityMember>(value, label);
				}
				else if (targetType == UnityObjectType.ScriptableObject)
				{
					selectedOption = new DropdownOption<UnityMember>(value, value.name);
				}
			}

			bool enabled = targetType != UnityObjectType.None;

			if (!enabled) EditorGUI.BeginDisabledGroup(true);

			EditorGUI.BeginChangeCheck();

			EditorGUI.showMixedValue = nameProperty.hasMultipleDifferentValues;

			value = DropdownGUI<UnityMember>.PopupSingle
			(
				position,
				GetAllMemberOptions,
				selectedOption,
				new DropdownOption<UnityMember>(null, string.Format("Nothing"))
			);

			EditorGUI.showMixedValue = false;

			if (EditorGUI.EndChangeCheck())
			{
				SetValue(value);
			}

			if (!enabled) EditorGUI.EndDisabledGroup();
		}

		private List<DropdownOption<UnityMember>> GetAllMemberOptions()
		{
			var options = new List<DropdownOption<UnityMember>>();

			if (targetType == UnityObjectType.GameObject)
			{
				// Check if all targets have a GameObject (none are empty).
				// If they do, display all members of the GameObject type.

				if (HasSharedGameObject())
				{
					var gameObjectOptions = GetTypeMemberOptions(typeof(GameObject));

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
					var componentOptions = GetTypeMemberOptions(componentType, componentType.Name);

					foreach (var componentOption in componentOptions)
					{
						// Prefix label and option by component type for clear distinction.

						componentOption.label = string.Format("{0}/{1}", componentType.Name, componentOption.label);

						options.Add(componentOption);
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
					options.AddRange(GetTypeMemberOptions(scriptableObjectType));
				}
			}

			return options;
		}

		#region Value

		/// <summary>
		/// Returns a member constructed from the current parameter values.
		/// </summary>
		/// <returns></returns>
		protected UnityMember GetValue()
		{
			if (hasMultipleDifferentValues ||
				string.IsNullOrEmpty(nameProperty.stringValue))
			{
				return null;
			}

			string component = componentProperty.stringValue;
			string name = nameProperty.stringValue;
			Type[] parameterTypes = UnityMemberDrawerHelper.DeserializeParameterTypes(parameterTypesProperty);

			if (component == string.Empty) component = null;
			if (name == string.Empty) name = null;
			// Cannot reliably determine if parameterTypes should be null; see other TODO note for selectedOption.

			return new UnityMember(component, name, parameterTypes);
		}

		/// <summary>
		/// Assigns the property values from a specified member.
		/// </summary>
		protected virtual void SetValue(UnityMember value)
		{
			if (value != null)
			{
				componentProperty.stringValue = value.component;
				nameProperty.stringValue = value.name;
				UnityMemberDrawerHelper.SerializeParameterTypes(parameterTypesProperty, value.parameterTypes);
			}
			else
			{
				componentProperty.stringValue = null;
				nameProperty.stringValue = null;
				parameterTypesProperty.arraySize = 0;
			}
		}

		/// <summary>
		/// Indicated whether the property has multiple different values.
		/// </summary>
		protected virtual bool hasMultipleDifferentValues
		{
			get
			{
				return
					componentProperty.hasMultipleDifferentValues ||
					nameProperty.hasMultipleDifferentValues ||
					UnityMemberDrawerHelper.ParameterTypesHasMultipleValues(parameterTypesProperty);
			}
		}

		#endregion

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

			var childrenComponents = targets.OfType<GameObject>().Select(gameObject => gameObject.GetComponents<Component>().Where(c => c != null));
			var siblingComponents = targets.OfType<Component>().Select(component => component.GetComponents<Component>().Where(c => c != null));

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
		protected virtual List<DropdownOption<UnityMember>> GetTypeMemberOptions(Type type, string component = null)
		{
			var options = type
				.GetMembers(validBindingFlags)
				.Where(member => validMemberTypes.HasFlag(member.MemberType))
				.Where(ValidateMember)
				.Select(member => GetMemberOption(member, component, member.DeclaringType != type))
				.ToList();

			if (filter.Extension)
			{
				var extensionMethods = type.GetExtensionMethods(filter.Inherited)
					.Where(ValidateMember)
					.Select(method => GetMemberOption(method, component, filter.Inherited && method.GetParameters()[0].ParameterType != type));

				options.AddRange(extensionMethods);
			}

			// Sort the options

			options.Sort((a, b) =>
			{
				var aSub = a.label.Contains("/");
				var bSub = b.label.Contains("/");

				if (aSub != bSub)
				{
					return bSub.CompareTo(aSub);
				}
				else
				{
					return a.value.name.CompareTo(b.value.name);
				}
			});

			return options;
		}

		protected DropdownOption<UnityMember> GetMemberOption(MemberInfo member, string component, bool inherited)
		{
			UnityMember value;
			string label;

			if (member is FieldInfo)
			{
				FieldInfo field = (FieldInfo)member;

				value = new UnityMember(component, field.Name);
				label = string.Format(labelTypeAfter ? "{1} : {0}" : "{0} {1}", field.FieldType.PrettyName(), field.Name);
			}
			else if (member is PropertyInfo)
			{
				PropertyInfo property = (PropertyInfo)member;

				value = new UnityMember(component, property.Name);
				label = string.Format(labelTypeAfter ? "{1} : {0}" : "{0} {1}", property.PropertyType.PrettyName(), property.Name);
			}
			else if (member is MethodInfo)
			{
				MethodInfo method = (MethodInfo)member;

				ParameterInfo[] parameters = method.GetParameters();

				value = new UnityMember(component, member.Name, parameters.Select(p => p.ParameterType).ToArray());

				string parameterString = string.Join(", ", parameters.Select(p => p.ParameterType.PrettyName()).ToArray());

				label = string.Format(labelTypeAfter ? "{1} ({2}) : {0}" : "{0} {1} ({2})", method.ReturnType.PrettyName(), member.Name, parameterString);
			}
			else
			{
				throw new UnityReflectionException();
			}

			if (inherited)
			{
				label = "(Inherited)/" + label;
			}

			return new DropdownOption<UnityMember>(value, label);
		}

		#endregion

		#region Filtering

		/// <summary>
		/// The valid BindingFlags when looking for reflected members.
		/// </summary>
		protected virtual BindingFlags validBindingFlags
		{
			get
			{
				// Build the flags from the filter attribute

				BindingFlags flags = 0;

				if (filter.Public) flags |= BindingFlags.Public;
				if (filter.NonPublic) flags |= BindingFlags.NonPublic;
				if (filter.Instance) flags |= BindingFlags.Instance;
				if (filter.Static) flags |= BindingFlags.Static;
				if (!filter.Inherited) flags |= BindingFlags.DeclaredOnly;
				if (filter.Static && filter.Inherited) flags |= BindingFlags.FlattenHierarchy;

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
				MemberTypes types = 0;

				if (filter.Fields || filter.Gettable || filter.Settable)
				{
					types |= MemberTypes.Field;
				}

				if (filter.Properties || filter.Gettable || filter.Settable)
				{
					types |= MemberTypes.Property;
				}

				if (filter.Methods || filter.Gettable)
				{
					types |= MemberTypes.Method;
				}

				return types;
			}
		}

		/// <summary>
		/// Determines whether a given MemberInfo should be included in the options.
		/// This check follows the BindingFlags and MemberTypes filtering. 
		/// </summary>
		protected virtual bool ValidateMember(MemberInfo member)
		{
			bool valid = true;

			FieldInfo field = member as FieldInfo;
			PropertyInfo property = member as PropertyInfo;
			MethodInfo method = member as MethodInfo;

			if (field != null) // Member is a field
			{
				// Validate type based on field type
				valid &= ValidateMemberType(field.FieldType);

				// Exclude constants (literal) and readonly (init) fields if
				// the filter rejects read-only fields.
				if (!filter.ReadOnly) valid &= !field.IsLiteral || !field.IsInitOnly;
			}
			else if (property != null) // Member is a property
			{
				// Validate type based on property type
				valid &= ValidateMemberType(property.PropertyType);

				// Exclude read-only and write-only properties
				if (!filter.ReadOnly || (!filter.Properties && filter.Settable)) valid &= property.CanWrite;
				if (!filter.WriteOnly || (!filter.Properties && filter.Gettable)) valid &= property.CanRead;
			}
			else if (method != null) // Member is a method
			{
				// Exclude methods without a return type
				if (!filter.Methods && filter.Gettable) valid &= method.ReturnType != typeof(void);

				// Validate type based on return type
				valid &= ValidateMemberType(method.ReturnType);

				// Exclude special compiler methods
				valid &= !method.IsSpecialName;

				// Exclude generic methods
				// TODO: Generic type (de)serialization
				valid &= !method.ContainsGenericParameters;

				// Exclude methods with parameters
				if (!filter.Parameters)
				{
					valid &= method.GetParameters().Length == 0;
				}
			}

			return valid;
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
			if (families.HasFlag(TypeFamily.Value)) validFamily |= (type.IsValueType && type != typeof(void));
			if (families.HasFlag(TypeFamily.Void)) validFamily |= type == typeof(void);

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