using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeData", menuName = "Node Data")]
public class NodeData : ScriptableObject
{
    public List<NodeClass> nodes = new List<NodeClass>();
    public List<ConnectionClass> connections = new List<ConnectionClass>();
}

[Serializable]
public class NodeClass
{
    public string id;
    public Rect rect;
    public string title;
    public bool isDragged;
    public bool isSelected;

    public string inPointId;
    public List<string> outPointId = new List<string>();
}

[Serializable]
public class ConnectionClass
{
    public string nodeInGuid;
    public string nodeOutGuid;
}
