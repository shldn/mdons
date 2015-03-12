using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum ResourceType
{
    TEXTURE = 0,
    MESH = 1,
    COLOR = 2
}

public class ResourceOptionList
{
    public ResourceType type;
    public string uniqueElementName; // name in database, some elements need multiple option lists, so this is the unique name in the database, can be different than the actual mesh/material name
    public int materialIdx;
    public string animationName;
    public string displayName;
    List<UnityEngine.Object> resourceInstances;
    List<string> resourceNames;
    public List<string> elementNameList = new List<string>(); // all the elements to change: materialName or meshName, this could be the same for multiple elements if you want to change both it's mesh and material
    public bool loadFromDisk; // special case for the meshes, if false assume that all mesh options are loaded, and switching between them is a matter of enable/disable flags

    public ResourceOptionList(ResourceType type_, string uniqueElementName_, List<string> resourceNames_, List<string> elementNames_, string displayName_ = "", string animationName_ = "", bool loadFromDisk_ = true, int materialIdx_ = 0)
    {
        elementNameList.AddRange(elementNames_);
        if (elementNames_.Count == 0)
            elementNames_[0] = "";
        Init(type_, uniqueElementName_, resourceNames_, elementNames_[0], displayName_, animationName_, loadFromDisk_, materialIdx_);
    }

    public ResourceOptionList(ResourceType type_, string uniqueElementName_, List<string> resourceNames_, string elementName_="", string displayName_ = "", string animationName_ = "", bool loadFromDisk_ = true, int materialIdx_ = 0)
    {
        Init(type_, uniqueElementName_, resourceNames_, elementName_, displayName_, animationName_, loadFromDisk_, materialIdx_); 
    }

    private void Init(ResourceType type_, string uniqueElementName_, List<string> resourceNames_, string elementName_ = "", string displayName_ = "", string animationName_ = "", bool loadFromDisk_ = true, int materialIdx_ = 0)
    {
        type = type_;
        uniqueElementName = uniqueElementName_;
        resourceNames = resourceNames_;
        string elementName = (elementName_ == "") ? uniqueElementName : elementName_;
        displayName = (displayName_ == "") ? uniqueElementName : displayName_;
        animationName = animationName_;
        loadFromDisk = loadFromDisk_;
        materialIdx = materialIdx_;
        if (elementNameList.Count == 0)
            elementNameList.Add(elementName);
        elementNameList[0] = elementName;
        resourceInstances = new List<UnityEngine.Object>(resourceNames.Count);
        for (int i = 0; i < resourceNames.Count; ++i)
            resourceInstances.Add(null);
    }

    public UnityEngine.Object GetResource(int idx)
    {
        if (resourceInstances[idx] == null && resourceNames[idx] != "")
            resourceInstances[idx] = Resources.Load(resourceNames[idx]);
        return resourceInstances[idx];
    }

    public string GetResourceName(int idx)
    {
        if (idx < 0 || idx >= resourceNames.Count)
            return "";
        return resourceNames[idx];
    }

    public Color GetColor(int idx)
    {
        if( type != ResourceType.COLOR )
            return Color.magenta;
        return MathHelper.HexToColor(resourceNames[idx]);
    }

    public int Count { get { return resourceNames.Count; } }
    public string ElementName { get { return elementNameList[0]; } }

}
