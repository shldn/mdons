using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AvatarOptionManager{
    private static AvatarOptionManager mInstance;
    public static AvatarOptionManager Instance
    {
        get
        {
            if (mInstance == null)
                mInstance = new AvatarOptionManager();
            return mInstance;
        }
    }

    public static AvatarOptionManager Inst
    {
        get { return Instance; }
    }

    // gender indices
    public static readonly int MALE = 0;
    public static readonly int FEMALE = 1;

    System.Random random = new System.Random(); // for randomly generated characters.
    public Dictionary<int, Dictionary<string, ResourceOptionList>> Options { get { return avatarOptions; } }
    public Dictionary<int, List<string>> OptionTypes { get { return optionTypes; } }

    Dictionary<int, Dictionary<string, ResourceOptionList>> avatarOptions; // map from avatar model index to it's list of options keyed by name.
    Dictionary<int, List<string>> optionTypes = new Dictionary<int, List<string>>();

    private AvatarOptionManager()
    {
        avatarOptions = new Dictionary<int, Dictionary<string, ResourceOptionList>>();
        Initialize();
    }

    private void Initialize()
    {
        if (GameManager.Inst.ServerConfig == "Medical")
        {
            InitializeMedical();
            return;
        }

        // hard code city -- move this to an external database at some point!
        int mIdx;

        // character 0 -- male
        mIdx = MALE;
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Hair", new List<string>() { "Hair1", "Hair2", "Hair3", "Hair4", "Hair5", ""}, "Hair", "Hair", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "HairC", new List<string>() { "161616", "3A2F1E", "472400", "935436", "775A1B", "c28340", "E6CC80", "9E9E9C"}, new List<string>() { "Hair1", "Hair2", "Hair3", "Hair4", "Hair5", "_Skin1/Brows1", "_Skin2/Brows2", "_Skin3/Brows3", "_Skin4/Brows4", "_Skin5/Brows5", "_Skin6/Brows6", "_Skin7/Brows7", "FHair1", "FHair2", "FHair3", "FHair4", "Eyebrows" }, "Hair Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "hair", new List<string>() { "", "FHair1", "FHair2", "FHair3", "FHair4" }, "FHair", "Facial Hair", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.TEXTURE, "Eyes", new List<string>() { "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Brown", "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Hazel", "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Green", "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Blue" }, "Eyes", "Eye Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Skin", new List<string>() { "_Skin1", "_Skin2", "_Skin3", "_Skin4", "_Skin5", "_Skin6", "_Skin7" }, "Skin", "Face", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "SkinC", new List<string>() { "fcd7b5", "EBC19C", "f2c5b7", "B99261", "7E5F46", "48312A" }, new List<string>() { "_Skin1", "_Skin2", "_Skin3", "_Skin4", "_Skin5", "_Skin6", "_Skin7", "Hands" }, "Skin Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Shirt", new List<string>() { "Shirt1", "Shirt2", "Shirt3" }, "Shirt", "Shirt", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "ShirtC", new List<string>() { "E3E3E3", "A6A8AB", "A8A096", "7D8CBC", "293A6F", "161616" }, new List<string>() { "Shirt1", "Shirt2", "Shirt3" }, "Shirt Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Tie", new List<string>() { "", "Tie1", "Tie2" }, "Tie", "Tie", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "TieC", new List<string>() { "161616", "5B0606", "2F4585", "4C553B", "5D5142" }, new List<string>() { "Tie1", "Tie2" }, "Tie Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Jacket", new List<string>() { "", "Jacket1", "Jacket2", "Jacket3", "Jacket4", "Jacket5", "Jacket6" }, "Jacket", "Jacket", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "JacketC", new List<string>() { "161616", "404041", "281C0E", "141D38", "343A28", "747063" }, new List<string>() { "Jacket1", "Jacket2", "Jacket3", "Jacket4", "Jacket5", "Jacket6" }, "Jacket Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Bottoms", new List<string>() { "Bottoms1", "Bottoms2", "Bottoms3", "Bottoms4" }, "Bottoms", "Bottoms", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "BottomsC", new List<string>() { "161616", "404041", "281C0E", "141D38", "343A28", "747063" }, new List<string>() { "Bottoms1", "Bottoms2", "Bottoms3", "Bottoms4" }, "Bottoms Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "AddOn", new List<string>() { "", "Glass1", "Glass2", "Glass7", "Glass8", "Glass3", "Glass4", "Glass5", "Glass6" }, "Glass", "Glasses", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "AddOnC", new List<string>() { "CCB69D", "AB435B", "5E4773", "2F376B", "4A4837", "161616" }, new List<string>() { "Glass1", "Glass2", "Glass3", "Glass4", "Glass5", "Glass6", "Glass7", "Glass8" }, "Glasses Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Shoe", new List<string>() { "Shoes", "Shoes2", "Shoes3", "Shoes4" }, "Shoes", "Shoes", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "ShoeC", new List<string>() { "161616", "404041", "281C0E", "141D38", "343A28", "747063" }, new List<string>() { "Shoes", "Shoes2", "Shoes3", "Shoes4", "Belt" }, "Shoe Color"));

 
        // character 1 -- female
        mIdx = FEMALE;
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Hair", new List<string>() { "Hair1", "Hair2", "Hair3", "Hair4", "Hair5", "Hair6", "Hair7" }, "Hair", "Hair", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "HairC", new List<string>() { "161616", "35250D", "572c00", "935436", "775A1B", "c28340", "E6CC80", "9E9E9C" }, new List<string>() { "Hair1", "Hair2", "Hair3", "Hair4", "Hair5", "Hair6", "Hair7", "_Skin1/Brows1", "_Skin2/Brows2", "_Skin3/Brows3", "_Skin4/Brows4", "_Skin5/Brows5", "_Skin6/Brows6", "_Skin7/Brows7", "Eyebrows" }, "Hair Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.TEXTURE, "Eyes", new List<string>() { "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Brown", "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Hazel", "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Green", "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Blue" }, "Eyes", "Eye Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Skin", new List<string>() { "_Skin1", "_Skin2", "_Skin3", "_Skin4", "_Skin5", "_Skin6", "_Skin7" }, "Skin", "Face", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "SkinC", new List<string>() { "fcd7b5", "EBC19C", "f2c5b7", "B99261", "7E5F46", "48312A" }, new List<string>() { "_Skin1", "_Skin2", "_Skin3", "_Skin4", "_Skin5", "_Skin6", "_Skin7", "Hands" }, "Skin Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Shirt", new List<string>() { "Shirt1", "Shirt2", "Shirt3", "Shirt4", "Shirt5" }, "Shirt", "Shirt", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "ShirtC", new List<string>() { "E3E3E3", "A6A8AB", "A8A096", "7D8CBC", "293A6F", "161616", "D6D69F", "B39480", "640D0D", "BE9CA8", "A897B3", "8CA3FF", "658A90" }, new List<string>() { "Shirt1", "Shirt2", "Shirt3", "Shirt4", "Shirt5" }, "Shirt Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Jacket", new List<string>() { "", "Jacket1", "Jacket2", "Jacket3", "Jacket4", "Jacket5" }, "Jacket", "Jacket", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "JacketC", new List<string>() { "161616", "404041", "281C0E", "141D38", "343A28", "747063", "AAAAAA", "828200", "60302B", "490D0D", "925A6E", "6E5281", "4967C7", "0E4851" }, new List<string>() { "Jacket1", "Jacket2", "Jacket3", "Jacket4", "Jacket5" }, "Jacket Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Bottoms", new List<string>() { "Bottoms1", "Bottoms2", "Bottoms3" }, "Bottoms", "Bottoms", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "BottomsC", new List<string>() { "161616", "404041", "281C0E", "141D38", "343A28", "747063", "AAAAAA", "828200", "60302B", "490D0D", "925A6E", "6E5281", "4967C7", "0E4851" }, new List<string>() { "Bottoms1", "Bottoms2", "Bottoms3" }, "Bottoms Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "AddOn", new List<string>() { "", "Glass1", "Glass2", "Glass7", "Glass8", "Glass5", "Glass6", "Glass3", "Glass4" }, "Glass", "Glasses", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "AddOnC", new List<string>() { "CCB69D", "AB435B", "5E4773", "2F376B", "4A4837", "161616" }, new List<string>() { "Glass1", "Glass2", "Glass3", "Glass4", "Glass5", "Glass6", "Glass7", "Glass8" }, "Glasses Color"));
		AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Shoe", new List<string>() { "Shoes", "Shoes2", "Shoes3", "Shoes4", "Shoes5", "Shoes6" }, "Shoes", "Shoes", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "ShoeC", new List<string>() { "161616", "404041", "281C0E", "141D38", "343A28", "747063" }, new List<string>() { "Shoes", "Shoes2", "Shoes3", "Shoes4", "Shoes5", "Shoes6" }, "Shoe Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Jewelry", new List<string>() { "", "Acc1", "Acc2" }, "Jewelry", "Jewelry", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "JewelryMetalC", new List<string>() { "EEB111", "C0C0C0" }, new List<string>() { "Acc1", "Acc2" }, "Metal Color"));
		AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "JewelryStoneC", new List<string>() { "9B111E", "336600", "336699", "7D1242" }, new List<string>() { "Acc1", "Acc2" }, "Stone Color"));
		BuildOptionTypesList();
    }


    private void InitializeMedical()
    {
        // hard code city -- move this to an external database at some point!
        int mIdx;

        // character 0 -- male
        mIdx = MALE;
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Hair", new List<string>() { "Hair1", "Hair2", "Hair3", "Hair4", "Hair5", "" }, "Hair", "Hair", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "HairC", new List<string>() { "242424", "3A2F1E", "472400", "935436", "775A1B", "c28340", "E6CC80", "9E9E9C"}, new List<string>() { "Hair1", "Hair2", "Hair3", "Hair4", "Hair5", "_Skin1/Brows1", "_Skin2/Brows2", "_Skin3/Brows3", "_Skin4/Brows4", "_Skin5/Brows5", "_Skin6/Brows6", "_Skin7/Brows7", "FHair1", "FHair2", "FHair3", "FHair4", "Eyebrows" }, "Hair Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "hair", new List<string>() { "", "FHair1", "FHair2", "FHair3", "FHair4" }, "FHair", "Facial Hair", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.TEXTURE, "Eyes", new List<string>() { "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Brown", "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Hazel", "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Green", "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Blue" }, "Eyes", "Eye Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Skin", new List<string>() { "_Skin1", "_Skin2", "_Skin3", "_Skin4", "_Skin5", "_Skin6", "_Skin7" }, "Skin", "Face", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "SkinC", new List<string>() { "fcd7b5", "EBC19C", "f2c5b7", "B99261", "7E5F46", "48312A" }, new List<string>() { "_Skin1", "_Skin2", "_Skin3", "_Skin4", "_Skin5", "_Skin6", "_Skin7", "Hands" }, "Skin Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Shirt", new List<string>() { "Shirt1", "Shirt2", "Shirt3" }, "Shirt", "Shirt", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "ShirtC", new List<string>() { "E3E3E3", "A6A8AB", "A8A096", "7D8CBC", "293A6F", "242424" }, new List<string>() { "Shirt2", "Shirt3" }, "Shirt Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "TieC", new List<string>() { "242424", "5B0606", "2F4585", "4C553B", "5D5142" }, new List<string>() { "Shirt2/Tie1" }, "Tie Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Jacket", new List<string>() { "", "Jacket1"}, "Jacket", "Jacket", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "BottomsC", new List<string>() { "3EBC94", "cf98a8", "851f2f", "61a9bf", "0fbff2", "525266" }, new List<string>() { "Bottoms1", "Shirt1" }, "Bottoms Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "AddOn", new List<string>() { "", "Glass1", "Glass2", "Glass7", "Glass8", "Glass3", "Glass4", "Glass5", "Glass6" }, "Glass", "Glasses", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "AddOnC", new List<string>() { "CCB69D", "AB435B", "5E4773", "2F376B", "4A4837", "242424" }, new List<string>() { "Glass1", "Glass2", "Glass3", "Glass4", "Glass5", "Glass6", "Glass7", "Glass8" }, "Glasses Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Shoe", new List<string>() { "Shoes", "Shoes2", "Shoes3", "Shoes4" }, "Shoes", "Shoes", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "ShoeC", new List<string>() { "242424", "404041", "281C0E", "141D38", "343A28", "747063" }, new List<string>() { "Shoes", "Shoes2", "Shoes3", "Shoes4" }, "Shoe Color"));


        // character 1 -- female
        mIdx = FEMALE;
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Hair", new List<string>() { "Hair1", "Hair2", "Hair3", "Hair4", "Hair5", "Hair6", "Hair7" }, "Hair", "Hair", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "HairC", new List<string>() { "242424", "3A2F1E", "472400", "935436", "775A1B", "c28340", "E6CC80", "9E9E9C" }, new List<string>() { "Hair1", "Hair2", "Hair3", "Hair4", "Hair5", "Hair6", "Hair7", "_Skin1/Brows1", "_Skin2/Brows2", "_Skin3/Brows3", "_Skin4/Brows4", "_Skin5/Brows5", "_Skin6/Brows6", "_Skin7/Brows7", "Eyebrows" }, "Hair Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.TEXTURE, "Eyes", new List<string>() { "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Brown", "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Hazel", "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Green", "Avatars/Materials/Textures/Char_Fem_Tex_Eye_Blue" }, "Eyes", "Eye Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Skin", new List<string>() { "_Skin1", "_Skin2", "_Skin3", "_Skin4", "_Skin5", "_Skin6", "_Skin7" }, "Skin", "Face", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "SkinC", new List<string>() { "fcd7b5", "EBC19C", "f2c5b7", "B99261", "7E5F46", "48312A" }, new List<string>() { "_Skin1", "_Skin2", "_Skin3", "_Skin4", "_Skin5", "_Skin6", "_Skin7", "Hands" }, "Skin Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Shirt", new List<string>() { "Shirt1", "Shirt2", "Shirt3", "Shirt4", "Shirt5" }, "Shirt", "Shirt", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "ShirtC", new List<string>() { "E3E3E3", "A6A8AB", "A8A096", "7D8CBC", "293A6F", "242424", "D6D69F", "B39480", "640D0D", "BE9CA8", "A897B3", "8CA3FF", "658A90" }, new List<string>() { "Shirt2", "Shirt3", "Shirt4", "Shirt5" }, "Shirt Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Jacket", new List<string>() { "", "Jacket1" }, "Jacket", "Jacket", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "BottomsC", new List<string>() { "3EBC94", "cf98a8", "851f2f", "61a9bf", "0fbff2", "525266" }, new List<string>() { "Bottoms1", "Shirt1" }, "Bottoms Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "AddOn", new List<string>() { "", "Glass1", "Glass2", "Glass7", "Glass8", "Glass5", "Glass6", "Glass3", "Glass4" }, "Glass", "Glasses", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "AddOnC", new List<string>() { "CCB69D", "AB435B", "5E4773", "2F376B", "4A4837", "242424" }, new List<string>() { "Glass1", "Glass2", "Glass3", "Glass4", "Glass5", "Glass6", "Glass7", "Glass8" }, "Glasses Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Shoe", new List<string>() { "Shoes" }, "Shoes", "Shoes", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "ShoeC", new List<string>() { "242424", "404041", "281C0E", "141D38", "343A28", "747063" }, new List<string>() { "Shoes", "Shoes2", "Shoes3", "Shoes4" }, "Shoe Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.MESH, "Jewelry", new List<string>() { "", "Acc1", "Acc2" }, "Jewelry", "Jewelry", "", false));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "JewelryMetalC", new List<string>() { "EEB111", "C0C0C0" }, new List<string>() { "Acc1", "Acc2" }, "Metal Color"));
        AddOption(mIdx, new ResourceOptionList(ResourceType.COLOR, "JewelryStoneC", new List<string>() { "9B111E", "336600", "336699", "7D1242" }, new List<string>() { "Acc1", "Acc2" }, "Stone Color"));
        BuildOptionTypesList();
    }
    
    
    
    private void AddOption(int idx, ResourceOptionList newOption)
    {
        if( !avatarOptions.ContainsKey(idx) )
            avatarOptions[idx] = new Dictionary<string, ResourceOptionList>();
        avatarOptions[idx].Add(newOption.uniqueElementName, newOption);
    }

    private void BuildOptionTypesList()
    {
        foreach(KeyValuePair<int, Dictionary<string, ResourceOptionList>> optionList in Options)
        {
            optionTypes[optionList.Key] = new List<string>();
            foreach (KeyValuePair<string, ResourceOptionList> option in optionList.Value)
                optionTypes[optionList.Key].Add(option.Key);
        }
    }

    public void UpdateElement(GameObject avatarGO, int characterIdx, string uniqueElementName, int newResourceIdx)
    {
        ResourceOptionList resourceOptList;
        if (GetResourceOptionList(characterIdx, uniqueElementName, out resourceOptList))
        {
            newResourceIdx = newResourceIdx % resourceOptList.Count;
            if (resourceOptList.type == ResourceType.TEXTURE)
            {
                if (!SetNewMainTexture(avatarGO, resourceOptList.ElementName, (Texture2D)resourceOptList.GetResource(newResourceIdx)))
                {
                    // assume there is one renderer with several materials (man_biz is setup like this)
                    MaterialHelpers.SetNewTexture(avatarGO.GetComponentInChildren<SkinnedMeshRenderer>(), resourceOptList.materialIdx, (Texture2D)resourceOptList.GetResource(newResourceIdx));
                }
            }
            else if (resourceOptList.type == ResourceType.MESH)
            {
                SetNewModel(avatarGO, resourceOptList.ElementName, resourceOptList, newResourceIdx);
            }
            else if (resourceOptList.type == ResourceType.COLOR)
            {
                foreach (string addElem in resourceOptList.elementNameList)
                    SetNewColor(avatarGO, addElem, resourceOptList.GetColor(newResourceIdx));
            }
        }
        else
            Debug.LogError("Resource: " + uniqueElementName + " not found.");
    }

    bool GetResourceOptionList(int characterIdx, string uniqueElementName, out ResourceOptionList toList)
    {
        toList = null;
        if (!avatarOptions.ContainsKey(characterIdx) || !avatarOptions[characterIdx].TryGetValue(uniqueElementName, out toList))
            return false;
        return true;
    }

    public int NumOptions(int characterIdx, string name)
    {
        return (avatarOptions[characterIdx])[name].Count;
    }

    private bool SetNewColor(GameObject avatarGO, string elementName, Color newColor)
    {
        string parentElemName = elementName;
        string childElemName = elementName;
        int sepIdx = elementName.IndexOf('/');
        if (sepIdx != -1)
        {
            parentElemName = elementName.Substring(0, sepIdx);
            childElemName = elementName.Substring(sepIdx + 1);
        }
        Transform subMeshTransform = avatarGO.transform.Find(parentElemName);
        if (subMeshTransform != null && subMeshTransform.renderer == null)
            subMeshTransform = subMeshTransform.Find(childElemName); // look one layer down as well
        if (subMeshTransform == null || subMeshTransform.renderer == null)
            return false;
        subMeshTransform.renderer.material.color = newColor;
        return true;
    }

    private void SetNewModel(GameObject avatarGO, string elementName, ResourceOptionList meshOptList, int newModelIdx)
    {
        if (!meshOptList.loadFromDisk)
        {
            string modelNameWeWant = meshOptList.GetResourceName(newModelIdx);
            for (int i = 0; i < meshOptList.Count; ++i)
            {
                Transform subMeshTransform = avatarGO.transform.Find(meshOptList.GetResourceName(i));
                if (subMeshTransform != null)
                    subMeshTransform.gameObject.SetActive(subMeshTransform.name == modelNameWeWant);
                else
                    if( meshOptList.GetResourceName(i) != "" )
                        Debug.LogError("Did not find model: " + meshOptList.GetResourceName(i));
            }
        }
        else
        {
            Transform subMeshTransform = avatarGO.transform.Find(elementName);
            if (subMeshTransform == null)
            {
                Debug.LogError(elementName + " not found");
                return;
            }
            GameObject newMeshGO = (GameObject)meshOptList.GetResource(newModelIdx);
            subMeshTransform.renderer.enabled = newMeshGO != null;
            if (newMeshGO != null)
                subMeshTransform.gameObject.GetComponent<MeshFilter>().mesh = ((GameObject)meshOptList.GetResource(newModelIdx)).GetComponent<MeshFilter>().mesh;
        }
    }

    private bool SetNewMainTexture(GameObject avatarGO, string elementName, Texture2D newTexture)
    {
        Transform subMeshTransform = avatarGO.transform.Find(elementName);
        if (subMeshTransform == null || subMeshTransform.renderer == null)
            return false;
        subMeshTransform.renderer.material.SetTexture("_MainTex", newTexture);
        return true;
    }

    private int GetRandomResourceIdx(int modelIdx, string uniqueName)
    {
        int randomIdx = random.Next(0, NumOptions(modelIdx, uniqueName));
        return randomIdx;
    }

    // Keep person looking as normal and matchy as possible
    private void StyleCheck(Player player, int modelIdx, string uniqueElementName, Dictionary<string, int> avatarOptions, ref int newResourceIdx)
    {

        // keep female clothing colors to a toned down pallete.
        if (modelIdx == 1)
        {
            if ((newResourceIdx > 5 || newResourceIdx == 4) && uniqueElementName == "JacketC" || uniqueElementName == "BottomsC" || uniqueElementName == "ShirtC")
                newResourceIdx = random.Next(0, 4);

            // Jacket 2 doesn't work with Shirt 3 at the moment.
            if( uniqueElementName == "Jacket" && newResourceIdx == 2 )
                newResourceIdx = random.Next(0, 1);
        }


        // don't give dark skinned people light hair.
        // defining hair, check skin
        int skinC = 0;
        if (uniqueElementName == "HairC" && avatarOptions.TryGetValue("SkinC", out skinC) && skinC > 3)
        {
            // stick to the darker hairs
            newResourceIdx = random.Next(0, 3);
        }

        // defining skin, check hair
        int hairC = 0;
        if (uniqueElementName == "SkinC" && newResourceIdx > 3 && avatarOptions.TryGetValue("HairC", out hairC) && hairC > 2)
        {
            // change hair
            hairC = random.Next(0, 3);
            if( player != null )
                UpdateElement(player.gameObject, modelIdx, "HairC", hairC);
            avatarOptions["HairC"] = hairC;
        }


        // dark shoes
        if ((newResourceIdx > 2) && uniqueElementName == "ShoeC")
            newResourceIdx = random.Next(0, 3);

        // no glasses
        if (uniqueElementName == "AddOn")
            newResourceIdx = 0;
    }

    public string CreateRandomAvatarJSON(int characterIdx)
    {
        Dictionary<string, int> avatarOptions = new Dictionary<string, int>();
        foreach (KeyValuePair<string, ResourceOptionList> option in Options[characterIdx])
        {
            int newResourceIdx = GetRandomResourceIdx(characterIdx, option.Value.uniqueElementName);
            StyleCheck(null, characterIdx, option.Value.uniqueElementName, avatarOptions, ref newResourceIdx);
            avatarOptions[option.Value.uniqueElementName] = newResourceIdx;
        }
        return AvatarOptionsToJson(avatarOptions);
    }

    public void CreateRandomAvatar(Player player, bool saveState)
    {
        Dictionary<string, int> avatarOptions = new Dictionary<string, int>();
        CreateRandomAvatar(player, avatarOptions, saveState);
    }

    public void CreateRandomAvatar(Player player, Dictionary<string, int> avatarOptions, bool saveState)
    {
        int characterIdx = player.ModelIdx;
        foreach (KeyValuePair<string, ResourceOptionList> option in Options[characterIdx])
        {
            int newResourceIdx = GetRandomResourceIdx(characterIdx, option.Value.uniqueElementName);
            StyleCheck(player, characterIdx, option.Value.uniqueElementName, avatarOptions, ref newResourceIdx);
            UpdateElement(player.gameObject, characterIdx, option.Value.uniqueElementName, newResourceIdx);
            avatarOptions[option.Value.uniqueElementName] = newResourceIdx;
        }

        if (saveState)
            SaveStateToServer(player, avatarOptions);
    }

    public string AvatarOptionsToJson(Dictionary<string, int> avatarOptions)
    {
        return JSHelpers.DictionaryToJSON(avatarOptions);
    }

    public string AvatarOptionsToString(Dictionary<string, int> avatarOptions)
    {
        string customizationStr = "";
        int gender = GameManager.Inst.LocalPlayer.ModelIdx;
        foreach (string option in AvatarOptionManager.Inst.OptionTypes[gender])
        {
            if (customizationStr != "")
                customizationStr += ",";
            customizationStr += (avatarOptions.ContainsKey(option) ? avatarOptions[option].ToString() : CommunicationManager.CurrentUserProfile.GetField(option));
        }
        return customizationStr;
    }

    public void SaveStateToServer(Player player, Dictionary<string, int> avatarOptions)
    {
        Debug.LogError("SaveStateToServer: " + PlayerPrefs.GetString("VirbelaAvatarOpt"));
        // Guests can save a local copy to the registry
        if( CommunicationManager.CurrentUserProfile.IsGuest())
        {
            PlayerPrefs.SetString("VirbelaAvatarOpt", AvatarOptionsToString(avatarOptions));
            Debug.LogError("Saving avatar to registry: " + PlayerPrefs.GetString("VirbelaAvatarOpt"));
            return;
        }
        if (!CommunicationManager.CurrentUserProfile.CheckLogin())
        {
            Debug.LogError("Current profile not logged in?");
            return;
        }
        int characterIdx = player.ModelIdx;
        string jsonStr = AvatarOptionsToJson(avatarOptions);
        if( jsonStr != "" )
            CommunicationManager.CurrentUserProfile.UpdateProfile(jsonStr);
    }
}

