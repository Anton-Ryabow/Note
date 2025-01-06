using Microsoft.Extensions.Primitives;

namespace Notes
{
    public static class Extensions
    {
        public static string GetHashPass(this string pass)
        {
            int code = 0;

            char[] chars = pass.ToCharArray();

            foreach (char c in chars)
            {
                code += c.GetHashCode();
            }

            return code.ToString();
        }

        public static string GetHashPass(this StringValues pass)
        {
            int code = 0;

            char[] chars = pass.ToString().ToCharArray();

            foreach (char c in chars)
            {
                code += c.GetHashCode();
            }

            return code.ToString();
        }
    }
}
