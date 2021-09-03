using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class NodeBaseEditor : EditorWindow
{
    public NodeData currentData, lastData;
    private List<Node> nodes;
    private List<Connection> connections;

    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle inPointStyle;
    private GUIStyle outPointStyle;
    private GUIStyle resizerStyle;

    private ConnectionPoint selectedInPoint;
    private ConnectionPoint selectedOutPoint;

    private Rect inspectorPanel, editorPanel, resizer;

    private Vector2 drag, offset;

    private float sizeRatio = 0.3f;
    private bool isResizing;

    [MenuItem("Window/Node Based Editor")]
    private static void OpenWindow()
    {
        NodeBaseEditor window = GetWindow<NodeBaseEditor>();
        window.titleContent = new GUIContent("Node Based Editor");
    }

    private void OnEnable()
    {
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
        nodeStyle.border = new RectOffset(12, 12, 12, 12);

        selectedNodeStyle = new GUIStyle();
        selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
        selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);

        inPointStyle = new GUIStyle();
        inPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left.png") as Texture2D;
        inPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left on.png") as Texture2D;
        inPointStyle.border = new RectOffset(4, 4, 12, 12);

        outPointStyle = new GUIStyle();
        outPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right.png") as Texture2D;
        outPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right on.png") as Texture2D;
        outPointStyle.border = new RectOffset(4, 4, 12, 12);

        resizerStyle = new GUIStyle();
        resizerStyle.normal.background = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;
    }

    private void OnDisable()
    {
        AssetDatabase.SaveAssets();
    }

    private void OnGUI()
    {
        DrawInspector();
        DrawNodeEditor();
        DrawResizer();

        ProcessEvents(Event.current);
        if (GUI.changed)
        {
            Repaint();
        }
    }

    private void DrawNodes()
    {
        if (nodes != null)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i].Draw();
            }
        }
    }

    private void DrawConnections()
    {
        if (connections != null)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                connections[i].Draw();
            }
        }
    }

    private void DrawNodeEditor()
    {
        editorPanel = new Rect(position.width * sizeRatio, 0f, position.width * (1f - sizeRatio), position.height);
        GUILayout.BeginArea(editorPanel);
        GUILayout.Label("Node Editor");
        DrawGrid(20f, 0.2f, Color.gray);
        DrawGrid(100f, 0.4f, Color.gray);

        DrawNodes();
        DrawConnections();

        ProcessNodeEvents(Event.current);
        ProcessAllContextMenu(Event.current);
        DrawConnectionLine(Event.current);
        GUILayout.EndArea();
    }

    private void DrawInspector()
    {
        inspectorPanel = new Rect(0f, 0f, position.width * sizeRatio, position.height);

        GUILayout.BeginArea(inspectorPanel);
        GUILayout.Label("Inspector");
        EditorGUI.BeginChangeCheck();
        currentData = (NodeData)EditorGUILayout.ObjectField(currentData, typeof(NodeData), false);
        if (EditorGUI.EndChangeCheck())
        {
            if (currentData != null)
            {
                AssetDatabase.SaveAssets();

                if (lastData != null && lastData != currentData)
                {
                    ClearNodes();
                    RedrawNodes();
                }
                else
                    RedrawNodes();
            }
            else
                ClearNodes();

            lastData = currentData;
        }
        if (nodes != null)
        {
            foreach (var item in nodes)
            {
                if (item.node.isSelected)
                {
                    if (GUILayout.Button("Add Output"))
                    {
                        item.AddOut(outPointStyle, OnClickOutPoint);
                    }

                    if (item.outPoint.Count > 1)
                    {
                        for (int i = 1; i < item.outPoint.Count; i++)
                        {
                            if (GUILayout.Button("Remove Output " + (i + 1)))
                            {
                                OnClickRemoveConnection(GetConnectionClass(currentData.connections, item.outPoint[i]));
                                item.RemoveOutPoint(item.outPoint[i]);
                            }
                        }
                    }
                }
            }
        }

        GUILayout.EndArea();
    }

    private void DrawResizer()
    {
        resizer = new Rect((position.width * sizeRatio) - 5f, 0f, 10f, position.height);

        GUILayout.BeginArea(new Rect(resizer.position + (Vector2.right * 5f), new Vector2(2f, position.height)), resizerStyle);
        GUILayout.EndArea();

        EditorGUIUtility.AddCursorRect(resizer, MouseCursor.ResizeHorizontal);
    }

    private void OnValidate()
    {
        if (nodeStyle == null)
            OnEnable();

        if (currentData != null)
        {
            RedrawNodes();
        }
        else
            ClearNodes();
    }

    private void ProcessEvents(Event e)
    {
        drag = Vector2.zero;

        switch(e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && !resizer.Contains(e.mousePosition))
                {
                    ClearConnectionSelection(true, true);
                }
                else if (e.button == 0 && resizer.Contains(e.mousePosition))
                {
                    isResizing = true;
                }
                EditorUtility.SetDirty(currentData);
                break;
            case EventType.MouseDrag:
                if (e.button == 2)
                {
                    OnDrag(e.delta);
                }
                break;
            case EventType.MouseUp:
                isResizing = false;
                break;
        }

        Resize(e);
    }

    private void ProcessAllContextMenu(Event e)
    {
        switch(e.type)
        {
            case EventType.MouseDown:
                if (e.button == 1)
                {
                    ProcessContextMenu(e.mousePosition);
                }
                break;
        }
    }

    private void Resize(Event e)
    {
        if (isResizing)
        {
            sizeRatio = e.mousePosition.x / position.width;
            Repaint();
        }
    }

    private void OnDrag(Vector2 delta)
    {
        drag = delta;

        if (nodes != null)
        {
            foreach (var item in nodes)
            {
                item.Drag(delta);
            }
        }

        GUI.changed = true;
    }

    private void DrawConnectionLine(Event e)
    {
        Vector2 positionInEditor = e.mousePosition;
        if (selectedInPoint != null && selectedOutPoint == null)
        {
            Handles.DrawBezier(
                selectedInPoint.rect.center,
                positionInEditor,
                selectedInPoint.rect.center + Vector2.left * 50f,
                positionInEditor - Vector2.left * 50f,
                Color.white,
                null,
                2f
            );

            GUI.changed = true;
        }

        if (selectedInPoint == null && selectedOutPoint != null)
        {
            Handles.DrawBezier(
                selectedOutPoint.rect.center,
                positionInEditor,
                selectedOutPoint.rect.center - Vector2.left * 50f,
                positionInEditor + Vector2.left * 50f,
                Color.white,
                null,
                2f
            );

            GUI.changed = true;
        }
    }

    private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
    {
        int widthDiv = Mathf.CeilToInt(position.width / gridSpacing);
        int heightDiv = Mathf.CeilToInt(position.height / gridSpacing);

        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);
        offset += drag * 0.5f;
        Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0f);

        for (int i = 0; i < widthDiv; i++)
        {
            Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0f) + newOffset, new Vector3(gridSpacing * i, position.height, 0f) + newOffset);
        }

        for (int j = 0; j < heightDiv; j++)
        {
            Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0f) + newOffset, new Vector3(position.width, gridSpacing * j, 0f) + newOffset);
        }

        Handles.color = Color.white;
        Handles.EndGUI();
    }

    private void ProcessNodeEvents(Event e)
    {
        if (nodes != null)
        {
            for (int i = nodes.Count - 1; i >= 0 ; i--)
            {
                bool guiChanged = nodes[i].ProcessEvents(e);

                if (guiChanged)
                {
                    GUI.changed = true;
                }
            }
        }
    }

    private void ProcessContextMenu(Vector2 mousePosition)
    {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Add Node"), false, delegate { OnClickAddNode(mousePosition); });
        genericMenu.ShowAsContext();
    }

    private void OnClickAddNode(Vector2 mousePosition)
    {
        if (nodes == null)
        {
            nodes = new List<Node>();
        }

        if (currentData.nodes == null)
        {
            currentData.nodes = new List<NodeClass>();
        }

        NodeClass temp = new NodeClass();
        temp.id = Guid.NewGuid().ToString();
        currentData.nodes.Add(temp);
  
        nodes.Add(new Node(temp, mousePosition, 200, 50, nodeStyle, selectedNodeStyle, 
            inPointStyle, outPointStyle, OnClickInPoint, OnClickOutPoint, OnClickRemoveNode));
    }

    private void OnClickRemoveNode(Node node)
    {
        if (connections != null)
        {
            List<Connection> connectionsToRemove = new List<Connection>();

            for (int i = 0; i < connections.Count; i++)
            {
                for (int j = 0; j < node.outPoint.Count; j++)
                {
                    if (connections[i].inPoint == node.inPoint || connections[i].outPoint == node.outPoint[j])
                    {
                        connectionsToRemove.Add(connections[i]);
                    }
                }
            }

            for (int i = 0; i < connectionsToRemove.Count; i++)
            {
                connections.Remove(connectionsToRemove[i]);
                currentData.connections.RemoveAt(i);
            }
        }

        nodes.Remove(node);
        currentData.nodes.Remove(node.node);
    }

    private void OnClickInPoint(ConnectionPoint inPoint)
    {
        if (selectedInPoint != null)
            ClearConnectionSelection(true);

        selectedInPoint = inPoint;

        if (selectedOutPoint != null)
        {
            if (selectedOutPoint.node != selectedInPoint.node)
            {
                ConnectionClass prevConn = GetConnectionClass(currentData.connections, selectedOutPoint);

                if (prevConn != null)
                    OnClickRemoveConnection(prevConn);

                CreateConnection();
                ClearConnectionSelection(false, true);
            }
            else
            {
                ClearConnectionSelection(false);
            }
        }
    }

    private void OnClickOutPoint(ConnectionPoint outPoint)
    {
        if (selectedOutPoint != null)
            ClearConnectionSelection(false);

        selectedOutPoint = outPoint;

        if (selectedInPoint != null)
        {
            if (selectedOutPoint.node != selectedInPoint.node)
            {
                CreateConnection();
                ClearConnectionSelection(false, true);
            }
            else
            {
                ClearConnectionSelection(true);
            }
        }
    }

    void OnClickRemoveConnection(ConnectionClass connection)
    {
        int idToRemove = -1;

        for (int i = 0; i < connections.Count; i++)
        {
            if (connections[i].connection == connection)
                idToRemove = i;
        }

        if (idToRemove > -1)
        {
            connections.RemoveAt(idToRemove);
            currentData.connections.RemoveAt(idToRemove);
        }
    }

    void CreateConnection()
    {
        if (connections == null)
        {
            connections = new List<Connection>();
        }

        if (currentData.connections == null)
        {
            currentData.connections = new List<ConnectionClass>();
        }

        ConnectionClass newConn = new ConnectionClass();
        newConn.nodeInGuid = selectedInPoint.guid;
        newConn.nodeOutGuid = selectedOutPoint.guid;
        currentData.connections.Add(newConn);
        connections.Add(new Connection(newConn, selectedInPoint, selectedOutPoint, OnClickRemoveConnection));
    }

    void ClearConnectionSelection(bool inPoint, bool all = false)
    {
        if (inPoint || all)
        {
            selectedInPoint?.ClearSelection();
            selectedInPoint = null;
        }
        if (!inPoint || all)
        {
            selectedOutPoint?.ClearSelection();
            selectedOutPoint = null;
        }
    }

    void RedrawNodes()
    {
        if (currentData == null)
            return;

        foreach (var item in currentData.nodes)
        {
            if (nodes == null)
            {
                nodes = new List<Node>();
            }

            nodes.Add(new Node(item, item.rect.position, 200, 50, nodeStyle, selectedNodeStyle,
            inPointStyle, outPointStyle, OnClickInPoint, OnClickOutPoint, OnClickRemoveNode));

            //Debug.LogFormat("node out count: {0}", item.outPoint.Count);
        }

        foreach (var item in currentData.connections)
        {
            if (connections == null)
            {
                connections = new List<Connection>();
            }

            ConnectionPoint inPoint = GetConnectionPointByGuid(item.nodeInGuid);
            ConnectionPoint outPoint = GetConnectionPointByGuid(item.nodeOutGuid, true);
            connections.Add(new Connection(item, inPoint, outPoint, OnClickRemoveConnection));
        }
    }

    void ClearNodes()
    {
        nodes?.Clear();
        connections?.Clear();
    }

    private ConnectionPoint GetConnectionPointByGuid(string guid, bool Out = false)
    {
        foreach (var item in nodes)
        {
            if (!Out)
            {

                if (string.Equals(item.inPoint.guid, guid))
                {
                    //Debug.LogFormat("node in guid: {0}", item.node.inPointId);
                    return item.inPoint;
                }
            }
            else
            {
                foreach (var outPoint in item.outPoint)
                {

                    if (string.Equals(outPoint.guid, guid))
                    {
                        //Debug.LogFormat("node out guid: {0}", outPoint.guid);
                        return outPoint;
                    }
                }
            }
        }

        return null;
    }
    
    private ConnectionClass GetConnectionClass(List<ConnectionClass> connections, ConnectionPoint outPoint)
    {
        if (connections != null)
        {
            foreach (var item in connections)
            {
                if (item.nodeOutGuid != null && string.Equals(item.nodeOutGuid, outPoint.guid))
                    return item;
            }
        }

        return null;
    }
}
