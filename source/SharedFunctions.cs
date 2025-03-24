namespace Collections
{
    internal static class SharedFunctions
    {
        public static int GetHashCode<T>(T value) where T : unmanaged
        {
            return value.GetHashCode() & 0x7FFFFFFF;
        }
    }
}