using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class PositionDataExporter {

    class EntryInfo {
        public EntryInfo(string name_)
        {
            name = name_;
            active = false;
        }

        public Vector3 Pos { 
            get { return pos; } 
            set { pos = new Vector3(value.x, 0, value.z); active = true; }  // remove y component
        }

        public bool active;
        public string name;
        private Vector3 pos;

    }
    bool skipToNextUpdate = false;
    float sampleSeconds;
    string delim = "\t";
    string filename;
    DateTime startTime;
    DateTime lastUpdateTime;
    Dictionary<string, int> playerIdx = new Dictionary<string,int>(); // name --> index into playerData
    List<EntryInfo> playerData = new List<EntryInfo>();

    public PositionDataExporter(string filename_, DateTime startTime_, float sampleSeconds_)
    {
        filename = filename_;
        startTime = startTime_;
        sampleSeconds = sampleSeconds_;
        lastUpdateTime = startTime.Subtract(TimeSpan.FromSeconds(sampleSeconds));
    }

    public void Initialize(List<string> names)
    {
        string header = "time" + delim + "NumUsers" + delim;
        string distHeader = "";
        for(int i=0; i < names.Count; ++i)
        {
            Debug.LogError("Add: " + names[i]);
            playerData.Add(new EntryInfo(names[i]));
            playerIdx.Add(names[i], i);
            header += names[i] + ".x" + delim + names[i] + ".z" + delim;
            for (int j = i + 1; j < names.Count; ++j)
                distHeader += "dist " + names[i] + "-" + names[j] + delim;
            distHeader += "dist " + names[i] + "-" + "All" + delim;
            distHeader += "dist " + names[i] + "-" + "Ave" + delim;
        }

        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
        {
            file.WriteLine(header + distHeader);
        }
    }

    private int GetNumActiveUsers()
    {
        int count = 0;
        for (int i = 0; i < playerData.Count; ++i)
            count += (playerData[i].active) ? 1 : 0;
        return count;
    }

    public void Update(DateTime time, string name, Vector3 pos)
    {
        if (!IsTrackingPlayer(name))
            return;

        if (!skipToNextUpdate && time.Subtract(lastUpdateTime).TotalSeconds < sampleSeconds)
        {
            playerData[playerIdx[name]].Pos = pos;
            return;
        }

        if (skipToNextUpdate)
        {
            lastUpdateTime = time;
            skipToNextUpdate = false;
        }


        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename,true))
        {
            while (time.Subtract(lastUpdateTime).TotalSeconds > sampleSeconds)
            {
                lastUpdateTime = lastUpdateTime.AddSeconds(sampleSeconds);

                int numActiveUsers = GetNumActiveUsers();
                string dataLine = lastUpdateTime.ToString() + delim + numActiveUsers + delim;
                string dataDistLine = "";
                for (int i = 0; i < playerData.Count; ++i)
                {
                    float distToAll = 0.0f;
                    if (playerData[i].active)
                        dataLine += playerData[i].Pos.x;
                    dataLine += delim;
                    if (playerData[i].active)
                        dataLine += playerData[i].Pos.z;
                    dataLine += delim;
                    for (int j = 0; j < playerData.Count; ++j)
                    {
                        string distStr = "";
                        if (i != j && playerData[i].active && playerData[j].active)
                        {
                            float dist = Vector3.Distance(playerData[i].Pos, playerData[j].Pos);
                            distToAll += dist;
                            distStr += dist;
                        }
                        if (j >= i + 1)
                            dataDistLine += distStr + delim;
                    }

                    // save distances to other players: all then average.
                    if (distToAll > 0.0f)
                        dataDistLine += distToAll;
                    dataDistLine += delim;
                    if (distToAll > 0.0f)
                        dataDistLine += distToAll / (float)numActiveUsers;
                    dataDistLine += delim;
                }
                if( AnyActive() )
                    file.WriteLine(dataLine + dataDistLine);
            }
        }
        playerData[playerIdx[name]].Pos = pos;
    }

    private bool AnyActive()
    {
        for (int i = 0; i < playerData.Count; ++i)
            if (playerData[i].active)
                return true;
        return false;
    }

    public void PlayerExit(string name)
    {
        if (!IsTrackingPlayer(name))
            return;
        playerData[playerIdx[name]].active = false;
        if (!AnyActive())
            skipToNextUpdate = true;
    }

    private bool IsTrackingPlayer(string name)
    {
        return playerIdx.ContainsKey(name);
    }
}
