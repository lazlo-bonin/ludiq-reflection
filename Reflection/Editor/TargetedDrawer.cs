using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ludiq.Reflection
{
	public abstract class TargetedDrawer : PropertyDrawer
	{
		/// <summary>
		/// Whether the self-targeted attribute is defined on the inspected field.
		/// </summary>
		protected bool isSelfTargeted;

		/// <summary>
		/// The UnityMember.target of the inspected property, of type Object.
		/// </summary>
		protected SerializedProperty targetProperty;

		#region Graphical Configuration

		/// <summary>
		/// The padding between the label and the target and member controls, in vertical display.
		/// </summary>
		protected const float LabelPadding = 2;

		/// <summary>
		/// The padding between the target and member controls, in vertical display.
		/// </summary>
		protected const float InnerPadding = 5;

		/// <summary>
		/// The padding below the drawer, in vertical display.
		/// </summary>
		protected const float BottomPadding = 5;

		#endregion

		/// <summary>
		/// Initializes the members of the drawer via the specified property.
		/// </summary>
		protected virtual void Update(SerializedProperty property)
		{
			this.targetProperty = property.FindPropertyRelative("_target");

			isSelfTargeted = Attribute.IsDefined(fieldInfo, typeof(SelfTargetedAttribute));
		}

		/// <summary>
		/// Calculates the height of the drawer.
		/// </summary>
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			Update(property);

			// Double the height and add the padding for self-targeting, 
			// because we'll display the controls on another line.

			if (isSelfTargeted || string.IsNullOrEmpty(label.text))
			{
				return base.GetPropertyHeight(property, label);
			}
			else
			{
				return base.GetPropertyHeight(property, label) * 2 + LabelPadding + BottomPadding;
			}
		}

		/// <summary>
		/// Renders the drawer.
		/// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Update(property);

			EditorGUI.BeginProperty(position, label, property);

			// Hack the indent level for full control
			position = EditorGUI.IndentedRect(position);
			int oldIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			// Positioning
			// When in self targeting mode, hide the target field and display
			// the member popup as a normal field. When in manual targeting, push
			// the field down below the label.

			Rect targetPosition;
			Rect memberPosition;

			if (isSelfTargeted)
			{
				targetPosition = new Rect(0, 0, 0, 0);
				memberPosition = EditorGUI.PrefixLabel(position, label);
			}
			else
			{
				if (!string.IsNullOrEmpty(label.text))
				{
					position.height = base.GetPropertyHeight(property, label);
					position.y += EditorGUI.PrefixLabel(position, label).height + LabelPadding;
				}

				targetPosition = position;
				memberPosition = position;

				targetPosition.width *= (1f / 3f);
				targetPosition.width -= (InnerPadding / 2);

				memberPosition.width *= (2f / 3f);
				memberPosition.width -= (InnerPadding / 2);
				memberPosition.x = targetPosition.xMax + InnerPadding;
			}

			// Render controls
			RenderTargetControl(targetPosition);
			RenderMemberControl(memberPosition);

			// Restore the indent level
			EditorGUI.indentLevel = oldIndent;

			EditorGUI.EndProperty();
		}

		protected virtual void RenderTargetControl(Rect position)
		{
			// When in self targeting mode, assign the target property to the
			// target object behind the scenes. When in manual targeting mode,
			// display a standard Object property field.

			if (isSelfTargeted)
			{
				foreach (var singleTargetProperty in targetProperty.Multiple())
				{
					singleTargetProperty.objectReferenceValue = singleTargetProperty.serializedObject.targetObject;
				}
			}
			else
			{
				EditorGUI.PropertyField(position, targetProperty, GUIContent.none);
			}
		}

		protected abstract void RenderMemberControl(Rect position);
	}
}