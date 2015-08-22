using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RestWcfApplication.Utils
{
  public static class Extensions
  {
    public static IEnumerable<string> SplitToChunks(this string str, int maxChunkSize)
    {
      if (string.IsNullOrEmpty(str)) throw new ArgumentException("Str cannot be empty or null", str);
      if (maxChunkSize == 0) throw new ArgumentException("maxChunkSize cannot be zero");

      for (var i = 0; i < str.Length; i += maxChunkSize)
        yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
    }
  }
}