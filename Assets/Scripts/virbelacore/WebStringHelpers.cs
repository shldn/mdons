using UnityEngine;
using System.Collections;
using System.Text;

public class WebStringHelpers {

    public static string AppendHTTP(string url)
    {
        if (url.Length > 1 && (url.Length <= 4 || url.Substring(0, 4) != "http"))
            url = "http://" + url;
        return url;
    }

    public static string CreateValidURLOrSearch(string url)
    {
        if (string.IsNullOrEmpty(url) || (url.Length >= 4 && url.Substring(0, 4) == "http"))
            return url;

        // spaces are not allowed in urls, assume search term.
        if (url.IndexOf(' ') != -1 || url.IndexOf('.') == -1)
        {
            string[] tok = url.Split(' ');
            url = "https://www.google.com/search?q=";
            url += tok[0];
            for(int i=1; i < tok.Length; ++i)
                url += "+" + tok[i];
            return url;
        }

        return AppendHTTP(url);
    }

    public static string HtmlEncode(string text)
    {
        if (text == null)
            return null;

        StringBuilder sb = new StringBuilder(text.Length);

        int len = text.Length;
        for (int i = 0; i < len; i++)
        {
            switch (text[i])
            {
                case '<':
                    sb.Append("&lt;");
                    break;
                case '>':
                    sb.Append("&gt;");
                    break;
                case '"':
                    sb.Append("&quot;");
                    break;
                case '\'':
                    sb.Append("&#39;");
                    break;
                case '&':
                    sb.Append("&amp;");
                    break;
                default:
                    if (text[i] > 159)
                    {
                        // decimal numeric entity
                        sb.Append("&#");
                        sb.Append(((int)text[i]).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        sb.Append(";");
                    }
                    else
                        sb.Append(text[i]);
                    break;
            }
        }
        return sb.ToString();
    }
}
