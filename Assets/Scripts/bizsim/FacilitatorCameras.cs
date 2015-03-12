using UnityEngine;
using System.Collections.Generic;

public class FacilitatorCameras : MonoBehaviour {

    private static List<FacilitatorCameras> facCams = new List<FacilitatorCameras>();
    public static List<FacilitatorCameras> GetAll() { return facCams; }

	void Awake () {
        facCams.Add(this);
        gameObject.SetActive(false);
	}

    void OnDestroy() {
        facCams.Remove(this);
    }
}
