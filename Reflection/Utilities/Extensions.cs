using System;
using System.Collections.Generic;
using System.Linq;

namespace Ludiq.Reflection
{
	public static class Extensions
	{
		/// <summary>
		/// Finds the intersection of a group of groups.
		/// </summary>
		public static IEnumerable<T> IntersectAll<T>(this IEnumerable<IEnumerable<T>> groups)
		{
			HashSet<T> hashSet = null;

			foreach (IEnumerable<T> group in groups)
			{
				if (hashSet == null)
				{
					hashSet = new HashSet<T>(group);
				}
				else
				{
					hashSet.IntersectWith(group);
				}
			}

			return hashSet == null ? Enumerable.Empty<T>() : hashSet.AsEnumerable();
		}

		/// <summary>
		/// Determines if an enum has the given flag defined bitwise.
		/// Fallback equivalent to .NET's Enum.HasFlag().
		/// </summary>
		public static bool HasFlag(this Enum value, Enum flag)
		{
			long lValue = Convert.ToInt64(value);
			long lFlag = Convert.ToInt64(flag);
			return (lValue & lFlag) != 0;
		}
	}
}