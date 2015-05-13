using UnityEngine;
using System.Collections;

public class RecursiveObjCreationEventArgs : System.EventArgs
{
    public RecursiveObjCreationEventArgs(int id_, Vector3 pos_) { id = id_; position = pos_; }
    public int id;
    public Vector3 position;
}


public class RealTimeRecursiveObjectGenerator : MonoBehaviour {

    GameObject baseObject = null;
    public Transform nextLevelTransform = null;
    public GameObject objToTransform = null;

    public int id = 0;
    public bool grounded = true;
    private bool setGrounded = false;
    public RealTimeRecursiveObjectGenerator biggerNeighbor = null;
    public RealTimeRecursiveObjectGenerator smallerNeighbor = null;

    // events
    public delegate void CreationHandler(object sender, RecursiveObjCreationEventArgs e);
    public static event CreationHandler Creation;

	void Start () {
        if (baseObject == null)
            baseObject = gameObject;
        if( nextLevelTransform == null )
            nextLevelTransform = transform.Find("RecursiveTransform");
        RaiseCreationEvent();
	}

    void Update()
    {
        if (!biggerNeighbor && Input.GetKeyUp(KeyCode.B))
            CreateBigger();
        if (!smallerNeighbor && Input.GetKeyUp(KeyCode.N))
            CreateSmaller();
        if (grounded && Input.GetKeyUp(KeyCode.K))
        {
            if( biggerNeighbor != null )
            {
                Transform topParent = transform.parent;
                while (topParent.parent != null)
                    topParent = topParent.parent;
                float angle = 10;
                topParent.transform.RotateAround(transform.position, Vector3.forward, angle);
                Vector3 groundOffset = biggerNeighbor.transform.position.y * Vector3.up;
                Vector3 rightOffset = (biggerNeighbor.transform.position.x - transform.position.x) * Vector3.right;
                topParent.transform.position -= groundOffset;
                topParent.transform.position -= rightOffset;
                biggerNeighbor.setGrounded = true;
                grounded = false;
            }
        }
        if( grounded && Input.GetKeyUp(KeyCode.L) )
        {
            if (smallerNeighbor != null)
            {

                Transform topParent = transform.parent;
                while (topParent.parent != null)
                    topParent = topParent.parent;
                float angle = -10;
                topParent.transform.RotateAround(smallerNeighbor.transform.position, Vector3.forward, angle);
                Vector3 groundOffset = smallerNeighbor.transform.position.y * Vector3.up;
                Vector3 rightOffset = (smallerNeighbor.transform.position.x - transform.position.x) * Vector3.right;
                topParent.transform.position -= groundOffset;
                topParent.transform.position -= rightOffset;
                smallerNeighbor.setGrounded = true;
                grounded = false;
            }
        }

        if (setGrounded)
        {
            grounded = true;
            setGrounded = false;
        }

    }

    void CreateBigger()
    {
        GameObject parent = GameObject.Instantiate(baseObject) as GameObject;
        parent.name = name;
        
        Vector3 invNextLevelScale = 2f * Vector3.one - nextLevelTransform.localScale;
        parent.transform.localScale = new Vector3(invNextLevelScale.x * transform.lossyScale.x, invNextLevelScale.y * transform.lossyScale.y, invNextLevelScale.z * transform.lossyScale.z);

        parent.GetComponent<RealTimeRecursiveObjectGenerator>().nextLevelTransform = parent.transform.Find(nextLevelTransform.name);
        parent.GetComponent<RealTimeRecursiveObjectGenerator>().id = id;
        parent.GetComponent<RealTimeRecursiveObjectGenerator>().grounded = false;
        parent.GetComponent<RealTimeRecursiveObjectGenerator>().smallerNeighbor = this;
        biggerNeighbor = parent.GetComponent<RealTimeRecursiveObjectGenerator>();

        parent.transform.position = transform.position;
        Vector3 posOffset = nextLevelTransform.position - transform.position;
        parent.transform.position = transform.position - new Vector3(posOffset.x * invNextLevelScale.x, posOffset.y * invNextLevelScale.y, posOffset.z * invNextLevelScale.z);

        Quaternion newRot = Quaternion.Inverse(nextLevelTransform.localRotation);
        Vector3 newRotAxis = Vector3.up;
        float newRotAngle = 0f;
        newRot.ToAngleAxis(out newRotAngle, out newRotAxis);
        parent.transform.RotateAround(transform.position, newRotAxis, newRotAngle);

        parent.transform.parent = transform.parent;
    }

    void CreateSmaller()
    {
        GameObject child = GameObject.Instantiate(baseObject) as GameObject;
        child.name = name;
        child.transform.position = nextLevelTransform.position;
        child.transform.rotation = nextLevelTransform.rotation;
        child.transform.localScale = new Vector3(nextLevelTransform.localScale.x * transform.lossyScale.x, nextLevelTransform.localScale.y * transform.lossyScale.y, nextLevelTransform.localScale.z * transform.lossyScale.z);
        child.GetComponent<RealTimeRecursiveObjectGenerator>().nextLevelTransform = child.transform.Find(nextLevelTransform.name);
        child.GetComponent<RealTimeRecursiveObjectGenerator>().id = id;
        child.GetComponent<RealTimeRecursiveObjectGenerator>().grounded = false;
        child.GetComponent<RealTimeRecursiveObjectGenerator>().biggerNeighbor = this;
        smallerNeighbor = child.GetComponent<RealTimeRecursiveObjectGenerator>();
        child.transform.parent = transform.parent;
    }

    private void RaiseCreationEvent()
    {
        try
        {
            if (Creation != null)
                Creation(this, new RecursiveObjCreationEventArgs(id, transform.position));
        }
        catch
        {
            Debug.Log("Exception raised throwing CreationHandler event");
        }
    }

}
