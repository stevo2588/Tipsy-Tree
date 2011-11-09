/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using UnityEditor;

public class QCARHelpMenu : Editor
{
    #region PUBLIC_METHODS

    // Method opens up a browser Window with the specified URL.
    // This method is called when "Qualcomm AR Documentation" is chosen from the
    // Unity "Help" menu.
    [MenuItem("Help/Qualcomm AR Documentation")]
    public static void browseQCARHelp()
    {
        System.Diagnostics.Process.Start(
            "http://ar.qualcomm.com/unity/sdk/ar/");
    }

    #endregion PUBLIC_METHODS
}
