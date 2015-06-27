using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEngine.Reflection
{
	/// <summary>
	/// Utility class to display complex editor popups.
	/// </summary>
	public static class PopupGUI<T>
	{
		public delegate void PopupCallback(T value);

		/// <summary>
		/// Render an editor popup and return the newly selected option.
		/// </summary>
		public static void Render
		(
			PopupCallback callback, 
			Rect position, 
			IEnumerable<PopupOption<T>> options, 
			PopupOption<T> selectedOption, 
			bool hasMultipleDifferentValues, 
			string noneLabel,
			bool enabled = true
		)
		{
			bool showNoneOption = noneLabel != null;
			bool hasOptions = options.Any();

			if (!enabled)
			{
				EditorGUI.BeginDisabledGroup(true);
			}
			
			string label;
			
			if (hasMultipleDifferentValues)
			{
				label = "—";
			}
			else if (selectedOption == null)
			{
				label = noneLabel;
			}
			else
			{
				label = selectedOption.label;
			}

			if (GUI.Button(position, label, EditorStyles.popup))
			{
				GenericMenu menu = new GenericMenu();
				GenericMenu.MenuFunction2 menuCallback = (o) => callback((T)o);

				if (showNoneOption)
				{
					menu.AddItem(new GUIContent(noneLabel), false, menuCallback, null);
				}

				if (showNoneOption && hasOptions)
				{
					menu.AddSeparator("");
				}

				foreach (var option in options)
				{
					menu.AddItem(new GUIContent(option.label), false, menuCallback, option.value);
				}

				Vector2 menuPosition = new Vector2(position.xMin, position.yMax);

				menu.DropDown(new Rect(menuPosition, Vector2.zero));
			}
			else if (selectedOption != null && !options.Select(o => o.value).Contains(selectedOption.value))
			{
				// Selected option isn't in the range
				callback(default(T));
			}

			if (!enabled)
			{
				EditorGUI.EndDisabledGroup();
			}
		}
	}
}