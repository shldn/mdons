using UnityEngine;

public class SplineDecorator : MonoBehaviour {

	public BezierSpline spline;

	public int frequency;

    public float percentage = 1.0f;

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
        if (frequency <= 0 || items == null || items.Length == 0 || percentage <= 0.0f)
        {
            return;
        }
        float stepSize = frequency * items.Length / percentage;
        if (spline.Loop || stepSize == 1)
        {
            stepSize = 1f / stepSize;
        }
        else
        {
            stepSize = 1f / (stepSize - 1);
        }
        for (int p = 0, f = 0; f < frequency; f++)
        {
            for (int i = 0; i < items.Length; i++, p++)
            {
                Transform item = Instantiate(items[i]) as Transform;
                Vector3 position = spline.GetPoint(p * stepSize);
                item.transform.localPosition = position;
                if (lookForward)
                {
                    item.transform.LookAt(position + spline.GetDirection(p * stepSize));
                }
                item.transform.parent = transform;
            }
        }
    }
}