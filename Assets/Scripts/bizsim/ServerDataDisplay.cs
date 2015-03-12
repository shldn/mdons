using UnityEngine;
using System.Collections;

public class ServerDataDisplay : MonoBehaviour {
    TextMesh teamRankTextMesh;
    TextMesh currentQuarterTextMesh;

	void Awake () {
        GameObject teamRankGO = GameObject.Find("TeamRank");
        if (teamRankGO != null)
            teamRankTextMesh = teamRankGO.GetComponent<TextMesh>();
        GameObject currentQuarterGO = GameObject.Find("CurrentQuarter");
        if (currentQuarterGO != null)
            currentQuarterTextMesh = currentQuarterGO.GetComponent<TextMesh>();
	}

    public void SetRank(int rank)
    {
        if( teamRankTextMesh != null )
            teamRankTextMesh.text = "Team Rank: " + rank;
    }

    public void SetCurrentQuarter(int quarter)
    {
        if( currentQuarterTextMesh != null )
            currentQuarterTextMesh.text = "Current Quarter: " + quarter;
    }
}
