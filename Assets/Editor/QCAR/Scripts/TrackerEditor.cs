/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using UnityEditor;

[CustomEditor(typeof(TrackerBehaviour))]
public class TrackerEditor : Editor
{
    #region UNITY_EDITOR_METHODS

    // OnInspectorGUI exposes public Tracker settings in Inspector
    // WorldCenterMode: Defines how the relative transformation that is returned
    //                  by the QCAR Tracker is applied. Either the camera is
    //                  moved in the scene with respect to a "world center" or
    //                  all the targets are moved with respect to the camera.
    public override void OnInspectorGUI()
    {
        TrackerBehaviour tb = (TrackerBehaviour) target;

        DrawDefaultInspector();

        tb.SetWorldCenterMode((TrackerBehaviour.WorldCenterMode)
                EditorGUILayout.EnumPopup("World Center Mode",
                tb.WorldCenterModeSetting));

// We assume Unity 3.2 as minimum requirement.
#if (UNITY_3_3 || UNITY_3_2)
        if (tb.WorldCenterModeSetting == TrackerBehaviour.WorldCenterMode.USER)
        {
            tb.SetWorldCenter((TrackableBehaviour)
                EditorGUILayout.ObjectField("World Center", tb.WorldCenter,
                typeof(TrackableBehaviour)));
        }
#else
        bool allowSceneObjects = !EditorUtility.IsPersistent(target);
        if (tb.WorldCenterModeSetting == TrackerBehaviour.WorldCenterMode.USER)
        {
            tb.SetWorldCenter((TrackableBehaviour)
                EditorGUILayout.ObjectField("World Center", tb.WorldCenter,
                typeof(TrackableBehaviour),
                allowSceneObjects));
        }        
#endif
    }

    #endregion // UNITY_EDITOR_METHODS
}
