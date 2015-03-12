using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OrientationBotAudience : MonoBehaviour {

    public static OrientationBotAudience Inst = null;

    float spawnDelay = 0.5f;
    float spawnCooldown = 0f;
    public bool spawnAudience = false;
    List<Transform> availablePositions = new List<Transform>();
    List<Player> audience = new List<Player>();

    void Awake(){
        Inst = this;
    } // End of Awake().

    void Start(){
        GameObject[] audiencePositions = GameObject.FindGameObjectsWithTag("AudiencePos");
        for(int i = 0; i < audiencePositions.Length; i++)
            availablePositions.Add(audiencePositions[i].transform);
    } // End of Start().

    void Update(){

        spawnCooldown -= Time.deltaTime;

        if(spawnAudience && (availablePositions.Count > 0) && (spawnCooldown <= 0f)){
            spawnCooldown = spawnDelay;
            Player newBot = LocalBotManager.Inst.Create(GameManager.Inst.playerManager.GetLocalSpawnTransform().position, GameManager.Inst.playerManager.GetLocalSpawnTransform().rotation, (Random.Range(0f, 1f) > 0.5f), GameManager.buildType == GameManager.BuildType.REPLAY, "");
            audience.Add(newBot);
            int posToTake = Random.Range(0, availablePositions.Count);
            newBot.BotGoto(availablePositions[posToTake].position, availablePositions[posToTake].eulerAngles.y);
            availablePositions.RemoveAt(posToTake);
        }

        for(int i = 0; i < audience.Count; i++){
            Player currentBot = audience[i];
            if(Random.Range(0f, 1f) <= 0.001f){
                if(!currentBot.playerController.pathfindingActive)
                    currentBot.gameObject.GetComponent<AnimatorHelper>().StartAnim("Think", false);
            }
        }

    } // End of Update().

    public void SpawnAudience(){
        spawnAudience = true;
    } // End of SpawnAudience().
}
