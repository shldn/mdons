using UnityEngine;
using System.Collections.Generic;

public class GameObjectStack : MonoBehaviour {

    private Stack<GameObject> gameObjectStack = new Stack<GameObject>();
    private Vector3 dropDirection = Vector3.up;
    private float dropPointSpacingDist = 1.0f;

    public string PrefabName{ get; set; }
    public IEnumerator<GameObject> GetEnumerator() { return gameObjectStack.GetEnumerator(); }

    // spacing as a percentage of the bounding box of the prefab. (if prefab is 100 wide, percentage is 1.1, space between them will be 10)
    public float DropPointSpacingDist { get { return dropPointSpacingDist; } set { dropPointSpacingDist = value; } }

    // direction new objects should be placed
    public Vector3 DropDirection { 
        get { return dropDirection; } 
        set {
            dropDirection = value;
            if (dropDirection.sqrMagnitude != 1.0f)
                dropDirection.Normalize();
        } 
    }

    // position of drop applies this offset from the original prefab position 
    public Vector3 PositionOffset { get; set; }

    public int NumObjects{
        get { return gameObjectStack.Count; }
        set
        {
            while (gameObjectStack.Count < value)
            {
                Object prefabObj = Resources.Load(PrefabName);
                if (prefabObj == null)
                {
                    Debug.LogError("GameObjectStack: Unable to load " + PrefabName);
                    return;
                }
                GameObject go = GameObject.Instantiate(prefabObj) as GameObject;
                go.transform.position += PositionOffset + gameObjectStack.Count * DropPointSpacingDist * DropDirection;
                gameObjectStack.Push(go);
            }
            while (gameObjectStack.Count > value)
            {
                GameObject car = gameObjectStack.Pop();
                GameObject.Destroy(car);
            }
        }
    }
}
