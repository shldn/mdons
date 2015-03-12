using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UsersDAO {

	private RESThelper restHelper = null;
	
	public UsersDAO()
	{
		//init RESThelper
		restHelper = new RESThelper("https://api.parse.com/1/", new Dictionary<string, string>());
		restHelper.AddHeader("X-Parse-Application-Id", "zs5W1kr9RMFlrHaTLrkY8Rxz8W44TXbhZ8TPvdzq");
		restHelper.AddHeader( "X-Parse-REST-API-Key", "ZVekLieNrfFwb9H6dCiYZaOr5dlMPw01VY1Zlvgr");
	}
	
	public string Create(string username, string password)
	{
		string json = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\"}";
		return Create(json);
	}
	
	public string Create(string jsonData)
	{
		return restHelper.sendRequest("users", "POST", jsonData);
	}
	
	public string Login(string username, string password)
	{
		string getString = "username=" + username + "&password=" + password;
		return restHelper.sendRequest("login", "GET", getString);
	}
	
	public string PasswordReset(string jsonData)
	{
		return restHelper.sendRequest("requestPasswordReset", "POST", jsonData);
	}
	
	public string GetUser(string userId)
	{
		return restHelper.sendRequest("users/" + userId, "GET");
	}

    public string GetUserByColumnValue(string jsonQuery)
    {
        return restHelper.sendRequest("users/", "GET", "where=" + System.Uri.EscapeDataString(jsonQuery) + "&limit=1000");
    }
	
	public string GetAllUsers()
	{
		return restHelper.sendRequest("users", "GET");
	}

    public void IncrementColumn(string userId, string sessionToken, string col, int amt = 1)
    {
        string jsonData = "{\"" + col + "\":{\"__op\": \"Increment\",\"amount\": " + amt + "}}";
        UpdateUser(userId, sessionToken, jsonData);
    }
	
	public string UpdateUser(string userId, string sessionToken, string jsonData)
	{
		restHelper.AddHeader("X-Parse-Session-Token", sessionToken);
		return restHelper.sendRequest("users/" + userId, "PUT", jsonData);
	}
	
	public string DeleteUser(string userId, string sessionToken)
	{
		restHelper.AddHeader("X-Parse-Session-Token", sessionToken);
		return restHelper.sendRequest("users/" + userId, "DELETE");
	}
	
}
