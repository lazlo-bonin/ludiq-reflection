using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace UnityEngine.Reflection
{
	/// <summary>
	/// An option in an editor popup field.
	/// </summary>
	/// <typeparam name="T">The type of the backing value.</typeparam>
	public struct PopupOption<T> where T : IComparable
	{
		/// <summary>
		/// A separator option.
		/// </summary>
		public static readonly PopupOption<T> separator = new PopupOption<T>(default(T), "");

		/// <summary>
		/// An option representing multiple different values.
		/// </summary>
		public static PopupOption<T> multiple { get; private set; }

		static PopupOption()
		{
			// Initialize multiple options for some of the more common types.
			// The backing value should be one that is so uncommon that it theoretically
			// never gets actually used and thus becomes a unique identifier.

			PopupOption<string>.multiple = new PopupOption<string>("#__MULTIPLE", "—");
			PopupOption<int>.multiple = new PopupOption<int>(int.MinValue, "—");
			PopupOption<float>.multiple = new PopupOption<float>(float.MinValue, "—");
		}

		/// <summary>
		/// The backing value of the option.
		/// </summary>
		public T value;

		/// <summary>
		/// The visible label of the option.
		/// </summary>
		public string label;

		/// <summary>
		/// Initializes a new instance of the PopupOption class with the specified value and label.
		/// </summary>
		public PopupOption(T value, string label)
		{
			this.value = value;
			this.label = label;
		}

		// Override equality checks
		// Two options of the same type are equal if they share 
		// the same value, regardless of their labels.

		public override bool Equals(object otherObject)
		{
			if (otherObject == null || !(otherObject is PopupOption<T>))
			{
				return false;
			}

			var otherOption = (PopupOption<T>)otherObject;

			return EqualityComparer<T>.Default.Equals(value, otherOption.value);
		}

		public bool Equals(PopupOption<T> other)
		{
			return EqualityComparer<T>.Default.Equals(value, other.value);
		}

		public override int GetHashCode()
		{
			return value.GetHashCode();
		}
	}
}