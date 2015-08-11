using UnityEngine;
using System.Collections.Generic;

public class CityBlockBotController : MonoBehaviour {

    class GridPt
    {
        public GridPt(int row_, int col_) { row = row_; col = col_; }

        public int row;
        public int col;

        public bool IsEqual(int r, int c) { return r == row && c == col; }
    }

    CityBlockGenerator cityBlockGen = null;
    List<GridPt> walkPts = new List<GridPt>();

	void Start () {
        if (cityBlockGen == null)
            cityBlockGen = GetComponent<CityBlockGenerator>();

        // Set path bots will walk along the grid.
        walkPts.Add(new GridPt(0, 0));
        walkPts.Add(new GridPt(0, 2));
        walkPts.Add(new GridPt(2, 2));
        walkPts.Add(new GridPt(2, 0));

        // reverse every other level's walk order/direction
        if( cityBlockGen.innerRecursionLevel % 2 == 1 )
            walkPts.Reverse();

        CreateBots();
	}

    void Update()
    {
        // keep bots above ground.
        foreach(KeyValuePair<string,Player> bot in LocalBotManager.Inst.GetBots())
        {
            if( bot.Value.gameObject.transform.position.y < PlayerManager.Inst.RespawnHeight )
            {
                BotMover mover = bot.Value.gameObject.GetComponent<BotMover>();
                if( mover != null )
                    bot.Value.UpdateTransform(mover.Destination, transform.eulerAngles.y);
            }
        }
    }

    void CreateBots()
    {
        int idx = 0;
        for(int r=0; r < 3; ++r)
        {
            for (int c = 0; c < 3; ++c)
            {
                if (r == 1 || c == 1)
                    continue;
                Vector3 pos = cityBlockGen.GetRoadIntersectionPosition(r, c);
                Player p = LocalBotManager.Inst.Create(pos, Quaternion.identity, Random.Range(0, 2) > 0);
                p.playerController.navMode = PlayerController.NavMode.physics;
                p.gameObject.transform.parent = cityBlockGen.meshContainer;
                p.Scale = 200f * Vector3.one;
                BotMover bMover = p.gameObject.AddComponent<BotMover>();

                List<Vector3> destinationPts = GetWalkPath(r, c);
                bMover.DestinationSet = destinationPts;
                ++idx;
            }
        }
    }

    List<Vector3> GetWalkPath(int row, int col)
    {
        // find start idx
        int idx = -1;
        for (int i = 0; i < walkPts.Count && idx == -1; ++i)
            if (walkPts[i].IsEqual(row, col))
                idx = i;

        if (idx == -1)
            Debug.LogError("GetWalkPath: didn\'t find point at " + row + ", " + col);

        List<Vector3> destPts = new List<Vector3>();
        for (int i = idx + 1; i < walkPts.Count; ++i)
            destPts.Add(cityBlockGen.GetRoadIntersectionPosition(walkPts[i].row, walkPts[i].col));
        for (int i = 0; i <= idx; ++i)
            destPts.Add(cityBlockGen.GetRoadIntersectionPosition(walkPts[i].row, walkPts[i].col));
        return destPts;
    }
}
