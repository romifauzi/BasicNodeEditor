using System;
using UnityEngine;

public enum ConnectionPointType { In, Out }

public class ConnectionPoint
{
    public string guid;
    public Rect rect;
    public ConnectionPointType type;
    public NodeClass node;
    public GUIStyle style, currentStyle;
    public Action<ConnectionPoint> OnClickConnectionPoint;

    public ConnectionPoint(NodeClass node, ConnectionPointType type, GUIStyle style, Action<ConnectionPoint> OnClickConnectionPoint)
    {
        this.node = node;
        this.type = type;
        this.style = style;
        currentStyle = new GUIStyle();
        currentStyle.normal = style.normal;
        this.OnClickConnectionPoint = OnClickConnectionPoint;
        this.rect = new Rect(0f, 0f, 10f, 20f);
        guid = Guid.NewGuid().ToString();
    }

    public void Draw(int id)
    {
        switch(type)
        {
            case ConnectionPointType.In:
                rect.x = node.rect.x - rect.width + 8f;
                rect.y = (node.rect.y + (node.rect.height * 0.5f)) - (rect.height * 0.5f);
                break;
            case ConnectionPointType.Out:
                rect.x = node.rect.x + node.rect.width - 8f;
                rect.y = node.rect.min.y + 50 * id + (50 * 0.5f) - (rect.height * 0.5f);
                break;
        }

        if (GUI.Button(rect, "", currentStyle))
        {
            currentStyle.normal = style.active;

            OnClickConnectionPoint?.Invoke(this);
        }
    }

    public void ClearSelection()
    {
        currentStyle.normal = style.normal;
    }
}
