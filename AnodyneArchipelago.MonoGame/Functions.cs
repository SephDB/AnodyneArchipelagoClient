﻿using TextCopy;

namespace AnodyneArchipelago
{
    internal class Functions
    {
        public static string GetClipboard()
        {
            return ClipboardService.GetText() ?? "";
        }
    }
}
