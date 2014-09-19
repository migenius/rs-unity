using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.migenius.rs4.core
{
    public class RSUtils
    {
        public static string RandomString()
        {
            int size = 8;
            bool lowerCase = true;

            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            if (lowerCase)
                return builder.ToString().ToLower();
            return builder.ToString();
        }

        public static string GetUserScope()
        {
            return "user_scope_" + RandomString();
        }
    }
}
