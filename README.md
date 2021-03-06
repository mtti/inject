**mtti.Inject** is a simple dependency injector for the Unity engine.

Released under the Apache License, Version 2.0.

## Basic usage

1. Add this repository into your Unity project as a submodule: `git submodule add git@github.com:mtti/inject.git Assets/mtti/Inject`.
2. Add the `mtti.Inject.DependencyInjector` MonoBehaviour to an empty GameObject, or to your existing "main" GameObject if you have one.
3. Implement services as plain C# classes (not as MonoBehaviours or ScriptableObjects) and mark them with `mtti.Inject.ServiceAttribute`.
4. Add a method to your MonoBehaviours to receive dependencies. You can call the methods whatever you want and they don't need to be `public` either. Just mark them with `mtti.Inject.InjectAttribute`.
5. If you create new GameObjets programmatically (from prefabs with `Instantiate`, for example) inject dependencies into them manually.

### Adding the dependency injector component

Add the *Dependency Injector* component to an empty GameObject. Take care not to let multiple dependency injectors exist at the same time as the results of doing so are undefined.

### Implementing services

Services are normal C# classes marked with the Service attribute. An instance of the class is created automatically the first time it's required.

```csharp
// ExampleService.cs

using System;
using mtti.Inject;

[Service]
public class ExampleService
{
    public int Sum(int a, int b)
    {
        return a + b;
    }
}
```

### Receiving dependencies

MonoBehaviours and other services get dependencies injected directly into any field marked with the `Inject` attribute:

```csharp
// ExampleScript.cs

using System;
using UnityEngine;
using mtti.Inject;

public class ExampleScript : MonoBehaviour
{
    [Inject]
    private ExampleService _exampleService;
}
```

Method injection is also supported:

```csharp
// ExampleScript.cs

using System;
using UnityEngine;
using mtti.Inject;

public class ExampleScript : MonoBehaviour
{
    private ExampleService _exampleService;

    [Inject]
    private OnInject(ExampleService exampleService)
    {
        _exampleService = exampleService;
    }
}
```

The injection method gets called after field injection even if it has no parameters so you can use it as a callback to initialize objects after they've received their dependencies:

```csharp
// ExampleScript.cs

using System;
using UnityEngine;
using mtti.Inject;

public class ExampleScript : MonoBehaviour
{
    [Inject]
    private ExampleService _exampleService;

    [Inject]
    private OnInject()
    {
        this.gameObject.SetActive(true);
    }
}
```

### Optional dependencies

Normally, the `[Inject]` attribute throws an exception when the dependency is unmet. If this is undesirable, `[InjectOptional]` instead leaves the value of the field untouched and doesn't throw an exeception.

```csharp
// ExampleScript.cs

using System;
using UnityEngine;
using mtti.Inject;

public class ExampleScript : MonoBehaviour
{
    [InjectOptional]
    private ExampleService _exampleService;

    [Inject]
    private OnInject()
    {
        if (_exampleService != null)
        {
            this.gameObject.SetActive(true);
        }
    }
}
```

### Injecting manually

The *Dependency Injector* automatically injects dependencies into all GameObjects in the all loaded scenes when it starts and into all scenes that are loaded after the EntryPoint was created, but it can't magically detect when new GameObjects are created programmatically.

Therefore, you need to manually inject dependencies into new GameObjects you create. You do so using an instance of the Injector class. In a Unity project, one is created automatically by the *Dependency Injector* component.

The injector is itself available as a service called `UnityInjector`. After you've created a new GameObject, simply call `injector.Inject(newGameObject)` to inject dependencies into it.

```csharp
// ExampleSpawner.cs

public class ExampleSpawner : MonoBehaviour
{
    public GameObject EnemyPrefab;

    [Inject]
    private UnityInjector _injector;

    [Inject]
    private ExampleService _exampleService;

    public void SpawnNewEnemy()
    {
        var obj = (GameObject)Instantiate(EnemyPrefab);
        _injector.Inject(obj);
        return obj;
    }
}
```

## Unity component helpers

mtti.Inject includes some Unity-specific helper attributes which don't have anything to do with dependency injection directly, but should help with reducing repetitive boilerplate code when finding references to GameObject components.

### GetComponentAttribute

`[GetComponent]` mirrors the functionality of [UnityEngine.Component.GetComponent](https://docs.unity3d.com/ScriptReference/Component.GetComponent.html).

Instead of doing this:

```csharp
// ExampleScript.cs

using UnityEngine;
using mtti.Inject;

public class ExampleScript : MonoBehaviour
{
    private MeshCollider _meshCollider;

    [Inject]
    private void OnInject()
    {
        _meshCollider = GetComponent<MeshCollider>();
    }
}
```

You can do this:

```csharp
// ExampleScript.cs

using UnityEngine;
using mtti.Inject;

public class ExampleScript : MonoBehaviour
{
    [GetComponent]
    private MeshCollider _meshCollider;
}
```

### GetComponentInChildrenAttribute

`[GetComponentInChildren]` mirrors the functionality of [UnityEngine.Component.GetComponentInChildren](https://docs.unity3d.com/ScriptReference/Component.GetComponentInChildren.html).

Instead of doing this:

```csharp
// ExampleScript.cs

using UnityEngine;
using mtti.Inject;

public class ExampleScript : MonoBehaviour
{
    private MeshCollider _meshCollider;

    [Inject]
    private void OnInject()
    {
        _meshCollider = GetComponentInChildren<MeshCollider>();
    }
}
```

You can do this:

```csharp
// ExampleScript.cs

using UnityEngine;
using mtti.Inject;

public class ExampleScript : MonoBehaviour
{
    [GetComponentInChildren]
    private MeshCollider _meshCollider;
}
```

### EnsureComponentAttribute

`[EnsureComponent]` is the same as GetComponent, except it adds the component if it doesn't already exist. Instead of doing this:

```csharp
// ExampleScript.cs

using UnityEngine;
using mtti.Inject;

public class ExampleScript : MonoBehaviour
{
    private MeshCollider _meshCollider;

    [Inject]
    private void OnInject()
    {
        _meshCollider = GetComponent<MeshCollider>();
        if (_meshCollider == null)
        {
            _meshCollider = this.gameObject.AddComponent<MeshCollider>();
        }
    }
}
```

You can do this:

```csharp
// ExampleScript.cs

using UnityEngine;
using mtti.Inject;

public class ExampleScript : MonoBehaviour
{
    [EnsureComponent]
    private MeshCollider _meshCollider;
}
```

## Advanced usage

### Services can require other services

Services can use the `[Inject]` attribute on one of their methods to require other services. Be aware, though, that circular dependencies are not supported and will fail throw a `DependencyInjectionException`.

### Binding with interfaces

In the basic example, ExampleService was marked with `[Service]` with no parameters. This binds the service using its concrete class, ExampleService. You can however bind a service with an interface that it implements by passing the interface type as a parameter to the Service attribute.

This makes it possible to create mock services for unit testing.

ExampleService could be written as:

```csharp
// ExampleService.cs

using System;
using mtti.Inject;

public interface IExampleService
{
    int Sum(int a, int b);
}

[Service(typeof(IExampleService))]
public class ExampleService
{
    public int Sum(int a, int b)
    {
        return a + b;
    }
}
```

After which it can be injected using the interface:

```csharp
// ExampleScript.cs

using System;
using UnityEngine;
using mtti.Inject;

public class ExampleScript : MonoBehaviour
{
    private IExampleService _exampleService;

    [Inject]
    private void OnInject(IExampleService exampleService)
    {
        _exampleService = exampleService;
    }
}
```

### Static factory methods

Normally a service instance is created using a parametreless default constructor, but if you want a lazily initialized service while still having some more control over how it's initialized, you can use a static factory method.

```csharp
[Service]
private static IExampleService ExampleServiceFactory()
{
    return new ExampleService();
}
```

One situation where you might want to do this is if you want to have a generic service. C# doesn't allow generic types in attribute parameters so you can't do `[Service(typeof(MyGenericService<string>))]`, for example.

Another use case is when you want a service to exist as a GameObject in your Unity scene so you can see it in the hierarchy and inspector.

```csharp
using UnityEngine;
using mtti.Inject;

public class MyService : MonoBehaviour
{
    [Service]
    private static MyService FindMyService()
    {
        return (MyService)UnityEngine.Object.FindObjectOfType(typeof(MyService));
    }

    public int Sum(int a, int b)
    {
        return a + b;
    }
}
```

### Update a service every frame

Implement the `mtti.Inject.IUpdateReceiver` interface in your service and its `OnUpdate()` method will get called every frame, just like Unity's `Update()`.

```csharp
// ExampleService.cs

using System;
using mtti.Inject;

[Service]
public class ExampleService : IUpdateReceiver
{
    public void OnUpdate()
    {
        // Called every frame
    }
}
```

### Binding manually

You can bind dependencies manually to an injector instance. For example, you could do `injector.Bind<IExampleService>(new ExampleService());`.

### Creating injectors manually

You can create instances of mtti.Inject.Injector (the base class) and mtti.Inject.UnityInjector (Unity-specific subclass) normally, for example when writing unit tests.
