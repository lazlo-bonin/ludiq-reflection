using System;

namespace UnityEngine.Reflection
{
	[Flags]
	public enum TypeFamily
	{
		None = 0,
		All = ~0,
		Value = 1,
		Reference = 2,
		Primitive = 4,
		Array = 8,
		Class = 16,
		Enum = 32,
		Interface = 64,
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public sealed class ReflectionAttribute : Attribute
	{
		public bool Inherited { get; set; }
		public bool Instance { get; set; }
		public bool Static { get; set; }
		public bool Public { get; set; }
		public bool NonPublic { get; set; }
		public bool ReadOnly { get; set; }
		public bool WriteOnly { get; set; }
		public TypeFamily TypeFamilies { get; set; }

		private readonly Type[] types;
		public Type[] Types
		{
			get { return types; }
		}

		public ReflectionAttribute(params Type[] types)
		{
			this.types = types;

			Inherited = true;
			Instance = true;
			Static = false;
			Public = true;
			NonPublic = false;
			ReadOnly = true;
			WriteOnly = true;
			TypeFamilies = TypeFamily.All;
		}
	}
}