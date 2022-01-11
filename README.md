[![Made with Unity](https://img.shields.io/badge/Made%20with-Unity-333.svg?style=flat-square&logo=unity)](https://unity.com/) [![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg?style=flat-square)](https://github.com/mtti/inject/blob/master/LICENSE)

**mtti.Inject** is a simple dependency injector for the Unity engine.

Originally written for my personal use because I wanted to better understand how dependency injection frameworks work. Now resuming development in 2022 after a two-year hiatus. For your own projects you should consider a more established framework like [Zenject](https://github.com/modesttree/Zenject).

## Basic usage

1. No UPM package is currently available, so you have to add this repository into your Unity project as a package: `git submodule add git@github.com:mtti/inject.git Packages/com.mattihiltunen.inject`.
2. Add the `mtti.Inject.DependencyInjector` MonoBehaviour to an empty GameObject, or to your existing "main" GameObject if you have one.
3. Implement services as plain C# classes (not as MonoBehaviours or ScriptableObjects) and mark them with `mtti.Inject.ServiceAttribute`.
4. Add a method to your MonoBehaviours to receive dependencies. You can call the methods whatever you want and they don't need to be `public` either. Just mark them with `mtti.Inject.InjectAttribute`.
5. If you create new GameObjects programmatically (from prefabs with `Instantiate`, for example) inject dependencies into them manually.

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

MonoBehaviours and other services get dependencies injected directly into any fields marked with the `[Inject]` attribute:

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

Therefore, you need to manually inject dependencies into new GameObjects you create. You do so using the overloaded `Inject` method of the `Injector` or `UnityInjector` classes.

## Special values

### Boolean value

Any `bool` field with the `[Inject]` attribute is always injected the value `true`. You can use this to easily check if dependency injection has taken place.

```csharp
// ExampleScript.cs

public class ExampleScript : MonoBehaviour
{
    [Inject]
    private bool _wasInjected;

    private void Update()
    {
        if (!_wasInjected_) return;

        // ...
    }
}
```

### UnityInjector

When using UnityInjector, the injector itself can be injected as a dependency.

```csharp
// ExampleSpawner.cs

public class ExampleSpawner : MonoBehaviour
{
    public GameObject EnemyPrefab;

    [Inject]
    private UnityInjector _injector;

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

### Binding manually

You can bind dependencies manually to an injector instance. For example, you could do `injector.Bind<IExampleService>(new ExampleService());`.

### Creating injectors manually

You can create instances of mtti.Inject.Injector (the base class) and mtti.Inject.UnityInjector (Unity-specific subclass) normally, for example when writing unit tests or just to have full control over when and how dependencies are injected.
