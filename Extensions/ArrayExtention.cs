namespace RegisterTaskWithOutlook.Extensions
{
    public static class ArrayExtention
    {
        public static bool IsIndexOutOfRange<T>(this T[] array, int index)
        {
            return array == null || index < 0 || index >= array.Length;
        }
        public static bool IsIndexOutOfRangeOrNull<T>(this T[] array, int index)
        {
            return array == null || index < 0 || index >= array.Length || array[index] == null;
        }

        public static bool TryGetValue<T>(this T[] array, int index, out T value)
        {
            if (array.IsIndexOutOfRangeOrNull(index))
            {
                value = default;
                return false;
            }
            value = array[index];
            return true;
        }
    }
}