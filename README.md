A small C# dependency injection framework. Intended mainly for use in Unity projects, but the core could theoretically be used in other types of C# projects as well.

Consider this library pre-release quality. For a more proven alternative with more features, take a look at [Zenject](https://github.com/modesttree/Zenject).

Released under the Apache License, Version 2.0.

## Basic usage

Here is a quick overview of what you need to do to use this library. More details follow below.

0. Add mttiInject into your Unity project. I do not intend to distribute a binary or unitypackage version of this library during pre-release development, so see the *Compiling* section near the end of this README for instructions on how to build it yourself.
0. Add the `mtti.Inject.DependencyInjector` MonoBehaviour to an empty GameObject, or to your existing "main" GameObject if you have one.
0. Implement services as plain C# classes (not as MonoBehaviours or ScriptableObjects) and mark them with `mtti.Inject.ServiceAttribute`.
0. Add a method to your MonoBehaviours to receive dependencies. You can call the methods whatever you want and they don't need to be `public` either. Just mark them with `mtti.Inject.InjectAttribute`.
0. If you create new GameObjets programmatically (from prefabs with `Instantiate`, for example) inject dependencies into them manually.

### Adding the EntryPoint

Add the *Dependency Injector* component to an empty GameObject. Take care not to let multiple dependency injectors exist at the same time as the results of doing so are undefined.

### Implementing services

Services are normal C# classes marked with the Service attribute. An instance of the class is created automatically the first time it's required.

    // ExampleService.cs

    using System;
    using mtti.Inject;

    [Service]
    public class ExampleService {
        public int Sum(int a, int b) {
            return a + b;
        }
    }

### Receiving dependencies

MonoBehaviours and other services get dependencies injected directly into any field marked with the `Inject` attribute:

    // ExampleScript.cs

    using System;
    using UnityEngine;
    using mtti.Inject;

    public class ExampleScript : MonoBehaviour {
        [Inject] private ExampleService exampleService = null;
    }

Method injection is also supported:

    // ExampleScript.cs

    using System;
    using UnityEngine;
    using mtti.Inject;

    public class ExampleScript : MonoBehaviour {
        private ExampleService exampleService = null;

        [Inject]
        private OnInject(ExampleService exampleService) {
            this.exampleService = exampleService;
        }
    }

The injection method gets called after field injection even if it has no parameters so you can use it as a callback to initialize objects after they've received their dependencies:

    // ExampleScript.cs

    using System;
    using UnityEngine;
    using mtti.Inject;

    public class ExampleScript : MonoBehaviour {
        [Inject] private ExampleService exampleService = null;

        private OnInject() {
            this.gameObject.SetActive(true);
        }
    }

### Injecting manually

The *Dependency Injector* automatically injects dependencies into all GameObjects in the all loaded scenes when it starts and into all scenes that are loaded after the EntryPoint was created, but it can't magically detect when new GameObjects are created programmatically.

Therefore, you need to manually inject dependencies into new GameObjects you create. You do so using a dependency injection context, which is created automatically by *Dependency Injector* and which is actually responsible for most of the functionality of this library.

The context is itself available as a service called `UnityContext`. After you've created a new GameObject, simply call `context.Inject(newGameObject)` to inject dependencies into it.

    // ExampleSpawner.cs

    public class ExampleSpawner : MonoBehaviour {
        public GameObject enemyPrefab = null;

        [Inject] private UnityContext context = null;
        [Inject] private ExampleService exampleService = null;

        public void SpawnNewEnemy() {
            var obj = (GameObject)Instantiate(this.enemyPrefab);
            this.context.Inject(obj);
            return obj;
        }
    }

## Advanced usage

### Services can require other services

Services can use the `[Inject]` attribute on one of their methods to require other services. Be aware, though, that circular dependencies are not supported and will fail throw a `DependencyInjectionException`.

### Binding with interfaces

In the basic example, ExampleService was marked with `[Service]` with no parameters. This binds the service using its concrete class, ExampleService. You can however bind a service with an interface that it implements by passing the interface type as a parameter to the Service attribute.

This makes it possible to create mock services for unit testing.

ExampleService could be written as:

    // ExampleService.cs

    using System;
    using mtti.Inject;

    public interface IExampleService {
        int Sum(int a, int b);
    }

    [Service(typeof(IExampleService))]
    public class ExampleService {
        public int Sum(int a, int b) {
            return a + b;
        }
    }

After which it can be required using the interface:

    // ExampleScript.cs

    using System;
    using UnityEngine;
    using mtti.Inject;

    public class ExampleScript : MonoBehaviour {
        private IExampleService exampleService = null;

        [Inject]
        private void OnInject(IExampleService exampleService) {
            this.exampleService = exampleService;
        }
    }

### Static factory methods

Normally a service instance is created using a parametreless default constructor, but if you want a lazily initialized service while still having some more control over how it's initialized, you can use a static factory method.

    [Service]
    private static IExampleService ExampleServiceFactory() {
        return new ExampleService();
    }

One situation where you might want to do this is if you want to have a generic service. C# doesn't allow generic types in attribute parameters so you can't do `[Service(typeof(MyGenericService<string>))]`, for example.

Another use case is when you want a service to exist as a GameObject in your Unity scene so you can see it in the hierarchy and inspector.

    using UnityEngine;
    using mtti.Inject;

    public class MyService : MonoBehaviour {
        [Service]
        private static MyService FindMyService() {
            return (MyService)UnityEngine.Object.FindObjectOfType(typeof(MyService));
        }

        public int Sum(int a, int b) {
            return a + b;
        }
    }

### Update a service every frame

Implement the `mtti.Inject.IUpdate` interface in your service and its `OnUpdate()` method will get called every frame, just like Unity's `Update()`.

    // ExampleService.cs

    using System;
    using mtti.Inject;

    [Service]
    public class ExampleService : IUpdate {
        public void OnUpdate() {
            // Called every frame
        }
    }

### Binding manually

You can bind dependencies manually to a context instance. For example, you could do `context.Bind<IExampleService>(new ExampleService());`.

### Creating contexts manually

You can create instances of mtti.Inject.Context (the base class) and mtti.Inject.UnityContext (Unity-specific subclass) normally, for example when writing unit tests.

## Development

This project is intended to be built on the command line and does not come with any solution or project files.

### Compiling

0. You need bash to run the build script, including on Windows.
0. The Mono `mcs` compiler needs to be in PATH.
0. Copy `UnityEngine.dll` and `UnityEditor.dll` from your Unity installation into the dependencies directory in this repository.
0. Run `build.sh`.
0. A `dist` directory will be created and will contain a `mtti` directory which you can copy into your Unity project.
0. In Unity, adjust the import settings for `dist/mtti/Inject/UnityDll` and `dist/mtti/Inject/UnityEditorDll` so that they're included only outside the editor and only inside the editor, respectively.

### Running tests

0. The Mono executable `mono` needs to be in PATH.
0. Copy `nunitlite.dll`, `nunit.framework.dll` and `NUnit.System.Linq.dll` into the `dependencies` directory.
0. Run `test.sh`.
