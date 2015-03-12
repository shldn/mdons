
public class RedirectHelper {

    public static string HandleYouTube(string requestedUrl)
    {
        int videoIdIdx = requestedUrl.IndexOf("v=");
        if (videoIdIdx == -1)
            return requestedUrl;

        string videoStr = requestedUrl.Substring(videoIdIdx);
        return "http://player.virbela.com/index.html?player=youtube&" + videoStr;
    }

    public static string HandleVimeo(string requestedUrl)
    {
        int videoIdIdx = requestedUrl.LastIndexOf('/');
        int videoID = -1;
        if (videoIdIdx != -1 && int.TryParse(requestedUrl.Substring(videoIdIdx + 1), out videoID))
            return "http://player.virbela.com/index.html?player=vimeo&v=" + videoID;
        return requestedUrl;
    }
}
