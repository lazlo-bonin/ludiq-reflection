namespace UnityEngine.Reflection
{
	/// <summary>
	/// An option in an editor popup field.
	/// </summary>
	/// <typeparam name="T">The type of the backing value.</typeparam>
	public class PopupOption<T>
	{
		/// <summary>
		/// The backing value of the option.
		/// </summary>
		public T value;

		/// <summary>
		/// The visible label of the option.
		/// </summary>
		public string label;

		/// <summary>
		/// Initializes a new instance of the PopupOption class with the specified value.
		/// </summary>
		public PopupOption(T value)
		{
			this.value = value;
			this.label = value.ToString();
		}

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

			return Equals((PopupOption<T>)otherObject);
		}

		public bool Equals(PopupOption<T> other)
		{
			if (other == null)
			{
				return Object.ReferenceEquals(other.value, null);
			}

			return value.Equals(other.value);
		}

		public override int GetHashCode()
		{
			return value.GetHashCode();
		}

		public static implicit operator T(PopupOption<T> option)
		{
			return option.value;
		}
	}
}