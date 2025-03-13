namespace Collections
{
    internal static class SharedFunctions
    {
        private const int Mask = 0x7FFFFFFF;

        public static int GetHashCode<T>(T value) where T : unmanaged
        {
            int hash = value.GetHashCode();
            return hash & Mask;
        }
    }
}