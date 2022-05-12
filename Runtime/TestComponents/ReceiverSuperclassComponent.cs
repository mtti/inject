#if UNITY_EDITOR

using UnityEngine;

namespace mtti.Inject
{
    public abstract class ReceiverSuperclassComponent<T> : MonoBehaviour
    {
        [Inject]
        protected IFakeService _superService;

        public IFakeService SuperService { get { return _superService; } }

        public bool SuperMethodCalled = false;

        [Inject]
        protected void OnInjectSuper()
        {
            SuperMethodCalled = true;
        }
    }
}

#endif
