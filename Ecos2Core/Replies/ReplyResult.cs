﻿/*
 * MIT License
 *
 * Copyright (c) 2017 Dr. Christian Benjamin Ries
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
namespace Ecos2Core.Replies
{
    public class ReplyResult
    {
        public int ErrorCode { get; private set; }
        public string ErrorMessage { get; private set; }

        public void Parse(string line)
        {
            line = line.Replace("<END ", "");
            line = line.Trim().TrimEnd('\r', '\n', '>');

            int n = line.IndexOf(' ');
            if (n == -1)
            {
                ErrorCode = 0;
                ErrorMessage = "OK";
                return;
            }

            var firstPart = line.Substring(0, n).Trim();
            int v;
            if (int.TryParse(firstPart, out v))
                ErrorCode = v;
            else
                ErrorCode = 0;

            var lastPart = line.Substring(n + 1).Trim();
            lastPart = lastPart.TrimStart('(').TrimEnd(')');
            if (!string.IsNullOrEmpty(lastPart))
                ErrorMessage = line.TrimStart('(').TrimEnd(')');
            else
                ErrorMessage = lastPart;
        }
    }
}
