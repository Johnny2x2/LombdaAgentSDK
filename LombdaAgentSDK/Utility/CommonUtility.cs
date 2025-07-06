namespace LombdaAgentSDK
{
    public static class EnumHelper
    {
        //String helper to parse an enum value from a string input.
        public static bool TryParseEnum<T>(this string input, out T? output)
        {
            try
            {
                Enum.TryParse(typeof(T), input, true, out object? result);
                output = (T?)result;
                return result != null;
            }
            catch (Exception)
            {
                output = default;
                return false;
            }
        }
    }
}
