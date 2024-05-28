using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Extension
{
    public static class SpanCharExtensions
    {
        public static ReadOnlySpan<char> SubLeftWith(this ReadOnlySpan<char> span, char[] chars, out ReadOnlySpan<char> item)
        {
            item = default(ReadOnlySpan<char>);
            int num = span.IndexOf(chars);
            if (num > 0)
            {
                item = span.Slice(0, num);
                return span.Slice(num + chars.Length);
            }

            return span;
        }

        public static ReadOnlySpan<char> SubRightWith(this ReadOnlySpan<char> span, char[] chars, out ReadOnlySpan<char> item)
        {
            item = default(ReadOnlySpan<char>);
            int num = span.LastIndexOf(chars);
            if (num > 0)
            {
                item = span.Slice(num + chars.Length);
                return span.Slice(0, num);
            }

            return span;
        }

        public static ReadOnlySpan<char> SubLeftWith(this ReadOnlySpan<char> span, char spitChar, out ReadOnlySpan<char> item)
        {
            item = default(ReadOnlySpan<char>);
            int num = span.IndexOf(spitChar);
            if (num > 0)
            {
                item = span.Slice(0, num);
                return span.Slice(num + 1);
            }

            return span;
        }

        public static ReadOnlySpan<char> SubRightWith(this ReadOnlySpan<char> span, char spitChar, out ReadOnlySpan<char> item)
        {
            item = default(ReadOnlySpan<char>);
            int num = span.LastIndexOf(spitChar);
            if (num > 0)
            {
                item = span.Slice(num + 1);
                return span.Slice(0, num);
            }

            return span;
        }

        public static ReadOnlySpan<char> SubLeftWith(this Span<char> span, char[] chars, out ReadOnlySpan<char> item)
        {
            item = default(ReadOnlySpan<char>);
            int num = span.IndexOf(chars);
            if (num > 0)
            {
                item = span.Slice(0, num);
                return span.Slice(num + chars.Length);
            }

            return span;
        }

        public static ReadOnlySpan<char> SubRightWith(this Span<char> span, char[] chars, out ReadOnlySpan<char> item)
        {
            item = default(ReadOnlySpan<char>);
            int num = span.LastIndexOf(chars);
            if (num > 0)
            {
                item = span.Slice(num + chars.Length);
                return span.Slice(0, num);
            }

            return span;
        }

        public static ReadOnlySpan<char> SubLeftWith(this Span<char> span, char spitChar, out ReadOnlySpan<char> item)
        {
            item = default(ReadOnlySpan<char>);
            int num = span.IndexOf(spitChar);
            if (num > 0)
            {
                item = span.Slice(0, num);
                return span.Slice(num + 1);
            }

            return span;
        }

        public static ReadOnlySpan<char> SubRightWith(this Span<char> span, char spitChar, out ReadOnlySpan<char> item)
        {
            item = default(ReadOnlySpan<char>);
            int num = span.LastIndexOf(spitChar);
            if (num > 0)
            {
                item = span.Slice(num + 1);
                return span.Slice(0, num);
            }

            return span;
        }

        public static string SubLeftWith(this string span, char[] chars, out string item)
        {
            item = null;
            int num = span.IndexOfAny(chars);
            if (num > 0)
            {
                item = span.Substring(0, num);
                return span.Substring(num + chars.Length);
            }

            return span;
        }

        public static string SubRightWith(this string span, char[] chars, out string item)
        {
            item = null;
            int num = span.LastIndexOfAny(chars);
            if (num > 0)
            {
                item = span.Substring(num + chars.Length);
                return span.Substring(0, num);
            }

            return span;
        }

        public static string SubLeftWith(this string span, char spitChar, out string item)
        {
            item = null;
            int num = span.IndexOf(spitChar);
            if (num > 0)
            {
                item = span.Substring(0, num);
                return span.Substring(num + 1);
            }

            return span;
        }

        public static string SubRightWith(this string span, char spitChar, out string item)
        {
            item = null;
            int num = span.LastIndexOf(spitChar);
            if (num > 0)
            {
                item = span.Substring(num + 1);
                return span.Substring(0, num);
            }

            return span;
        }
    }
}
