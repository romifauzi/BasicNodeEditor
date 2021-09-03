using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Node
{
    public NodeClass node;

    public ConnectionPoint inPoint;
    public List<ConnectionPoint> outPoint = new List<ConnectionPoint>();

    public GUIStyle style;
    public GUIStyle defaultNodeStyle, selectedNodeStyle;

    public Action<Node> OnRemoveNode;

    public float height;

    public Node(NodeClass data, Vector2 position, float width, float height, GUIStyle nodeStyle, GUIStyle selectedStyle, GUIStyle inPointStyle,
        GUIStyle outPointStyle, Action<ConnectionPoint> OnClickInPoint, Action<ConnectionPoint> OnClickOutPoint, Action<Node> OnClickRemoveNode)
    {
        node = data;
        node.isSelected = false;
        node.rect = new Rect(position.x, position.y, width, height);
        style = nodeStyle;
        
        inPoint = new ConnectionPoint(node, ConnectionPointType.In, inPointStyle, OnClickInPoint);

        if (string.IsNullOrEmpty(node.inPointId))
            node.inPointId = inPoint.guid;
        else
            inPoint.guid = node.inPointId;
        
        if (node.outPointId.Count == 0)
        {
            node.outPointId = new List<string>();
            outPoint = new List<ConnectionPoint>();

            outPoint.Add(new ConnectionPoint(node, ConnectionPointType.Out, outPointStyle, OnClickOutPoint));
            node.outPointId.Add(outPoint[0].guid);
        }
        else
        {
            outPoint = new List<ConnectionPoint>();

            for (int i = 0; i < node.outPointId.Count; i++)
            {
                outPoint.Add(new ConnectionPoint(node, ConnectionPointType.Out, outPointStyle, OnClickOutPoint));

                outPoint[i].guid = node.outPointId[i];
            }
        }

        defaultNodeStyle = nodeStyle;
        selectedNodeStyle = selectedStyle;
        OnRemoveNode = OnClickRemoveNode;
        this.height = height;
    }

    public void Drag(Vector2 delta)
    {
        node.rect.position += delta;
    }

    public void Draw()
    {
        inPoint.Draw(0);
        for (int i = 0; i < outPoint.Count; i++)
        {
            outPoint[i].Draw(i);
        }

        node.rect.height = height * (outPoint.Count);
        GUI.Box(node.rect, node.title, style);
    }

    public bool ProcessEvents(Event e)
    {
        switch(e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    if (node.rect.Contains(e.mousePosition))
                    {
                        node.isDragged = true;
                        GUI.changed = true;
                        node.isSelected = true;
                        style = selectedNodeStyle;
                    }
                    else
                    {
                        GUI.changed = true;
                        node.isSelected = false;
                        style = defaultNodeStyle;
                    }
                }
                if (e.button == 1 && node.isSelected && node.rect.Contains(e.mousePosition))
                {
                    ProcessContextMenu();
                    e.Use();
                }
                break;
            case EventType.MouseUp:
                node.isDragged = false;
                break;
            case EventType.MouseDrag:
                if (e.button == 0 && node.isDragged)
                {
                    Drag(e.delta);
                    e.Use();
                    return true;
                }
                break;
        }
        return false;
    }

    private void ProcessContextMenu()
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Remove Node"), false, OnClickRemoveNode);
        genericMenu.ShowAsContext();
    }

    private void OnClickRemoveNode()
    {
        OnRemoveNode?.Invoke(this);
    }

    public void AddOut(GUIStyle outPointStyle, Action<ConnectionPoint> OnClickOutPoint)
    {
        ConnectionPoint newOutNode = new ConnectionPoint(node, ConnectionPointType.Out, outPointStyle, OnClickOutPoint);
        outPoint.Add(newOutNode);
        node.outPointId.Add(newOutNode.guid);
    }

    public void RemoveOutPoint(ConnectionPoint outPoint)
    {
        node.outPointId.Remove(outPoint.guid);
        this.outPoint.Remove(outPoint);
    }
}
