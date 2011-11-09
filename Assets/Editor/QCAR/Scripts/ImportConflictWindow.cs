/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using UnityEditor;
using UnityEngine;

public class ImportConflictWindow : EditorWindow
{
    #region PRIVATE_MEMBER_VARIABLES

    private GUIStyle mWindowStyle = null;
    private bool mCleanClose = false;
    private const string WARNING_MESSAGE =
                        "Warning: You imported new data files over existing " +
                        "ones. If you proceed your current copies of " +
                        "config.xml and qcar-resources.dat will be replaced.";

    #endregion // PRIVATE_MEMBER_VARIABLES



    #region PUBLIC_METHODS

    // This method needs to be called to instantiate the Window object and
    // draw a Window on the screen.
    public static void ShowWindow()
    {
        // Get existing open window or if none, make a new one:
        ImportConflictWindow instance =
            EditorWindow.GetWindow<ImportConflictWindow>(true,
                                                         "Import Conflict",
                                                         true);
        int width = 250;
        int height = 50;
        instance.position = new Rect((Screen.width - width) / 2,
                                     (Screen.height + height) / 2, 250, 50);

        instance.mCleanClose = false;

        instance.ShowPopup();
    }

    #endregion // PUBLIC_METHODS



    #region UNITY_EDITORWINDOW_METHODS

    // OnGUI is called when GUI actions happen on the Window.
    public void OnGUI()
    {
        this.mWindowStyle = new GUIStyle(GUI.skin.label);
        this.mWindowStyle.wordWrap = true;
        GUILayout.Label(WARNING_MESSAGE, mWindowStyle, null);

        GUILayout.BeginHorizontal(GUI.skin.label, null);
        if (GUILayout.Button("Ok"))
        {
            // Replace existing file with newly imported one.
            TargetDataPostprocessor.HandleFileReplacements(true);
            mCleanClose = true;
            this.Close();
        }
        if (GUILayout.Button("Cancel"))
        {
            // Do not replace the existing file.
            TargetDataPostprocessor.HandleFileReplacements(false);
            mCleanClose = true;
            this.Close();
        }
        GUILayout.EndHorizontal();
    }


    // OnDestroy is called when Window is closed.
    public void OnDestroy()
    {
        // If user has not chosen how to handle the file replacement the
        // it is assumed that the old file should not be replaced.
        if (!mCleanClose)
            TargetDataPostprocessor.HandleFileReplacements(false);
    }

    #endregion // UNITY_EDITORWINDOW_METHODS
}