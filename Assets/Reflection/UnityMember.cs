using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEngine.Reflection
{
	[Serializable]
	public abstract class UnityMember
	{
		[SerializeField]
		private Object _target;
		/// <summary>
		/// The object containing the member.
		/// </summary>
		public Object target
		{
			get { return _target; }
			set { _target = value; isTargeted = false; isReflected = false; }
		}

		[SerializeField]
		private string _component;
		/// <summary>
		/// The name of the component containing the member, if the target is a GameObject.
		/// </summary>
		public string component
		{
			get { return _component; }
			set { _component = value; isTargeted = false; isReflected = false; }
		}

		[SerializeField]
		private string _name;
		/// <summary>
		/// The name of the member.
		/// </summary>
		public string name
		{
			get { return _name; }
			set { _name = value; isReflected = false; }
		}

		/// <summary>
		/// Indicates whether the reflection target has been found and cached.
		/// </summary>
		protected bool isTargeted { get; private set; }

		/// <summary>
		/// Indicates whether the reflection data has been found and cached.
		/// </summary>
		public bool isReflected { get; protected set; }

		/// <summary>
		/// The real object on which to perform reflection.
		/// For GameObjects, this is the component of type <see cref="UnityMember.component"/> or the object itself if null.
		/// For ScriptableObjects, this is the object itself. 
		/// Other Unity Objects are not supported.
		/// </summary>
		protected Object realTarget { get; private set; }

		/// <summary>
		/// Gathers and caches the reflection target for the member.
		/// </summary>
		protected void Target()
		{
			if (target == null)
			{
				throw new NullReferenceException("Target cannot be null.");
			}

			GameObject targetAsGameObject = target as GameObject;
			Component targetAsComponent = target as Component;

			if (targetAsGameObject != null || targetAsComponent != null)
			{
				if (!string.IsNullOrEmpty(component))
				{
					Component componentObject;

					if (targetAsGameObject != null)
					{
						componentObject = targetAsGameObject.GetComponent(component);
					}
					else // if (targetAsComponent != null)
					{
						componentObject = targetAsComponent.GetComponent(component);
					}

					if (componentObject == null)
					{
						throw new UnityException(string.Format("Target object does not contain a component of type '{0}'.", component));
					}

					realTarget = componentObject;
					return;
				}
				else
				{
					if (targetAsGameObject != null)
					{
						realTarget = targetAsGameObject;
					}
					else // if (targetAsComponent != null)
					{
						realTarget = targetAsComponent.gameObject;
					}

					return;
				}
			}

			ScriptableObject scriptableObject = target as ScriptableObject;

			if (scriptableObject != null)
			{
				realTarget = scriptableObject;
				return;
			}

			throw new UnityException("Target should be a GameObject, a Component or a ScriptableObject.");
		}

		/// <summary>
		/// Gathers and caches the reflection data for the member.
		/// </summary>
		public abstract void Reflect();

		/// <summary>
		/// Gathers the reflection data if it is not present.
		/// </summary>
		protected void EnsureReflected()
		{
			if (!isReflected)
			{
				Reflect();
			}
		}

		/// <summary>
		/// Gathers the reflection target if it is not present.
		/// </summary>
		protected void EnsureTargeted()
		{
			if (!isTargeted)
			{
				Target();
			}
		}
	}
}