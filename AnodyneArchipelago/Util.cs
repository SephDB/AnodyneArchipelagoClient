﻿using System.Globalization;
using System.Text;

namespace AnodyneArchipelago
{
    public class Util
    {
        public static string WordWrap(string input, int width)
        {
            StringBuilder sb = new();
            Queue<string> queue = new(input.Split(' '));
            int curWidth = 0;

            while (queue.Count > 0)
            {
                string word = queue.Dequeue();

                if (curWidth > 0 && curWidth + 1 + word.Length > width)
                {
                    curWidth = 0;
                    sb.Append('\n');
                }

                if (curWidth == 0)
                {
                    while (word.Length > width)
                    {
                        sb.Append(word[..(width - 1)]);
                        sb.Append('-');

                        word = word[(width - 1)..];
                    }
                }

                if (curWidth > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(word);

                curWidth += word.Length;
            }

            return sb.ToString();
        }


        public static string ToTitleCase(string s)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

            return textInfo.ToTitleCase(s.ToLower());
        }

        public static int StringToIntVal(string s)
        {
            int val = 0;

            foreach (char b in s)
            {
                val += (byte)b;
            }

            return val;
        }
    }
}
