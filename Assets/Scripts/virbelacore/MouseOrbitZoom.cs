//
//Filename: MouseOrbitZoom.cs
//
// original: http://www.unifycommunity.com/wiki/index.php?title=MouseOrbitZoom
//
// --01-18-2010 - create temporary target, if none supplied at start
 
using UnityEngine;
using System.Collections;
 
 
[AddComponentMenu("Camera-Control/3dsMax Camera Style")]
public class MouseOrbitZoom : MonoBehaviour
{
    public Transform target;
    public Vector3 targetOffset;
    public float distance = 5.0f;
    public float maxDistance = 120;
    public float minDistance = .6f;
    public float xSpeed = 200.0f;
    public float ySpeed = 200.0f;
    public int yMinLimit = -80;
    public int yMaxLimit = 80;
    public int zoomRate = 40;
    public float panSpeed = 0.3f;
    public float zoomDampening = 5.0f;

    private bool followTarget = true;
    private float xDeg = 0.0f;
    private float yDeg = 0.0f;
    private float currentDistance;
    private float desiredDistance;
    private Quaternion currentRotation;
    private Quaternion desiredRotation;
    private Quaternion rotation;
    private Vector3 position;
    private Vector3 posOffset = Vector3.zero;
    private GameObject targetGO;
    private GameObject tempTargetGO;
    private Camera cam;

    public bool FollowPosition { get { return followTarget; } set { followTarget = value; } }

    void Start() { Init(); }
    void OnEnable() { Init(); }

    public void SetTarget(GameObject go)
    {
        if (tempTargetGO == null)
            tempTargetGO = new GameObject("Cam Target");
        if (go == null)
            tempTargetGO.transform.position = transform.position + (transform.forward * distance);
        else
        {
            tempTargetGO.transform.position = go.transform.position;
            tempTargetGO.transform.rotation = go.transform.rotation;
        }
        target = tempTargetGO.transform;
        targetGO = go;
        posOffset = Vector3.zero;
    }

    public void Init()
    {

        //If there is no target, create a temporary target at 'distance' from the cameras current viewpoint
        if (!target)
            SetTarget(null);
        cam = GetComponent<Camera>();
 
        distance = Vector3.Distance(transform.position, target.position);
        currentDistance = distance;
        desiredDistance = distance;
 
        //be sure to grab the current rotations as starting points.
        position = transform.position;
        rotation = transform.rotation;
        currentRotation = transform.rotation;
        desiredRotation = transform.rotation;
 
        xDeg = Vector3.Angle(Vector3.right, transform.right );
        yDeg = Vector3.Angle(Vector3.up, transform.up );
    }
 
    /*
     * Camera logic on LateUpdate to only update after all character movement logic has been handled. 
     */
    void LateUpdate()
    {
        if (targetGO != null && followTarget)
        {
            target.position = targetGO.transform.position + posOffset;
        }

        // If Control and Alt and Middle button? ZOOM!
        if (Input.GetMouseButton(2) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftControl))
        {
            desiredDistance -= Input.GetAxis("Mouse Y") * Time.deltaTime * zoomRate*0.125f * Mathf.Abs(desiredDistance);
        }
        // If middle mouse and left alt are selected? ORBIT
        else if (Input.GetMouseButton(2) && Input.GetKey(KeyCode.LeftAlt))
        {
            xDeg += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            yDeg -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
 
            ////////OrbitAngle
 
            //Clamp the vertical axis for the orbit
            yDeg = ClampAngle(yDeg, yMinLimit, yMaxLimit);
            // set camera rotation 
            desiredRotation = Quaternion.Euler(yDeg, xDeg, 0);
            currentRotation = transform.rotation;
 
            rotation = Quaternion.Lerp(currentRotation, desiredRotation, Time.deltaTime * zoomDampening);
            transform.rotation = rotation;
        }
        // otherwise if middle mouse is selected, we pan by way of transforming the target in screenspace
        else if (Input.GetMouseButton(2))
        {
            Vector3 initialPos = target.transform.position;
            //grab the rotation of the camera so we can move in a psuedo local XY space
            target.rotation = transform.rotation;
            target.Translate(Vector3.right * -Input.GetAxis("Mouse X") * panSpeed);
            target.Translate(transform.up * -Input.GetAxis("Mouse Y") * panSpeed, Space.World);
            posOffset += (target.transform.position - initialPos);
        }
 
        ////////Orbit Position
 
        // affect the desired Zoom distance if we roll the scrollwheel
        desiredDistance -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * zoomRate * Mathf.Abs(desiredDistance);
        //clamp the zoom min/max
        desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
        // For smoothing of the zoom, lerp distance
        currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * zoomDampening);
 
        // calculate position based on the new currentDistance 
        position = target.position - (rotation * Vector3.forward * currentDistance + targetOffset);
        transform.position = position;
    }
 
    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }

    void OnGUI()
    {
        if (cam != null && Event.current != null && Event.current.type == EventType.MouseUp && Event.current.button == 0 && (Event.current.control || Event.current.command)) // 0 == LEFT MOUSE BUTTON
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            // Casts the ray and get the first game object hit.
            if (Physics.Raycast(ray, out hit))
            {
                Debug.LogError("Set target: " + hit.transform.gameObject.name);
                SetTarget(hit.transform.gameObject);
            }
        }
    }
}