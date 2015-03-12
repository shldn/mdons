using UnityEngine;
using System.Collections;

public enum BillboardType
{
    MATCH_POSITION,
    MATCH_ORIENT
}

public class Billboard : MonoBehaviour {

    public bool flipDirection = false; // if billboarding the opposite direction, flip it around
    [SerializeField()]
	private BillboardType type = BillboardType.MATCH_POSITION;
    public Camera cam;
    public bool interpolate = false;
    public float interpSpeed = 1.0f;

    private float vel = 0.0f;
    private float t = 0.0f;

    // changing type will reset the interpolation, to give a little more time.
    public BillboardType Type { get { return type; } set { if (type != value) { t = 0.0f; vel = 0.0f; } type = value; } } 

    void Start()
    {
        if (cam == null)
            cam = Camera.main;
    }

	void LateUpdate () {
        float dir = flipDirection ? -1.0f : 1.0f;
        Quaternion from = transform.rotation;
        Quaternion to = Quaternion.identity;
        if (type == BillboardType.MATCH_POSITION)
        {
            Vector3 heading = cam.transform.position - transform.position;
            transform.LookAt(transform.position + dir * heading);
            to = transform.rotation;
        }
        else // MATCH_ORIENT
        {
            transform.forward = -dir * cam.transform.forward;
            to = transform.rotation;
        }
        if ( interpolate && t < 1.0f )
        {
            t = Mathf.SmoothDamp(t, interpSpeed * Time.deltaTime, ref vel, 1.0f);
            transform.rotation = Quaternion.Slerp(from, to, t);
        }
	}
}
