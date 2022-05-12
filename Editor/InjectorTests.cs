/*
Copyright 2017-2022 Matti Hiltunen

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace mtti.Inject
{
    public interface IAnotherFakeService
    {
        int Minus(int a, int b);
    }

    public class AnotherFakeService : IAnotherFakeService
    {
        public int Minus(int a, int b)
        {
            return a - b;
        }
    }

    public interface INonExistentService
    {
        int Multiply(int a, int b);
    }

    public class FieldInjectReceiver
    {
        public IFakeService PrivateFakeService
        {
            get
            {
                return _privateFakeService;
            }
        }

        public IFakeService OptionalPrivateFakeService
        {
            get
            {
                return _optionalPrivateFakeService;
            }
        }

        [Inject]
        public IFakeService PublicFakeService = null;

        [Inject]
        public IAnotherFakeService PublicAnotherFakeService = null;

        [Inject]
        private IFakeService _privateFakeService = null;

        [InjectOptional]
        private IFakeService _optionalPrivateFakeService = null;
    }

    public class SingleFieldInjectReceiver
    {
        [Inject]
        private IFakeService _privateFakeService = null;

        public IFakeService PrivateFakeService
        {
            get
            {
                return _privateFakeService;
            }
        }
    }

    public class OptionalFieldInjectReceiver
    {
        [InjectOptional]
        private IFakeService _optionalPrivateFakeService = null;

        public IFakeService OptionalPrivateFakeService
        {
            get
            {
                return _optionalPrivateFakeService;
            }
        }
    }

    public class MethodInjectReceiver
    {
        public IFakeService FakeService;

        public IAnotherFakeService AnotherFakeService;

        [Inject]
        private void Inject(IFakeService fakeService, IAnotherFakeService anotherFakeService)
        {
            FakeService = fakeService;
            AnotherFakeService = anotherFakeService;
        }
    }

    public class DelegateInjectReceiver
    {
        public static List<object> ReceivedStaticParams = new List<object>();

        public static int StaticMethod(
            string message,
            int number,
            IFakeService fakeService,
            IAnotherFakeService anotherFakeService
        )
        {
            ReceivedStaticParams.Add(message);
            ReceivedStaticParams.Add(number);
            ReceivedStaticParams.Add(fakeService);
            ReceivedStaticParams.Add(anotherFakeService);
            return number;
        }

        public List<object> ReceivedInstanceParams = new List<object>();

        public int InstanceMethod(
            string message,
            int number,
            IFakeService fakeService,
            IAnotherFakeService anotherFakeService
        )
        {
            ReceivedInstanceParams.Add(message);
            ReceivedInstanceParams.Add(number);
            ReceivedInstanceParams.Add(fakeService);
            ReceivedInstanceParams.Add(anotherFakeService);
            return number;
        }
    }

    public abstract class ReceiverSuperclass
    {
        [Inject]
        protected IFakeService _superFakeService;

        public bool SuperMethodCalled = false;

        public IFakeService SuperFakeService { get { return _superFakeService; } }

        [Inject]
        public void OnInject()
        {
            SuperMethodCalled = true;
        }
    }

    public class ReceiverSubclass : ReceiverSuperclass { }

    public abstract class GenericReceiverSuperclass<T>
    {
        public T Value;

        [Inject]
        protected IFakeService _superFakeService;

        public bool SuperMethodCalled = false;

        public IFakeService SuperFakeService { get { return _superFakeService; } }

        [Inject]
        public void OnInject()
        {
            SuperMethodCalled = true;
        }
    }

    public class GenericReceiverSubclass : GenericReceiverSuperclass<int> { }

    public abstract class ReceiverAncestor
    {
        [Inject]
        protected IFakeService _ancestorService;

        public IFakeService AncestorService { get { return _ancestorService; } }

    }

    public abstract class ReceiverParent<T> : ReceiverAncestor
    {
        [Inject]
        public IFakeService ParentService;
    }

    public class ReceiverWithAncestor : ReceiverParent<int> { }

    [TestFixture]
    public class InjectorTests
    {
        private Injector _injector;

        private FakeService _fakeService;

        private AnotherFakeService _anotherFakeService;

        [SetUp]
        public void Init()
        {
            _fakeService = new FakeService();
            _anotherFakeService = new AnotherFakeService();

            _injector = new Injector();
            _injector.Bind<IFakeService>(_fakeService);
            _injector.Bind<IAnotherFakeService>(_anotherFakeService);
        }

        [Test]
        public void BindAndGet()
        {
            Assert.AreSame(_fakeService, _injector.Get<IFakeService>());
        }

        [Test]
        public void GetOptionalReturnsNullOnNonExistentService()
        {
            Assert.IsNull(_injector.GetOptional<INonExistentService>());
        }

        [Test]
        public void InjectFields()
        {
            var receiver = new FieldInjectReceiver();
            _injector.Inject(receiver);

            Assert.AreSame(_fakeService, receiver.PublicFakeService);
            Assert.AreSame(_anotherFakeService, receiver.PublicAnotherFakeService);
            Assert.AreSame(_fakeService, receiver.PrivateFakeService);
            Assert.AreSame(_fakeService, receiver.OptionalPrivateFakeService);
        }

        [Test]
        public void InjectMethod()
        {
            var receiver = new MethodInjectReceiver();
            _injector.Inject(receiver);

            Assert.AreSame(_fakeService, receiver.FakeService);
            Assert.AreSame(_anotherFakeService, receiver.AnotherFakeService);
        }

        [Test]
        public void InjectInherited()
        {
            var receiver = new ReceiverSubclass();
            _injector.Inject(receiver);

            Assert.AreSame(_fakeService, receiver.SuperFakeService);
            Assert.IsTrue(receiver.SuperMethodCalled);
        }

        [Test]
        public void InjectGenericInherited()
        {
            var receiver = new GenericReceiverSubclass();
            _injector.Inject(receiver);

            Assert.AreSame(_fakeService, receiver.SuperFakeService);
            Assert.IsTrue(receiver.SuperMethodCalled);
        }

        [Test]
        public void InjectAncestor()
        {
            var receiver = new ReceiverWithAncestor();
            _injector.Inject(receiver);

            Assert.AreSame(_fakeService, receiver.AncestorService);
            Assert.AreSame(_fakeService, receiver.ParentService);
        }

        [Test]
        public void ThrowOnUnmetDependency()
        {
            var receiver = new MethodInjectReceiver();
            var injector = new Injector();
            injector.Bind<IFakeService>(_fakeService);
            Assert.Throws<DependencyInjectionException>(() => { injector.Inject(receiver); });
        }

        [Test]
        public void SetOptionalToNullAfterUnbind()
        {
            var fakeService = new FakeService();

            var injector = new Injector();
            var receiver = new OptionalFieldInjectReceiver();

            injector.Bind<IFakeService>(fakeService);
            injector.Inject(receiver);

            Assert.AreSame(fakeService, receiver.OptionalPrivateFakeService);

            injector.Unbind<IFakeService>();
            injector.Inject(receiver);

            Assert.AreSame(null, receiver.OptionalPrivateFakeService);
        }

        [Test]
        public void ThrowAfterUnbind()
        {
            var fakeService = new FakeService();

            var injector = new Injector();
            var receiver = new SingleFieldInjectReceiver();

            injector.Bind<IFakeService>(fakeService);
            injector.Inject(receiver);

            Assert.AreSame(fakeService, receiver.PrivateFakeService);

            injector.Unbind<IFakeService>();
            Assert.Throws<DependencyInjectionException>(
                () => { injector.Inject(receiver); }
            );
        }

        [Test]
        public void TestInstanceInvoke()
        {
            var receiver = new DelegateInjectReceiver();

            var result = _injector.Invoke<int>(
                receiver,
                "InstanceMethod",
                new List<object>(new object[] { "hello", 42 })
            );

            Assert.AreEqual(42, result);
            Assert.AreEqual("hello", receiver.ReceivedInstanceParams[0]);
            Assert.AreEqual(42, receiver.ReceivedInstanceParams[1]);
            Assert.AreSame(_fakeService, receiver.ReceivedInstanceParams[2]);
            Assert.AreSame(
                _anotherFakeService,
                receiver.ReceivedInstanceParams[3]
            );
        }

        [Test]
        public void TestStaticInvoke()
        {
            var result = _injector.Invoke<int>(
                typeof(DelegateInjectReceiver),
                "StaticMethod",
                new List<object>(new object[] { "hello", 42 })
            );

            Assert.AreEqual(42, result);
            Assert.AreEqual(
                "hello",
                DelegateInjectReceiver.ReceivedStaticParams[0]
            );
            Assert.AreEqual(
                42,
                DelegateInjectReceiver.ReceivedStaticParams[1]
            );
            Assert.AreSame(
                _fakeService,
                DelegateInjectReceiver.ReceivedStaticParams[2]
            );
            Assert.AreSame(
                _anotherFakeService,
                DelegateInjectReceiver.ReceivedStaticParams[3]
            );
        }
    }
}
