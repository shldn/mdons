using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TeleportLocation : MonoBehaviour {
    public string spawnPtName;

    private static List<TeleportLocation> locations = new List<TeleportLocation>();
    private static Dictionary<string, TeleportLocation> locationsByName = new Dictionary<string, TeleportLocation>();
    public static List<TeleportLocation> GetAll() { return locations; }
    public static TeleportLocation GetByName(string name) 
    {
        TeleportLocation tele = null;
        locationsByName.TryGetValue(name, out tele);
        return tele; 
    }

    void Awake()
    {
        if( string.IsNullOrEmpty(spawnPtName) )
    		spawnPtName = this.name;
        locations.Add(this);
        locationsByName.Add(spawnPtName, this);
    }
	
	void OnDestroy()
	{
		locations.Clear();
        locationsByName.Clear();
	}

}

