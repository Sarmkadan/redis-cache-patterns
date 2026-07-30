public class CacheKeyExtensions
    {
        public static string BuildKey(string key, params string[] parts)
        {
            return string.Join(":", parts);
        }
    }