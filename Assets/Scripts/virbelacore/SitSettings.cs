using UnityEngine;
using System.Collections;

public enum ChairType { CUSTOM = -1, COMFY = 0, BAR = 1, BENCH = 2 }

public class SitSettings : MonoBehaviour {

    [SerializeField]
    private ChairType chairType = ChairType.CUSTOM;
    [SerializeField]
    private float distanceFromChair = 0.9f;
    [SerializeField]
    private float dropDist = 35.8f - 35.38879f;

    public float DistanceFromChair { get { return distanceFromChair; } }
    public float DropDistance { get { return dropDist; } }
    public ChairType ChairTypeAcc{ 
        get{ return chairType; }
        set{
            chairType = value;
            switch(chairType)
            {
                case ChairType.COMFY:
                    distanceFromChair = 0.8f;
                    dropDist = -0.1f; 
                    break;
                case ChairType.BAR:
                    distanceFromChair = 0.3f;
                    dropDist = 0.4112091f;
                    break;
                case ChairType.BENCH:
                    break;

            }
        }
    }

}
