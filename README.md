# UnityEngine.Reflection

A set of Unity classes and their inspector drawers that provide easy reflection and decoupling.

This editor extension provides 2 new classes, `UnityVariable` and `UnityMethod`, along with their custom drawers, that behave like Unity's built-in `UnityEvent` for fields, properties and methods reflection.

With them, you can easily refer to members of `Unity.Object` classes directly in the inspector and use them in scripting later. This allows for quick prototyping and decoupling for complex games and applications (albeit at a small performance cost).

### Say what?

Ugh. Here's a picture:

![Explanation](http://i.imgur.com/Tltom7f.png)

### Features

- Inspect fields, properties or methods
- Serialized for persistency across reloads
- Works on GameObjects and ScriptableObjects
- Multi-object editing
- Undo / Redo support
- Looks and feels like a built-in Unity component

### Limitations

- Doesn't support method overloading (yet)

## Installation

Import the `Assets/Reflection` folder in your project and you're good to go!

# Usage

1. Create a behaviour script
2. Add `using UnityEngine.Reflection;` to your namespaces.
2. Use `UnityVariable` or `UnityMethod` as public behaviour members
3. Set your bindings in the inspector
4. Access the variables directly from your script!

Here's a simple example that will display the value of any method and any variable on start:
```csharp
using UnityEngine;
using UnityEngine.Reflection;

public class BasicExample : MonoBehaviour
{
	public UnityMethod method;

	public UnityVariable variable;

	void Start()
	{
		Debug.LogFormat("Method return: {0}", method.Invoke());
		Debug.LogFormat("Variable value: {0}", variable.Get());
	}
}
```

## Basic Usage
There are 3 commonly used methods to deal with reflected members. Each are described below.

`Get` and `Invoke` also have typed generic equivalents that will attempt a cast.

#### UnityVariable.Get

```csharp
object UnityVariable.Get()
```

Retrieves the value of the variable. The typed version attemps a cast to T.

#### UnityVariable.Set

```csharp
object UnityVariable.Set(object value)
```

Assigns a new value to the variable.

#### UnityMethod.Invoke

```csharp
object UnityMethod.Invoke(params object[] args)
```

Invokes the method with any number of arguments of any type and returns its return value, or null if there isn't (void).

## Advanced Usage

You can specify which members will appear in the inspector using the `Reflection` attribute. You can combine a number of attributes to display only the members you want. For example:

```csharp
using UnityEngine;
using UnityEngine.Reflection;

public class AdvancedExample : MonoBehaviour
{
    // Only show variables of type Transform
    [Reflection(typeof(Transform))]
    public UnityVariable transformVariable;

    // Only show methods that return an integer or a float
    [Reflection(typeof(int), typeof(float))]
    public UnityMethod numericMethod;

    // Only show methods that return primitives or enums
    [Reflection(TypeFamilies = TypeFamily.Primitive | TypeFamily.Enum)]
    public UnityMethod primitiveOrEnumMethod;

	// Only show static methods
	[Reflection(Static = true, Instance = false)]
	public UnityMethod staticMethod;

	// Include non-public variables
    [Reflection(NonPublic = true)]
	public UnityVariable hiddenVariable;

    // Exclude readonly properties
    [Reflection(ReadOnly = false)]
    public UnityVariable writableVariable;

    // Only show methods that are on the defined on the object itself
    [Reflection(Inherited = false)]
    public UnityMethod definedMethod;

    // Combine any of the above options
    [Reflection(typeof(Collider), Static = true, ReadOnly = false)]
    public UnityVariable colliderVariable;
}
```

The available options are:

Option		| Description | Default
------------|-------------|--------
Inherited	|Display members defined in the types's ancestors|true
Instance	|Display instance members|true
Static		|Display static members|false
Public		|Display public members|true
NonPublic	|Display private and protected members|false
ReadOnly	|Display read-only properties and fields|true
WriteOnly	|Display write-only properties and fields|true
TypeFamilies|Determines which member type families are displayed|TypeFamily.All
Types		|Determines which member types are displayed|*(Any)*


The `TypeFamilies` enumeration is a [bitwise flag set](http://stackoverflow.com/questions/8447/what-does-the-flags-enum-attribute-mean-in-c) with the following options:

Flag		|Description
------------|-----------
*None*		|No type allowed
*All*		|Any type allowed
Value		|Value types only
Reference	|Reference types only
Primitive	|Primitive types only
Array		|Arrays only
Enum		|Enumerations only
Class		|Classes only
Interface	|Interfaces only

You can combine them with the bitwise or operator:

```csharp
TypeFamily enumsOrInterfaces = TypeFamily.Enum | TypeFamily.Interface;
```

### Overriding defaults

You can override the defaults by editing the inspector drawer classes and modifying the `DefaultReflectionAttribute()` method.

- For variables: `Reflection/Editor/UnityVariableDrawer.cs`
- For methods: `Reflection/Editor/UnityMethodDrawer.cs`

For example, if you wanted to make non-public variables show up by default (without having to specify it with a `Reflection` attribute), you could add the following line:

```csharp
using System.Reflection;
using UnityEditor;

namespace UnityEngine.Reflection
{
	[CustomPropertyDrawer(typeof(UnityVariable))]
	public class UnityVariableDrawer : UnityMemberDrawer
	{
		protected override ReflectionAttribute DefaultReflectionAttribute()
		{
			ReflectionAttribute reflection = base.DefaultReflectionAttribute();

			// Override defaults here
            reflection.NonPublic = true;

			return reflection;
		}

        ...
    }
}
```

# Contributing

I'll happily accept pull requests if you have improvements or fixes to suggest.

### To-do

- Method overloading (requires method signature distinction and serialization)
- Figure out a way to make the member dropdown less verbose (like `UnityEvent`'s)
- Document the source

#  License

The whole source is under MIT License, which basically means you can freely use and redistribute it in your commercial and non-commercial projects. See [the license file](LICENSE) for the boring details.

If you use it in a plugin that you redistribute, please the namespaces to avoid version conflicts with your users. For example, change `UnityEngine.Reflection` to `MyPlugin.UnityEngine.Reflection`.
