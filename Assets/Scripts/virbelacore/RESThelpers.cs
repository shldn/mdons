using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Security;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Runtime.Serialization;

//public enum CertificateProblem : long
//{
//        CertEXPIRED                   = 0x800B0101,
//        CertVALIDITYPERIODNESTING     = 0x800B0102,
//        CertROLE                      = 0x800B0103,
//        CertPATHLENCONST              = 0x800B0104,
//        CertCRITICAL                  = 0x800B0105,
//        CertPURPOSE                   = 0x800B0106,
//        CertISSUERCHAINING            = 0x800B0107,
//        CertMALFORMED                 = 0x800B0108,
//        CertUNTRUSTEDROOT             = 0x800B0109,
//        CertCHAINING                  = 0x800B010A,
//        CertREVOKED                   = 0x800B010C,
//        CertUNTRUSTEDTESTROOT         = 0x800B010D,
//        CertREVOCATION_FAILURE        = 0x800B010E,
//        CertCN_NO_MATCH               = 0x800B010F,
//        CertWRONG_USAGE               = 0x800B0110,
//        CertUNTRUSTEDCA               = 0x800B0112
//}

//public class RESThelperICertificatePolicy : ICertificatePolicy
//{
//    private string GetProblemMessage(CertificateProblem Problem)
//    {
//        string ProblemMessage = "Unknown Certificate Problem.";
//        CertificateProblem problemList = new CertificateProblem();
//        string ProblemCodeName = Enum.GetName(problemList.GetType(),Problem);
//        if(ProblemCodeName != null)
//           ProblemMessage = "Certificate problem:" + ProblemCodeName;
//        return ProblemMessage;
//     }
	
//    public bool CheckValidationResult (ServicePoint sp,X509Certificate certificate, WebRequest request, int problem)
//    {
//        if (problem == 0)
//            return true;
//        Debug.LogError("Certificate Problem with accessing " + request.RequestUri);
//        Debug.LogError(String.Format("Certificate Problem code: 0x{0:X8},",(int)problem));
//        Debug.LogError(GetProblemMessage((CertificateProblem)problem));
//        //always honoring the certificate regardless of problems
//        return true;
//    }
//}

public class RESThelper {
	
	public string baseURL = "";
	private Dictionary<string, string> httpHeaders;
	private static WebClient webclient;
	
	public RESThelper(string url, Dictionary<string, string> headers)
	{
		baseURL = url;
		httpHeaders = headers;
    }
	
	public static bool Validator (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
	{
        Debug.LogError(String.Format("Certificate error: {0}", sslPolicyErrors));
        if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
        {
            Debug.LogError("X509ChainStatus Details: ");
#if !UNITY_WEBPLAYER
            foreach (X509ChainStatus cs in chain.ChainStatus)
                Debug.LogError("X509ChainStatus: " + cs.StatusInformation);
#endif
        }
		return true;
	}
	
	public void AddHeader(string k, string v)
	{
		if(!httpHeaders.ContainsKey(k))
			httpHeaders.Add(k, v);
		else
			httpHeaders[k] = v;
	}
	
	public string sendRequest(string parseClass, string method, string data="")
	{
#if UNITY_WEBPLAYER
        return "";
#endif

		string ret = "";
		string url = baseURL + parseClass;
		method = method.ToUpper();
		
		if (method == "GET" && data != "")
			url = url + "?" + data;	
		
		Debug.Log("RESTHelper: " + method + " : " + url + " Data: " + data);
		
		ServicePointManager.ServerCertificateValidationCallback = Validator;
		//ServicePointManager.CertificatePolicy = new RESThelperICertificatePolicy();
		HttpWebRequest myHttpWebRequest=(HttpWebRequest)WebRequest.Create(url);
		myHttpWebRequest.Method = method;
		foreach(KeyValuePair<string, string> kv in httpHeaders){
			myHttpWebRequest.Headers.Add(kv.Key, kv.Value);
		}
		if (method == "POST" || method == "PUT") 
		{
			myHttpWebRequest.ContentType = "application/json";
			using (var streamWriter = new StreamWriter(myHttpWebRequest.GetRequestStream()))
			{
				streamWriter.Write(data);
				streamWriter.Flush();
				streamWriter.Close();
			}
		}
		HttpWebResponse httpResponse = null;
        try
        {
            httpResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
        }
        catch (WebException ex)
        {
            Debug.Log("WebException caught, Status code: " + ex.Status + " " + ex.ToString());
            if (ex.Response != null && ((HttpWebResponse)ex.Response).StatusCode > HttpStatusCode.Accepted)
            {
                Debug.Log("HTTP problem detected! Code: " + (int)(((HttpWebResponse)ex.Response).StatusCode));
            }
            httpResponse = (HttpWebResponse)ex.Response;
        }
        catch (System.Exception ex)
        {
            Debug.Log("HttpWebRequest::GetResponse exception caught " + ex.ToString());
        }
		finally
		{
            if (httpResponse != null)
            {
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    ret = streamReader.ReadToEnd();
                }
            }
		}		
		return ret;
	}
	
	static public void DownloadFile(string url, string destpath) {
		webclient = new WebClient();
		webclient.DownloadFile (url, destpath);
	}
}
