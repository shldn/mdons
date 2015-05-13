using UnityEngine;
using System.Collections;

public class MaterialHelpers {

    static public void ReplaceAllMaterials(Renderer r, Material newMat)
    {
        Material[] materialsCopy = new Material[r.materials.Length];
        for (int i = 0; i < materialsCopy.Length; ++i)
            materialsCopy[i] = newMat;
        r.materials = materialsCopy;
    }

    static public void SetNewTexture(Renderer renderer, int materialIdx, Texture2D newTexture)
    {
        if (materialIdx < 0 || renderer == null || materialIdx >= renderer.materials.Length)
            return;

        // can't modify elements of the array directly, have to make a copy and set them all...
        Material[] materialsCopy = new Material[renderer.materials.Length];
        materialsCopy = renderer.materials;
        materialsCopy[materialIdx].SetTexture("_MainTex", newTexture);
        renderer.materials = materialsCopy;
    }
}
