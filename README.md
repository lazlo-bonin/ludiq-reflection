# Ludiq.Reflection

A set of Unity classes and their inspector drawers that provide easy reflection and decoupling.

This editor extension provides 3 new classes, `UnityVariable`, `UnityMethod` and `AnimatorParameter`, along with their custom drawers, that behave like Unity's built-in `UnityEvent` for fields, properties, methods and animator parameters reflection.

With them, you can easily refer to members of `Unity.Object` classes directly in the inspector and use them in scripting later. This allows for quick prototyping and decoupling for complex games and applications (albeit at a small performance cost).

### Say what?

It's easier to explain it with pictures:

![Steps](http://i.imgur.com/Tltom7f.png)

Inspector view:

![Inspector](http://i.imgur.com/DANkdON.png)

### Features

- Inspect fields, properties and methods
- Inspect animator parameters
- Serialized for persistency across reloads
- Works on GameObjects and ScriptableObjects
- Multi-object editing
- Undo / Redo support
- Looks and feels like a built-in Unity component

### Limitations

- Doesn't support open-constructed generic methods (yet)

## Installation

1. Import [Ludiq.Controls](https://github.com/lazlo-bonin/unity-controls/) in your project
2. Import the `Reflection` folder in your project

You're good to go!

## Usage

1. Create a behaviour script
2. Add `using Ludiq.Reflection;` to your namespaces.
2. Add a `UnityVariable` or `UnityMethod` as a public member
3. Set your bindings in the inspector
4. Access the variables and methods directly from your script!

Here's a simple example that will display the value of any method and any variable on start:
```csharp
using UnityEngine;
using Ludiq.Reflection;

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

### Basic Usage
There are 3 commonly used methods to deal with reflected members. Each are described below.

`Get` and `Invoke` also have typed generic equivalents that will attempt a cast.

##### UnityVariable.Get

```csharp
object UnityVariable.Get()
```

Retrieves the value of the variable.

##### UnityVariable.Set

```csharp
void UnityVariable.Set(object value)
```

Assigns a new value to the variable.

##### UnityMethod.Invoke

```csharp
object UnityMethod.Invoke(params object[] args)
```

Invokes the method with any number of arguments of any type and returns its return value, or null if there isn't any (void).

---

You can also get the type of the reflected member using the following shortcuts:

##### UnityVariable.type

```csharp
Type UnityVariable.type { get; }
```

The field or property type of the reflected variable.

##### UnityMethod.returnType

```csharp
Type UnityMethod.returnType { get; }
```

The return type of the reflected method.

---

#### Assignment Check

Unfortunately, the Unity inspector doesn't allow for `null` values to be assigned to properties. If you want to know if a `UnityMember` has been properly assigned (e.g. not "No Variable" or "No Method" in the inspector), use the `isAssigned` indicator:

```csharp
using UnityEngine;
using Ludiq.Reflection;

public class AssignmentExample : MonoBehaviour
{
	public UnityVariable variable;

	void Start()
	{
		// Bad:

		if (variable != null) // Will always return true
		{
			Debug.Log(variable.Get()); // Might throw an exception
		}

		// Good:

		if (variable.isAssigned)
		{
			Debug.Log(variable.Get());
		}
	}
}
```

### Advanced Usage

#### Self-Targeting

You can tell the inspector to look on the current object instead of manually specifying one by adding the `[SelfTargeted]` attribute. For example:

```csharp
using UnityEngine;
using Ludiq.Reflection;

public class AdvancedExample : MonoBehaviour
{
	[SelfTargeted]
	public UnityVariable selfVariable;
}
```

#### Member Filtering

You can specify which members will appear in the inspector using the `Filter` attribute. You can combine a number of options to display only the members you want. For example:

```csharp
using UnityEngine;
using Ludiq.Reflection;

public class AdvancedExample : MonoBehaviour
{
    // Only show variables of type Transform
    [Filter(typeof(Transform))]
    public UnityVariable transformVariable;

    // Only show methods that return an integer or a float
    [Filter(typeof(int), typeof(float))]
    public UnityMethod numericMethod;

    // Only show methods that return primitives or enums
    [Filter(TypeFamily = TypeFamily.Primitive | TypeFamily.Enum)]
    public UnityMethod primitiveOrEnumMethod;

	// Only show static methods
	[Filter(Static = true, Instance = false)]
	public UnityMethod staticMethod;

	// Include non-public variables
    [Filter(NonPublic = true)]
	public UnityVariable hiddenVariable;

    // Exclude readonly properties
    [Filter(ReadOnly = false)]
    public UnityVariable writableVariable;

    // Include methods that are defined in the object's hierarchy
    [Filter(Inherited = true)]
    public UnityMethod definedMethod;

    // Combine any of the above options
    [Filter(typeof(Collider), Static = true, ReadOnly = false)]
    public UnityVariable colliderVariable;
}
```

The available options are:

Option		| Description | Default
------------|-------------|--------
Inherited	|Display members defined in the types's ancestors|false
Instance	|Display instance members|true
Static		|Display static members|false
Public		|Display public members|true
NonPublic	|Display private and protected members|false
ReadOnly	|Display read-only properties and fields|true
WriteOnly	|Display write-only properties and fields|true
TypeFamily|Determines which member type families are displayed|TypeFamily.All
Types		|Determines which member types are displayed|*(Any)*


The `TypeFamily` enumeration is a [bitwise flag set](http://stackoverflow.com/questions/8447/what-does-the-flags-enum-attribute-mean-in-c) with the following options:

Flag		|Description
------------|-----------
*None*		|No type allowed
*All*		|Any type allowed
Value		|Value types (excl. void)
Reference	|Reference types
Primitive	|Primitive types
Array		|Arrays
Enum		|Enumerations
Class		|Classes
Interface	|Interfaces
Void        |Void

You can combine them with the bitwise or operator:

```csharp
TypeFamily enumsOrInterfaces = TypeFamily.Enum | TypeFamily.Interface;
```

#### Overriding Defaults

You can override the defaults by editing the inspector drawer classes and modifying the `DefaultFilter()` method.

- For variables: `Reflection/Editor/UnityVariableDrawer.cs`
- For methods: `Reflection/Editor/UnityMethodDrawer.cs`

For example, if you wanted to make non-public variables show up by default (without having to specify it with a `Filter` attribute), you could add the following line:

```csharp
using System.Reflection;
using UnityEditor;

namespace Ludiq.Reflection
{
	[CustomPropertyDrawer(typeof(UnityVariable))]
	public class UnityVariableDrawer : UnityMemberDrawer
	{
		...

		protected override FilterAttribute DefaultFilter()
		{
			FilterAttribute filter = base.DefaultFilter();

			// Override defaults here
			filter.Inherited = true;
            filter.NonPublic = true;

			return filter;
		}

        ...
    }
}
```

#### Creating from script

You can create `UnityMember`s directly from script, if you need to:

```csharp
using UnityEngine;
using Ludiq.Reflection;

public class ScriptExample : MonoBehaviour
{
	public UnityVariable inspectorVariable;

	void Start()
	{
		// Print the transform's position
		var variable = new UnityVariable("Transform", "position", gameObject);
		Debug.Log(variable.Get());

		// Call SetActive directly on the GameObject
		var method = new UnityMethod("SetActive", gameObject);
		method.Invoke(false);

		// Modify a variable assigned from the inspector
		inspectorVariable = new UnityVariable("Transform", "Rotation", inspectorVariable.target);
		Debug.Log(inspectorVariable.Get());
	}
}
```

### Direct Access

If you want to directly access the `System.Reflection` objects, you can do so using the following properties. Note that you must previously have reflected the member, either manually via `UnityMember.Reflect()`, or automatically by accessing / invoking it.

##### UnityVariable.fieldInfo

```csharp
FieldInfo UnityVariable.fieldInfo { get; }
```

The underlying reflected field, or null if the variable is a property.

##### UnityVariable.propertyInfo

```csharp
PropertyInfo UnityVariable.propertyInfo { get; }
```

The underlying reflected property, or null if the variable is a field.

##### UnityMethod.methodInfo

```csharp
MethodInfo UnityVariable.methodInfo { get; }
```

The underlying reflected method.

### Animator Parameters
You can "reflect" animator parameters with the `AnimatorParameter` class. It supports the `SelfTargeted` attribute, but not the `Filter` attribute. Example:

```csharp
using UnityEngine;
using Ludiq.Reflection;

public class AnimatorExample : MonoBehaviour
{
	public AnimatorParameter speedParameter;

	void Start()
	{
		speedParameter.Set(5);
	}
}
```

The following methods and properties are available:

##### AnimatorParameter.Get

```csharp
object AnimatorParameter.Get()
```

Retrieves the value of the parameter.

##### AnimatorParameter.Set

```csharp
void AnimatorParameter.Set(object value)
```

Assigns a new value to the parameter.

##### AnimatorParameter.SetTrigger

```csharp
void AnimatorParameter.SetTrigger()
```

Triggers the parameter.

##### AnimatorParameter.ResetTrigger

```csharp
void AnimatorParameter.ResetTrigger()
```

Resets the trigger on the parameter.

##### AnimatorParameter.type

```csharp
Type AnimatorParameter.type { get; }
```

The type of the parameter, or null if it is a trigger.

##### AnimatorParameter.parameterInfo

```csharp
AnimatorControllerParameter AnimatorParameter.parameterInfo { get; }
```

The underlying animator controller parameter.

## Contributing

I'll happily accept pull requests if you have improvements or fixes to suggest.

### To-do

- Open-constructed generic method support (requires generic type serialization)

##  License

The whole source is under MIT License, which basically means you can freely use and redistribute it in your commercial and non-commercial projects. See [the license file](LICENSE) for the boring details. You must keep the license file and copyright notice in copies of the library.

If you use it in a plugin that you redistribute, please change the namespace to avoid version conflicts with your users. For example, change `Ludiq.Reflection` to `MyPlugin.Reflection`.
