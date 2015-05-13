using UnityEngine;
using System.Collections;

public class RealTimeRecursiveObjectGenerator : MonoBehaviour {

    GameObject baseObject = null;
    public Transform nextLevelTransform = null;
    public GameObject objToTransform = null;

    public bool createdBigger = false;
    public bool createdSmaller = false;

	void Start () {
        if (baseObject == null)
            baseObject = gameObject;
        if( nextLevelTransform == null )
            nextLevelTransform = transform.Find("RecursiveTransform");
	}

    void Update()
    {
        if (!createdBigger && Input.GetKeyUp(KeyCode.B))
            CreateBigger();
        if (!createdSmaller && Input.GetKeyUp(KeyCode.N))
            CreateSmaller();

    }

    void CreateBigger()
    {
        GameObject parent = GameObject.Instantiate(baseObject) as GameObject;
        parent.name = name;
        
        Vector3 invNextLevelScale = 2f * Vector3.one - nextLevelTransform.localScale;
        parent.transform.localScale = new Vector3(invNextLevelScale.x * transform.lossyScale.x, invNextLevelScale.y * transform.lossyScale.y, invNextLevelScale.z * transform.lossyScale.z);

        RealTimeRecursiveObjectGenerator parentGenerator = parent.GetComponent<RealTimeRecursiveObjectGenerator>();
        parentGenerator.nextLevelTransform = parent.transform.Find(nextLevelTransform.name);
        parentGenerator.createdSmaller = true;

        parent.transform.position = transform.position;
        Vector3 posOffset = nextLevelTransform.position - transform.position;
        parent.transform.position = transform.position - new Vector3(posOffset.x * invNextLevelScale.x, posOffset.y * invNextLevelScale.y, posOffset.z * invNextLevelScale.z);

        Quaternion newRot = Quaternion.Inverse(nextLevelTransform.localRotation);
        Vector3 newRotAxis = Vector3.up;
        float newRotAngle = 0f;
        newRot.ToAngleAxis(out newRotAngle, out newRotAxis);
        parent.transform.RotateAround(transform.position, newRotAxis, newRotAngle);

        parent.transform.parent = transform.parent;
        createdBigger = true;
    }

    void CreateSmaller()
    {
        GameObject child = GameObject.Instantiate(baseObject) as GameObject;
        child.name = name;
        child.transform.position = nextLevelTransform.position;
        child.transform.rotation = nextLevelTransform.rotation;
        child.transform.localScale = new Vector3(nextLevelTransform.localScale.x * transform.lossyScale.x, nextLevelTransform.localScale.y * transform.lossyScale.y, nextLevelTransform.localScale.z * transform.lossyScale.z);
        child.GetComponent<RealTimeRecursiveObjectGenerator>().nextLevelTransform = child.transform.Find(nextLevelTransform.name);
        child.GetComponent<RealTimeRecursiveObjectGenerator>().createdBigger = true;
        child.transform.parent = transform.parent;
        createdSmaller = true;
    }
}
