/*
Copyright 2017 mtti

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

    public class FakeService : IFakeService, IUpdate
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

    public class FieldInjectReceiver
    {
        public IFakeService PrivateFakeService
        {
            get
            {
                return this.privateFakeService;
            }
        }

        [Inject]
        public IFakeService publicFakeService;

        [Inject]
        public IAnotherFakeService publicAnotherFakeService;

        [Inject]
        private IFakeService privateFakeService = null;
    }

    public class MethodInjectReceiver
    {
        public IFakeService fakeService;

        public IAnotherFakeService anotherFakeService;

        [Inject]
        private void Inject(IFakeService fakeService, IAnotherFakeService anotherFakeService)
        {
            this.fakeService = fakeService;
            this.anotherFakeService = anotherFakeService;
        }
    }

    [TestFixture]
    public class ContextTests
    {
        private Context context;

        private FakeService fakeService;

        private AnotherFakeService anotherFakeService;

        [SetUp]
        public void Init()
        {
            this.fakeService = new FakeService();
            this.anotherFakeService = new AnotherFakeService();

            this.context = new Context();
            this.context.Bind<IFakeService>(this.fakeService);
            this.context.Bind<IAnotherFakeService>(this.anotherFakeService);
        }

        [Test]
        public void BindAndGet()
        {
            Assert.AreSame(this.fakeService, this.context.Get<IFakeService>());
        }

        [Test]
        public void InjectFields()
        {
            var receiver = new FieldInjectReceiver();
            this.context.Inject(receiver);

            Assert.AreSame(this.fakeService, receiver.publicFakeService);
            Assert.AreSame(this.anotherFakeService, receiver.publicAnotherFakeService);
            Assert.AreSame(this.fakeService, receiver.PrivateFakeService);
        }

        [Test]
        public void InjectMethod()
        {
            var receiver = new MethodInjectReceiver();
            this.context.Inject(receiver);

            Assert.AreSame(this.fakeService, receiver.fakeService);
            Assert.AreSame(this.anotherFakeService, receiver.anotherFakeService);
        }

        [Test]
        public void ThrowOnUnmetDependency()
        {
            var receiver = new MethodInjectReceiver();
            var context = new Context();
            context.Bind<IFakeService>(this.fakeService);
            Assert.Throws<DependencyInjectionException>(() => { context.Inject(receiver); });
        }

        [Test]
        public void CallOnUpdate()
        {
            this.context.OnUpdate();
            Assert.AreEqual(1, this.fakeService.OnUpdateCallCount);
        }
    }
}
