/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using UnityEditor;
using UnityEngine;

public class QCARSettingsWindow : EditorWindow
{
    #region PRIVATE_MEMBER_VARIABLES

    private GUIStyle mWindowStyle = null;
    private const string INFO_MESSAGE =
                        "Info: If enabled (default) Marker synchronization " +
                        "will cause Markers to be removed from the " +
                        "config.xml file if they don't exist in the scene. " +
                        "If deactivated Markers will be kept in the " +
                        "config.xml file until they are manually removed " +
                        "from the file or until synchronization is activated " +
                        "again.";

    #endregion // PRIVATE_MEMBER_VARIABLES



    #region PUBLIC_METHODS

    // This method needs to be called to instantiate the Window object and
    // draw a Window on the screen.
    public static void ShowWindow()
    {
        // Get existing open window or if none, make a new one:
        QCARSettingsWindow instance =
            EditorWindow.GetWindow<QCARSettingsWindow>(true,
                                                       "QCAR Advanced Settings",
                                                       true);
        int width = 410;
        int height = 150;
        instance.position =
            new Rect((Screen.width - width) / 2,
                     (Screen.height + height) / 2,
                     width,
                     height);

        instance.ShowPopup();
    }

    #endregion // PUBLIC_METHODS



    #region UNITY_EDITORWINDOW_METHODS

    // OnGUI is called when GUI actions happen on the Window.
    public void OnGUI()
    {
        this.mWindowStyle = new GUIStyle(GUI.skin.label);
        this.mWindowStyle.wordWrap = true;
        GUILayout.Label(INFO_MESSAGE, mWindowStyle, null);

        SceneManager.Instance.SyncMarkersSceneAndConfig =
            GUILayout.Toggle(SceneManager.Instance.SyncMarkersSceneAndConfig,
                             "Marker Synchronization");
        if (GUILayout.Button("Reset"))
        {
            SceneManager.Instance.SyncMarkersSceneAndConfig = true;
        }
        if (GUILayout.Button("Ok"))
        {
            this.Close();
        }
    }

    #endregion // UNITY_EDITORWINDOW_METHODS



    #region PRIVATE_METHODS

    [MenuItem("CONTEXT/MarkerBehaviour/Advanced Settings")]
    private static void Init()
    {
        ShowWindow();
    }

    #endregion // PRIVATE_METHODS
}