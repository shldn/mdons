using UnityEngine;
using Sfs2X.Entities;
using System.Collections.Generic;
using System.Globalization;

public class LocalBotManager {

    private static LocalBotManager mInstance;
    public static LocalBotManager Inst
    {
        get
        {
            if (mInstance == null){
                mInstance = new LocalBotManager();
            }
            return mInstance;
        }
    }
    
    private Dictionary<string, Player> localBots = new Dictionary<string, Player>();
    public Dictionary<string, Player> GetBots() { return localBots; }
    public void DestroyAll() 
    {
        foreach(KeyValuePair<string,Player> bot in localBots)
            Object.Destroy(bot.Value.gameObject);
        localBots.Clear(); 
    }

    public void Clear()
    {
        localBots.Clear();
    }

    public Player Create(Vector3 pos, Quaternion rot, bool male = true, bool addToUserList = false, string name = "")
    {
        int modelIdx = male ? 0 : 1;
        if (name == "")
            name = RandomName(male);
        ForceUniqueName(ref name);

        // create player
        GameObject go = GameManager.Inst.playerManager.CreateRemotePlayerGO(modelIdx);
        MsgData userInitData = new MsgData();
        userInitData.name = name;
        userInitData.playerID = UnityEngine.Random.Range(0, 8);
        User user = new ReplayUser(userInitData);
        Player bot = new Player(user, go, modelIdx, PlayerType.STEALTH, "", userInitData.name, 0);
        bot.isBot = true;
        AvatarOptionManager.Inst.CreateRandomAvatar(bot, false);

        // assign transform
        bot.gameObject.transform.position = pos;
        bot.gameObject.transform.rotation = rot;
        bot.gameObject.GetComponent<PlayerController>().forwardAngle = rot.eulerAngles.y;

        // make visible and kill bouncing
        bot.gameObject.SetActive(true);
        bot.gameObject.GetComponent<SimpleRemoteInterpolation>().enabled = false;

        if(addToUserList)
            GameGUI.Inst.ExecuteJavascriptOnGui(bot.GetAddToGUIUserListJSCmd());

        localBots.Add(name, bot);
        return bot;
    }


    public Player CreateRon(Vector3 pos, Quaternion rot, bool male = true, string name = "")
    {
        int modelIdx = male ? 0 : 1;
        if (name == "")
            name = RandomName(male);
        ForceUniqueName(ref name);

        // create player
        GameObject go = GameObject.Instantiate(Resources.Load("Avatars/ron")) as GameObject;
        MsgData userInitData = new MsgData();
        userInitData.name = name;
        userInitData.playerID = UnityEngine.Random.Range(0, 8);
        User user = new ReplayUser(userInitData);
        Player bot = new Player(user, go, modelIdx, PlayerType.STEALTH, "", userInitData.name, 0);
        bot.isBot = true;

        // assign transform
        bot.gameObject.transform.position = pos;
        bot.gameObject.transform.rotation = rot;
        bot.gameObject.GetComponent<PlayerController>().forwardAngle = rot.eulerAngles.y;

        // make visible and kill bouncing
        bot.gameObject.SetActive(true);

        localBots.Add(name, bot);
        return bot;
    }

    private void ForceUniqueName(ref string name)
    {
        string origName = name;
        if (localBots.ContainsKey(name))
        {
            char letterCounter = (char)65; // start with ascii A
            name = origName + (char)letterCounter;
            while (localBots.ContainsKey(name))
            {
                letterCounter++;
                if (letterCounter > 90) // 90 == ascii Z
                {
                    origName = name;
                    letterCounter = (char)65; // 65 == ascii A
                }

                name = origName + (char)letterCounter;
            }
        }
    }

    private string RandomName(bool male)
    {
        if(male)
            return PlayerManager.maleNames[Random.Range(0, PlayerManager.maleNames.Length)];
        else
            return PlayerManager.femaleNames[Random.Range(0, PlayerManager.femaleNames.Length)];
    }

    public void StartAnimation(string botName, string animName)
    {
        string animNameTC = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(animName);
        Player bot;
        if (localBots.TryGetValue(botName, out bot))
            bot.gameObject.GetComponent<AnimatorHelper>().StartAnim(animNameTC, false);
        else
            Debug.LogError("Bot: " + botName + " not found");
    }

    public void StopAnimation(string botName, string animName)
    {
        string animNameTC = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(animName);
        Player bot;
        if (localBots.TryGetValue(botName, out bot))
            bot.gameObject.GetComponent<AnimatorHelper>().StopAnim(animNameTC, false);
        else
            Debug.LogError("Bot: " + botName + " not found");
    }

    public void AnimateAll(string animName)
    {
        string animNameTC = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(animName);
        foreach( KeyValuePair<string, Player> bot in localBots )
            bot.Value.gameObject.GetComponent<AnimatorHelper>().StartAnim(animNameTC, false);
    }

    public Player GetBotByName(string name)
    {
        string lowercaseName = name.ToLower().Trim();
        foreach (KeyValuePair<string, Player> playerPair in localBots)
        {
            if (playerPair.Key.ToLower().Trim().Equals(lowercaseName)){
                return playerPair.Value;
            }
        }
        return null;
    }

    public void DestroyBotByName(string name)
    {
        Player bot = GetBotByName(name);
        localBots.Remove(name);
        Object.Destroy(bot.gameObject);
    }
}
