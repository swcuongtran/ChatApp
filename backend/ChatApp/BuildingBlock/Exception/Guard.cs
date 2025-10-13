namespace BuildingBlock.Exception
{
    public static class Guard
    {
        public static void AgainstNull<T>(T? value, string name)
        {
            if (value is null) throw new ArgumentNullException(name);
        }

        public static void AgainstNullOrWhiteSpace(string? value, string name, int maxLen = int.MaxValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{name} is required.", name);
            if (value.Length > maxLen)
                throw new ArgumentOutOfRangeException(name, $"Max length {maxLen}.");
        }

        public static void AgainstOutOfRange<T>(T value, T min, T max, string name) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
                throw new ArgumentOutOfRangeException(name, $"Range [{min}, {max}].");
        }

        public static void AgainstNegative(int value, string name)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(name, "Must be >= 0.");
        }
    }
}
