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
using NUnit.Framework;

namespace mtti.Inject
{
    public interface IFakeService
    {
        int Sum(int a, int b);
    }

    public class FakeService : IFakeService, IUpdateReceiver
    {
        public int OnUpdateCallCount = 0;

        public int Sum(int a, int b)
        {
            return a + b;
        }

        public void OnUpdate()
        {
            OnUpdateCallCount++;
        }
    }

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
        public void ThrowOnUnmetDependency()
        {
            var receiver = new MethodInjectReceiver();
            var injector = new Injector();
            injector.Bind<IFakeService>(_fakeService);
            Assert.Throws<DependencyInjectionException>(() => { injector.Inject(receiver); });
        }

        [Test]
        public void CallOnUpdate()
        {
            _injector.OnUpdate();
            Assert.AreEqual(1, _fakeService.OnUpdateCallCount);
        }
    }
}
