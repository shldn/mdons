using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

/*
 * COPYRIGHT BAKNO GAMES 2012
 * ALL RIGHTS RESERVED
 *
 * This is a wrapper class that allowa the interaction with the native panels to load / save files
 * as well as setting warning messages to users.
 *
 * WARNING: Windows users should NEVER use these dialogs unside Unity Editor 3.5. It will cause an immediate
 * 			crash due to the tight behavior inside the Editor. However, the standalone application works prfectly
 * 			without crashes, so it is recommended to test this in Windows standalone player. Mac Users have no
 * 			issues to this date
*/

public class NativePanels
{
	public static bool isFinished;
	
	public const int OK_PRESSED = 1;
	public const int CANCEL_PRESSED = 2;
	public const int YES_PRESSED = 3;
	public const int NO_PRESSED = 4;

#if UNITY_EDITOR || UNITY_STANDALONE
    [DllImport("NativePanels")]
    private static extern void ShowOpenFileDialog(StringBuilder fName, string filter);
    
    [DllImport("NativePanels")]
    private static extern void ShowSaveFileDialog(StringBuilder fName, string defaultFileName, string filter);
	
	[DllImport("NativePanels")]
	private static extern int ShowWarningMessage(string strMessage, int mesType);
	
	[DllImport("NativePanels")]
	private static extern int ShowMessage(string strMessage, string strTitle, int mesType);
#endif
	
	public enum MessageButtons
	{
		OK,
		OKCancel,
		YesNo,
		YesNoCancel
	}
	
	/*
	 * Method that create a warning messagebox with the message and some of the standard button configurations
	*/
	public static int SetWarningMessage(string strMessage, MessageButtons mesType)
	{
#if UNITY_EDITOR || UNITY_STANDALONE
		return ShowWarningMessage(strMessage, (int)mesType);
#else
        return 0;
#endif
	}
	
	/*
	 * Method that create a messagebox with the message and some of the standard button configurations
	*/
	public static int SetMessageBox(string strMessage, string strTitle, MessageButtons mesType)
	{
#if UNITY_EDITOR || UNITY_STANDALONE
		return ShowMessage(strMessage, strTitle, (int)mesType);
#else
        return 0;
#endif
	}
	
	/*
	 * static method that shows an open file dialog to choose a file to open.
	 * If the extensions parameter is null or is an empty array a text file will be assumed
	 * It returns the full path of the file to be loaded internally in Unity
	*/
	public static string OpenFileDialog(string[] extensions)
	{
		bool isWindows = Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;
		isFinished = false;
		StringBuilder filePath = new StringBuilder(255);
		string extensionStr = "";
		int i = 0;
		if(isWindows)
		{
			if(extensions != null && extensions.Length > 0)
			{
				string extensions2 = "Compatible Files (";
				for(i = 0; i < extensions.Length; i++)
				{
					extensions2 += "*." + extensions[i];
					extensionStr += "*." + extensions[i];
					if(i < (extensions.Length - 1))
					{
						extensions2 += ", ";
						extensionStr += ";";
					}
					else
					{
						extensions2 += ")\0";
						extensionStr += "\0";
					}
				}
				extensionStr = extensions2 + extensionStr;
			}
			else
			{
				extensionStr = "All Files (*.*)\0*.*";
			}
		}
		else
		{
			if(extensions != null && extensions.Length > 0)
            {
                for(i = 0; i < extensions.Length; i++)
                {
                    extensionStr += extensions[i];
                    if(i < (extensions.Length - 1))
                        extensionStr += ":";
                }
            }
            else
            {
                extensionStr = "txt";
            }
		}
		isFinished = true;
#if UNITY_EDITOR || UNITY_STANDALONE
        ShowOpenFileDialog(filePath, extensionStr);
#endif
		return filePath.ToString().Trim();
	}
	
	/*
	 * static method that shows an save file dialog to choose a file to save given a default fileName to save.
	 * If the extension parameter is null or is an empty string a text file will be assumed
	 * It returns the full path of the file to be saved internally in Unity
	 *
	 * NOTE: Due to the latest releases in Carbon Framework for Mac OSX, there is a weird behavior with the save sanel
	 *		 in which you cannot edit the file name in standalone applications when called from the bundle itself.
	 * 		 so it will offer an alternative to communicate with a slight version of CocoaDialog to use the save panel
	 *		 with little risks, so remember to keep it in your Plugins folder in your standalone Application
	*/
	public static string SaveFileDialog(string defaultFileName, string extension)
	{
		bool isWindows = Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;
		isFinished = false;
		StringBuilder filePath = new StringBuilder(255);
		string fName = "";
		string extensionStr = "";
		
		if(string.IsNullOrEmpty(defaultFileName))
			defaultFileName = "Untitled";
			
		if(isWindows)
		{
			if(!string.IsNullOrEmpty(extension))
			{
				extensionStr = extension.ToUpper() + " File (*." + extension + ")\0*." + extension + "\0";
			}
			else
			{
				extensionStr = "Text File (*.txt)\0*.txt";
            }
#if UNITY_EDITOR || UNITY_STANDALONE
            ShowSaveFileDialog(filePath, defaultFileName, extensionStr);
#endif
			if(!filePath.ToString().Equals("") && !filePath.ToString().Contains("." + extension))
				filePath.Append("." + extension);
				
			fName = filePath.ToString();
		}
		else
		{
			// This uses the Bundled fileDialog but its unstable
			// if(!string.IsNullOrEmpty(extension))
            // {
                // extensionStr = extension;
            // }
            // else
            // {
                // extensionStr = "txt";
            // }
			// SaveFileDialog(filePath, defaultFileName, extensionStr);
			
			// if(!filePath.ToString().Equals("") && !filePath.ToString().Contains("." + extension))
				// filePath.Append("." + extension);
				
			// fName = filePath.ToString();
			
			// Using CocoaDialog
			string command = Application.dataPath + "/Plugins/CocoaDialog.app/Contents/MacOS/CocoaDialog"; 
			string arguments = "filesave --title \"Save File\" --text \"Save As\" --with-extensions";
			if(!string.IsNullOrEmpty(extension))
			{
				arguments += " ." + extension;
			}
			else
			{
				arguments += " .txt";
			}
			arguments += " --with-file " + defaultFileName;
#if !UNITY_WEBPLAYER
            System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo(command, arguments);
			procStartInfo.RedirectStandardOutput = true;
			procStartInfo.UseShellExecute = false;
			procStartInfo.CreateNoWindow = true;
			System.Diagnostics.Process proc = new System.Diagnostics.Process();
			proc.StartInfo = procStartInfo;
			proc.Start();
			fName = proc.StandardOutput.ReadToEnd();
#endif
            if (fName == null)
				fName = "";
		}
		isFinished = true;
        return fName.Trim();
	}
}
