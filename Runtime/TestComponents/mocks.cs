#if UNITY_EDITOR

namespace mtti.Inject
{
    public interface IFakeService
    {
        int Sum(int a, int b);
    }

    public class FakeService : IFakeService
    {
        public int Sum(int a, int b)
        {
            return a + b;
        }
    }
}

#endif
