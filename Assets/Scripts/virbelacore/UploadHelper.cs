using UnityEngine;
using System.Collections;

public class UploadHelper : MonoBehaviour
{

    IEnumerator UploadFileCo(string localFileName, string uploadURL)
    {
        WWW localFile = new WWW("file:///" + localFileName);
        yield return localFile;
        if (localFile.error == null)
            VDebug.LogError("Loaded file successfully");
        else
        {
            Debug.LogError("Open file error: " + localFile.error);
            GameGUI.Inst.guiLayer.SendGuiLayerUploadComplete(localFileName, "Error Opening File: " + localFile.error);
            yield break; // stop the coroutine here
        }

        // build form
        WWWForm postForm = new WWWForm();
        string mimeType = (localFileName.EndsWith(".txt")) ? "text/plain" : "application/octet-stream";
        postForm.AddBinaryData("theFile", localFile.bytes, localFileName, mimeType);
        postForm.AddField("action", "virbela upload");
        postForm.AddField("who", CommunicationManager.CurrentUserProfile.Username);

        // upload
        WWW upload = new WWW(uploadURL, postForm);
        yield return upload;
        if (upload.error == null)
        {
            VDebug.LogError("upload done :" + upload.text);
            GameGUI.Inst.guiLayer.SendGuiLayerUploadComplete(localFileName);
        }
        else
        {
            Debug.LogError("Error during upload: " + upload.error);
            GameGUI.Inst.guiLayer.SendGuiLayerUploadComplete(localFileName, upload.error);
        }
        Destroy(this);
    }

    public void UploadFile(string localFileName, string uploadURL)
    {
        StartCoroutine(UploadFileCo(localFileName, uploadURL));
    }
}