using System;
using System.IO;

public class SlideHTMLGenerator {

    public static string errorStr = null;
    public static bool Generate(string imgFolderPath)
    {
        errorStr = null;
        string htmlStr = GetHTMLHeader();
        try
        {
            string[] filePaths = Directory.GetFiles(imgFolderPath);
            foreach (string fileP in filePaths)
            {
                int splitIdx = Math.Max(fileP.LastIndexOf('/'), fileP.LastIndexOf('\\'));
                string fileName = fileP.Substring(splitIdx+1);
                htmlStr += "        <li><img src=\"images/" + fileName + "\" alt=\"\"></li>\n";
            }
            htmlStr += GetHTMLFooter();
            string pathToSaveFile = "index.html";
            GameGUI.Inst.WriteToConsoleLog("Writing html to " + pathToSaveFile);
#if !UNITY_WEBPLAYER
            File.WriteAllText(pathToSaveFile, htmlStr);
#endif
        }
        catch (Exception e)
        {
            errorStr = e.ToString();
            return false;
        }
        return true;
    }

    private static string GetHTMLHeader()
    {
        return @"
        <!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width,initial-scale=1'>
  <link rel='stylesheet' href='js/responsiveslides.css'>
  <link rel='stylesheet' href='style.css'>
  <script src='http://ajax.googleapis.com/ajax/libs/jquery/1.8.3/jquery.min.js'></script>
  <script src='js/hashchange.js'></script>
  <script src='js/responsiveslides.min.js'></script> <!-- now depends on hashchange.js -->

  <script>
    // You can also use '$(window).load(function() {'
    $(function () {

      // Slideshow 4
      $('#slider').responsiveSlides({
        auto: false,
        pager: false,
        nav: true,
        speed: 200,
        namespace: 'callbacks',
        before: function (idx) {
          window.location.hash = 'slide-'+ idx + ''; 
        },
      });
    
    // when the page loads, we need to trigger a hashchange
    $(window).trigger( 'hashchange' );

    });
  </script>
</head>
<body>
  <div id='wrapper'>

    <!-- Slideshow -->
    <div class='callbacks_container'>
      <ul class='rslides' id='slider'>";



    }

    private static string GetHTMLFooter()
    {
        return @"</ul>
    </div>
  </div>
</body>
</html>";

    }
}
