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

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using IEnumerator = System.Collections.IEnumerator;

namespace mtti.Inject
{
    [TestFixture]
    public class UnityInjectorTests
    {
        private UnityInjector _injector;

        private FakeService _fakeService;

        [SetUp]
        public void Init()
        {
            _fakeService = new FakeService();
            _injector = new UnityInjector();
            _injector.Bind<IFakeService>(_fakeService);
        }

        [UnityTest]
        public IEnumerator InjectSuperclass()
        {
            var parent = new GameObject("parent");
            var obj = new GameObject("main");
            obj.transform.SetParent(parent.transform);

            var component = obj.AddComponent<ReceiverSubclassComponent>();

            _injector.Inject(parent);

            Assert.AreSame(
                _fakeService,
                component.SuperService,
                "Injects dependency into child component's superclass field"
            );
            Assert.IsTrue(
                component.SuperMethodCalled,
                "Calls OnInject method on child component's superclass"
            );

            UnityEngine.Object.DestroyImmediate(parent);

            yield return null;
        }
    }
}
