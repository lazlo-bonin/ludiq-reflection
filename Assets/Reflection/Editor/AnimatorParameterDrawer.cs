using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;

namespace UnityEngine.Reflection
{
	[CustomPropertyDrawer(typeof(AnimatorParameter))]
	public class AnimatorParameterDrawer : TargetedDrawer
	{
		#region Fields

		/// <summary>
		/// The inspected property, of type AnimatorParameter.
		/// </summary>
		protected SerializedProperty property;

		/// <summary>
		/// The UnityMember.name of the inspected property, of type string.
		/// </summary>
		protected SerializedProperty nameProperty;

		/// <summary>
		/// The targeted animators.
		/// </summary>
		protected Animator[] targets;

		#endregion

		/// <inheritdoc />
		protected override void Update(SerializedProperty property)
		{
			// Update the targeted drawer
			base.Update(property);

			// Assign the property and sub-properties
			this.property = property;
			nameProperty = property.FindPropertyRelative("_name");

			// Find the targets
			targets = FindTargets();
		}

		/// <inheritdoc />
		protected override void RenderMemberControl(Rect position)
		{
			var options = GetNameOptions();

			PopupOption<string> selectedOption = null;
			
			if (!string.IsNullOrEmpty(nameProperty.stringValue))
			{
				selectedOption = new PopupOption<string>(nameProperty.stringValue);
			}

			// Make sure the callback uses the property of this drawer, not at its later value.
			var propertyNow = property;

			PopupGUI<string>.Render
			(
				value => UpdateMember(propertyNow, value), 
				position, options, 
				selectedOption, 
				nameProperty.hasMultipleDifferentValues, 
				"No Parameter",
				targets.Any(target => target != null)
			);
		}

		protected void UpdateMember(SerializedProperty property, string value)
		{
			nameProperty.stringValue = value;

			property.serializedObject.ApplyModifiedProperties();
		}

		/// <summary>
		/// Get the list of targets on the inspected objects.
		/// </summary>
		protected Animator[] FindTargets()
		{
			IEnumerable<Object> objects;

			if (isSelfTargeted)
			{
				// In self targeting mode, the objects are the inspected objects themselves.

				objects = property.serializedObject.targetObjects;
			}
			else
			{
				// In manual targeting mode, the targets the values of each target property.

				objects = targetProperty.Multiple().Select(p => p.objectReferenceValue);
			}

			var childrenAnimators = objects.OfType<GameObject>().SelectMany(gameObject => gameObject.GetComponents<Animator>());
			var siblingAnimators = objects.OfType<Component>().SelectMany(component => component.GetComponents<Animator>());

			return childrenAnimators.Concat(siblingAnimators).ToArray();
		}

		/// <summary>
		/// Gets the list of shared parameter names as popup options.
		/// </summary>
		protected List<PopupOption<string>> GetNameOptions()
		{
			var options = new List<PopupOption<string>>();

			List<string> names = targets
				.Select(animator => ((AnimatorController)animator.runtimeAnimatorController))
				.Where(animatorController => animatorController != null)
				.Select(animatorController => animatorController.parameters)
				.Select(parameters => parameters.Select(parameter => parameter.name))
				.IntersectAll()
				.Distinct()
				.ToList();

			foreach (string name in names)
			{
				options.Add(new PopupOption<string>(name));
			}

			// Alphabetic sort: 
			// options.Sort((o1, o2) => o1.value.CompareTo(o2.value));

			return options;
		}
	}
}