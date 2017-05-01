## Currently unmaintained

This repository is no longer maintained. I am working on a much more robust Unity reflection framework which will entirely replace it.

# Ludiq.Reflection

A set of Unity classes and their inspector drawers that provide easy reflection and decoupling.

This editor extension provides 2 new classes, `UnityMember` and `AnimatorParameter`, along with their custom drawers, that behave like Unity's built-in `UnityEvent` for fields, properties, methods and animator parameters reflection.

With them, you can easily refer to members of `Unity.Object` classes directly in the inspector and use them in scripting later. This allows for quick prototyping and decoupling for complex games and applications (albeit at a small performance cost).

### Say what?

It's easier to explain it with pictures:

![Steps](http://i.imgur.com/Tltom7f.png)

Inspector view:

![Inspector](http://i.imgur.com/DANkdON.png)

(Note: These screenshots are outdated; `UnityMethod` and `UnityVariable` are now unified in `UnityMember`)

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
2. Add `using Ludiq.Reflection;` to your namespaces
2. Add a `UnityMember` as a public member
3. Add a `[Filter]` attribute to choose which members are shown
4. Set your bindings in the inspector
5. Access the variables and methods directly from your script!

Here's a simple example that will display the value of any method and any variable on start:
```csharp
using UnityEngine;
using Ludiq.Reflection;

public class BasicExample : MonoBehaviour
{
	[Filter(Methods = true)]
	public UnityMember method;

	[Filter(Fields = true, Properties = true)]
	public UnityMember variable;

	void Start()
	{
		Debug.LogFormat("Method return: {0}", method.Invoke());
		Debug.LogFormat("Variable value: {0}", variable.Get());
	}
}
```

### Basic Usage

##### UnityMember.Get

```csharp
object UnityMember.Get()
T UnityMember.Get<T>()
```

Retrieves the value of the variable.

##### UnityMember.Set

```csharp
void UnityMember.Set(object value)
```

Assigns a new value to the variable.

##### UnityMember.Invoke

```csharp
object UnityMember.Invoke(params object[] arguments)
T UnityMember.Invoke<T>(params object[] arguments)
```

Invokes the method with any number of arguments of any type and returns its return value, or null if there isn't any (void).

##### UnityMember.GetOrInvoke

```csharp
object UnityMember.GetOrInvoke(params object[] arguments)
T UnityMember.GetOrInvoke<T>(params object[] arguments)
```

If the member is a field or property, retrieves its value. If the member is a method, invokes it with any number of arguments of any type and returns its return value, or null if there isn't any (void).

This method is usually combined with `[Filter(Gettable = true)]`.

##### UnityMember.InvokeOrSet

```csharp
object UnityMember.InvokeOrSet(params object[] argumentsOrValue)
T UnityMember.InvokeOrSet<T>(params object[] argumentsOrValue)
```

If the member is a method, invokes it with any number of arguments of any type and returns its return value, or null if there isn't any (void). If the member is a field or property, sets its value to the first argument and returns null.

This method is usually combined with `[Filter(Methods = true, Settable = true)]`.

##### UnityMember.type

```csharp
Type UnityMember.type { get; }
```

The type of the reflected field or property or return type of the reflected method.

---

#### Assignment Check

Unfortunately, the Unity inspector doesn't allow for `null` values to be assigned to properties. If you want to know if a `UnityMember` has been properly assigned (e.g. not "Nothing" in the inspector), use the `isAssigned` indicator:

```csharp
using UnityEngine;
using Ludiq.Reflection;

public class AssignmentExample : MonoBehaviour
{
	[Filter(Fields = true, Properties = true)]
	public UnityMember variable;

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
	[SelfTargeted, Filter(Methods = true)]
	public UnityMember selfMethod;
}
```

#### Member Filtering

You can specify which members will appear in the inspector using the `Filter` attribute. You can combine a number of options to display only the members you want. For example:

```csharp
using UnityEngine;
using Ludiq.Reflection;

public class AdvancedExample : MonoBehaviour
{
    // Only show fields of type Transform
    [Filter(typeof(Transform), Fields = true)]
    public UnityMember transformField;

    // Only show methods that return an integer or a float
    [Filter(typeof(int), typeof(float), Methods = true)]
    public UnityMember numericMethod;

    // Only show methods that return primitives or enums
    [Filter(Methods = true, TypeFamily = TypeFamily.Primitive | TypeFamily.Enum)]
    public UnityMember primitiveOrEnumMethod;

	// Only show static methods
	[Filter(Methods = true, Static = true, Instance = false)]
	public UnityMember staticMethod;

	// Include non-public properties
    [Filter(Properties = true, NonPublic = true)]
	public UnityMember hiddenProperty;

    // Exclude readonly properties
    [Filter(Properties = true, ReadOnly = false)]
    public UnityMember writableProperty;

    // Include methods that are defined in the object's hierarchy
    [Filter(Methods = true, Inherited = true)]
    public UnityMember definedMethod;

    // Combine any of the above options
    [Filter(typeof(Collider), Fields = true, Static = true, ReadOnly = false)]
    public UnityMember colliderVariable;
}
```

You should always enable at least one of the following options:

Option		| Description
------------|------------
Fields		|Display fields
Properties	|Display properties
Methods		|Display methods
Gettable	|Display fields, properties with a getter and methods with a return type
Settable	|Display fields and properties with a setter

The additional optional options are:

Option		| Description | Default
------------|-------------|--------
Inherited	|Display members defined in the types's ancestors|false
Instance	|Display instance members|true
Static		|Display static members|false
Public		|Display public members|true
NonPublic	|Display private and protected members|false
ReadOnly	|Display read-only properties and fields|true
WriteOnly	|Display write-only properties and fields|true
Extension	|Display extension methods|true
Parameters	|Display methods with parameters|true
TypeFamily	|Determines which member type families are displayed|TypeFamily.All
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

#### Changing Defaults

You can change the filter defaults by editing the the `FilterAttribute` constructor in `Reflection/FilterAttribute.cs`.

#### Type Labeling Format

You can tell the inspector to label the member options as `{Name} : {Type}` instead of `{Type} {Name}` by adding the `[LabelTypeAfter]` attribute. For example:

```csharp
using UnityEngine;
using Ludiq.Reflection;

public class AdvancedExample : MonoBehaviour
{
	[Filter(Methods = true), LabelTypeAfter]
	public UnityMember method;
}
```

#### Creating from script

You can create `UnityMember`s directly from script, if you need to:

```csharp
using UnityEngine;
using Ludiq.Reflection;

public class ScriptExample : MonoBehaviour
{
	[Filter(Fields = true, Properties = true)]
	public UnityMember inspectorVariable;

	void Start()
	{
		// Print the transform's position
		var variable = new UnityMember("Transform", "position", gameObject);
		Debug.Log(variable.Get());

		// Call SetActive directly on the GameObject
		var method = new UnityMember("SetActive", gameObject);
		method.Invoke(false);

		// Modify a variable assigned from the inspector
		inspectorVariable = new UnityMember("Transform", "Rotation", inspectorVariable.target);
		Debug.Log(inspectorVariable.Get());
	}
}
```

### Direct Access

If you want to directly access the `System.Reflection` objects, you can do so using the following properties. Note that you must previously have reflected the member, either manually via `UnityMember.Reflect()`, or automatically by accessing / invoking it.

##### UnityMember.fieldInfo

```csharp
FieldInfo UnityMember.fieldInfo { get; }
```

The underlying reflected field, or null if the variable is a property or a method.

##### UnityMember.propertyInfo

```csharp
PropertyInfo UnityMember.propertyInfo { get; }
```

The underlying reflected property, or null if the getter is a field or a method.

##### UnityMember.methodInfo

```csharp
MethodInfo UnityMember.methodInfo { get; }
```

The underlying reflected method, or null if the getter is a field or a property.

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
