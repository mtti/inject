**mtti.Inject** is a simple dependency injector for the Unity engine.

Released under the Apache License, Version 2.0.

## Basic usage

1. Add mttiInject into your Unity project. I do not intend to distribute a binary or unitypackage version of this library during pre-release development, so see the *Compiling* section near the end of this README for instructions on how to build it yourself.
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

Implement the `mtti.Inject.IUpdate` interface in your service and its `OnUpdate()` method will get called every frame, just like Unity's `Update()`.

```csharp
// ExampleService.cs

using System;
using mtti.Inject;

[Service]
public class ExampleService : IUpdate
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

## Development

This project is intended to be built on the command line and does not come with any solution or project files.

### Compiling

1. You need bash to run the build script, including on Windows.
2. The Mono `mcs` compiler needs to be in PATH.
3. Copy `UnityEngine.dll` and `UnityEditor.dll` from your Unity installation into the dependencies directory in this repository.
4. Run `build.sh`.
5. A `dist` directory will be created and will contain a `mtti` directory which you can copy into your Unity project.
6. In Unity, adjust the import settings for `dist/mtti/Inject/UnityDll` and `dist/mtti/Inject/UnityEditorDll` so that they're included only outside the editor and only inside the editor, respectively.

### Running tests

1. The Mono executable `mono` needs to be in PATH.
2. Copy `nunitlite.dll`, `nunit.framework.dll` and `NUnit.System.Linq.dll` into the `dependencies` directory.
3. Run `test.sh`.
