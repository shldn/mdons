using UnityEngine;

// This Places Objects at the points of the spline
public class SplinePtDecorator : MonoBehaviour {

	public BezierSpline spline;

	public bool lookForward;

	public Transform[] items;

	private void Start () {
        Decorate();
	}

    public void ReDecorate()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        Decorate();
    }

    private void Decorate()
    {
        if (items == null || items.Length == 0)
            return;

        int i = 0;
        for (int p = 0; p < spline.ControlPointCount; p+=3, ++i )
        {
            if (items[i % items.Length] == null)
                continue;
            Transform item = Instantiate(items[i % items.Length]) as Transform;
            Vector3 position = spline.transform.TransformPoint(spline.GetControlPoint(p));
            item.transform.localPosition = position;
            if (lookForward)
            {
                if( p < spline.ControlPointCount-1 )
                    item.transform.LookAt(spline.transform.TransformPoint(spline.GetControlPoint(p + 1)));
                else
                    item.transform.forward = (spline.transform.TransformPoint(spline.GetControlPoint(p)) - spline.transform.TransformPoint(spline.GetControlPoint(p-1)));
            }
            item.transform.parent = transform;

            // Should bind event instead to make more abstract.
            if( item.gameObject != null && item.gameObject.GetComponent<TunnelEventTrigger>())
                item.gameObject.GetComponent<TunnelEventTrigger>().splinePtIdx = p / 3;
        }
    }
}