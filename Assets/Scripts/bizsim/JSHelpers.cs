using UnityEngine;
using Awesomium.Mono;
using System.Collections;
using System.Collections.Generic;

public class JSHelpers{

    public static void HitDismissForGoogleDocOutOfDateWarning(CollabBrowserTexture bTex)
    {
        // click dismiss button for out of date browser
        string cmd = "var elemList = document.getElementsByClassName(\"docs-butterbar-link\"); if (elemList != null && elemList.length > 0 && elemList[0].parentNode != null && (elemList[0].parentNode.textContent.indexOf(\"out of date\") != -1 || elemList[0].parentNode.textContent.indexOf(\"supported\") != -1) && elemList[0].parentNode.parentNode != null) { elemList[0].parentNode.parentNode.removeChild(elemList[0].parentNode); }";
        bTex.ExecuteJavaScript(cmd);
    }

    public static void MinimizeGoogleDocMenus(CollabBrowserTexture bTex)
    {
        string cmd = "var elem = document.getElementById(\"viewModeButton\"); if( elem != null ){var attr = elem.getAttribute(\"data-tooltip\"); if(attr != null && attr.indexOf(\"Hide\") != -1){elem.getBoundingClientRect();}}";
        JSValue rect = bTex.ExecuteJavaScriptWithResult(cmd);
        if (rect != null && rect.Type != JSValueType.Null)
            bTex.ClickOnTopCenterOfObject(rect.GetObject());
    }

    public static void AutoPopulateGoogleForm(CollabBrowserTexture bTex, Dictionary<string, string> labelToValue)
    {
        string cmd = "";

        // build javascript associative map
        cmd += "var vals = " + DictionaryToJSON(labelToValue);
        cmd += @"var elems = document.getElementsByClassName(""ss-q-short"");
                for(var i = 0; i < elems.length; ++i){ 
	                var label = elems[i].getAttribute(""aria-label"").trim(); 
	                if( vals.hasOwnProperty(label) )
	                {
		                elems[i].value = vals[label];
	                }
                }";
        bTex.ExecuteJavaScript(cmd);
    }

    public static void EnterTwiddlaUserName(CollabBrowserTexture bTex, string name)
    {
        string cmd = "var gn = document.getElementById(\"guestName\"); if(gn != null){gn.value = \"" + name + "\";gn.focus();}";
        bTex.ExecuteJavaScript(cmd);
    }
    public static void CloseTwiddlaSideNav(CollabBrowserTexture bTex)
    {
        string cmd = "if(slideNavSingleton != null){slideNavSingleton.slideClosed();}";
        bTex.ExecuteJavaScript(cmd);
    }


    public static string DictionaryToJSON<K,V>(Dictionary<K, V> dict)
    {
        string jsonStr = "";
        foreach (KeyValuePair<K, V> dictVal in dict)
        {
            jsonStr += "\"" + dictVal.Key.ToString() + "\":\"" + dictVal.Value.ToString() + "\",";
        }
        if (jsonStr != "")
            jsonStr = "{" + jsonStr.Remove(jsonStr.Length - 1) + "}"; // remove the last comma, put in brackets
        return jsonStr;
    }
    
    
}
