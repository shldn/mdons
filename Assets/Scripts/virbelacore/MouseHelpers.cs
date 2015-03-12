using UnityEngine;

public struct MouseHitData
{
    public MouseHitData(int frame, GameObject go, RaycastHit hit_)
    {
        frameNumber = frame;
        gameObject = go;
        hit = hit_;
    }
    public int frameNumber;
    public GameObject gameObject;
    public RaycastHit hit;
}

public class MouseHelpers {

    private static MouseHitData lastMouseUpHitData; 

    // returns the first game object the mouse position is currently over
    public static GameObject GetGameObject(out RaycastHit hit)
    {
        // Builds a ray from camera point of view to the mouse position.
        if(!Camera.main){
            hit = new RaycastHit();
            return null;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // Casts the ray and get the first game object hit.
        if(Physics.Raycast(ray, out hit))
		{
			Debug.DrawLine(ray.origin, hit.point);
			return hit.transform.gameObject;
		}
		else
		{
			Debug.DrawLine(ray.origin, ray.direction * 10, Color.yellow);
			return null;
		}
    }

    // get the first GameObject hit by a ray from the mouse position shot into the scene at the current frame
    // stores the GameObject hit at a frame to save multiple intersection calls per frame: OnGUI could potentially call this a lot every frame.
    public static GameObject GetCurrentGameObjectHit(out RaycastHit hit)
    {
        if (lastMouseUpHitData.frameNumber != Time.frameCount)
        {
            GameObject go = GetGameObject(out hit);
            lastMouseUpHitData = new MouseHitData(Time.frameCount, go, hit);
        }
        hit = lastMouseUpHitData.hit;
        return lastMouseUpHitData.gameObject;
    }

    // get the first GameObject hit by a ray from the mouse position shot into the scene at the current frame
    public static GameObject GetCurrentGameObjectHit()
    {
        RaycastHit hit;
        return GetCurrentGameObjectHit(out hit);
    }
}
