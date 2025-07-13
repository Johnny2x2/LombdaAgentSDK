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

    public static class AsyncHelpers
    {
        //Checks if the type is a generic Task<T> and returns the type of T if it is.
        public static bool IsGenericTask(Type type, out Type taskResultType)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    taskResultType = type;//type.GetGenericArguments()[0];
                    return true;
                }

                type = type.BaseType!;
            }

            taskResultType = null;
            return false;
        }
    }
}
