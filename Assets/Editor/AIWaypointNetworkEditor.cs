using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

[CustomEditor(typeof(AIWaypointNetwork))]
public class AIWaypointNetworkEditor : Editor {

    public override void OnInspectorGUI()
    {
        // target is the reference to the opject which the editor script relates to
        // We need to CAST it as its type (AIWaypointNetwork)
        AIWaypointNetwork waypointNetwork = (AIWaypointNetwork)target;

        waypointNetwork.displayMode
            = (PathDisplayMode)EditorGUILayout.EnumPopup("Display Mode", waypointNetwork.displayMode);

        if (waypointNetwork.displayMode == PathDisplayMode.Paths)
        {
            waypointNetwork.UIStart = EditorGUILayout.IntSlider
                ("Waypoint Start", waypointNetwork.UIStart, 0, waypointNetwork.waypoints.Count - 1);
            waypointNetwork.UIEnd = EditorGUILayout.IntSlider
                ("Waypoint End", waypointNetwork.UIEnd, 0, waypointNetwork.waypoints.Count - 1);
        }

        DrawDefaultInspector();
    }

    private void OnSceneGUI()
    {
        // target is the reference to the opject which the editor script relates to
        // We need to CAST it as its type (AIWaypointNetwork)
        AIWaypointNetwork waypointNetwork = (AIWaypointNetwork)target;
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;

        for (int i = 0; i < waypointNetwork.waypoints.Count; i++)
        {
            if (waypointNetwork.waypoints[i] != null)
            {
                // Add label to waypoint 
                Handles.Label(waypointNetwork.waypoints[i].position, "Waypoint " + i, style);
            }
        }

        if (waypointNetwork.displayMode == PathDisplayMode.Connections)
        {
            DisplayConnections(waypointNetwork);
        }
        else if (waypointNetwork.displayMode == PathDisplayMode.Paths)
        {
            DisplayNavmeshPath(waypointNetwork);
        }

    }

    private void DisplayConnections(AIWaypointNetwork waypointNetwork)
    {
        // Draw lines connecting waypoints
        Vector3[] linePoints = new Vector3[waypointNetwork.waypoints.Count + 1];

        for (int i = 0; i <= waypointNetwork.waypoints.Count; i++)
        {
            // the last iteration is used to refer back to the first waypoint (0)
            int index = 0;
            if (i != waypointNetwork.waypoints.Count) { index = i; }

            // build array of linepoints to link with lines. First and last will be 0
            if (waypointNetwork.waypoints[index] != null)
            {
                linePoints[i] = waypointNetwork.waypoints[index].position;
            }
            else  // Make empty array item obvious 
            {
                linePoints[i] = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            }
        }
        Handles.color = Color.cyan;
        Handles.DrawPolyLine(linePoints);
    }

    private void DisplayNavmeshPath(AIWaypointNetwork waypointNetwork)
    {
        NavMeshPath path = new NavMeshPath();

        if (waypointNetwork.waypoints[waypointNetwork.UIStart] != null &&
           waypointNetwork.waypoints[waypointNetwork.UIEnd] != null)
        {
            Vector3 from = waypointNetwork.waypoints[waypointNetwork.UIStart].position;
            Vector3 to = waypointNetwork.waypoints[waypointNetwork.UIEnd].position;

            NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);
            // draw line connecting all points on the returned path
            Handles.color = Color.yellow;
            Handles.DrawPolyLine(path.corners);
        }
    }
}
