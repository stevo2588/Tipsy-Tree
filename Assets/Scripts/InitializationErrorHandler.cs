/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using UnityEngine;

// A custom handler that implements the IQCARErrorHandler interface.
[RequireComponent(typeof(TrackerBehaviour))]
public class InitializationErrorHandler : MonoBehaviour, IQCARErrorHandler
{
    #region PRIVATE_MEMBER_VARIABLES

    private string mErrorText = "";
    private bool mErrorOccurred = false;

    private const string WINDOW_TITLE = "QCAR Initialization Error";

    #endregion // PRIVATE_MEMBER_VARIABLES



    #region PUBLIC_METHODS

    // Implementation of the IQCARErrorHandler function which sets the
    // error message.
    public void SetErrorCode(TrackerBehaviour.InitError errorCode)
    {
        switch (errorCode)
        {
            case TrackerBehaviour.InitError.INIT_CANNOT_DOWNLOAD_DEVICE_SETTINGS:
                mErrorText =
                      "Network connection required to initialize camera " +
                      "settings. Please check your connection and restart " +
                      "the application. If you are still experiencing " +
                      "problems, then your device may not be currently " +
                      "supported.";
                break;
            case TrackerBehaviour.InitError.INIT_DEVICE_NOT_SUPPORTED:
                mErrorText =
                      "Failed to initialize QCAR because this device is not " +
                      "supported.";

                break;
            case TrackerBehaviour.InitError.INIT_ERROR:
                mErrorText = "Failed to initialize QCAR.";
                break;
        }    
    }


    // Implementation of the IQCARErrorHandler function which sets if an
    // error has been thrown.
    public void SetErrorOccurred(bool errorOccurred)
    {
        mErrorOccurred = errorOccurred;

        // We set the clear mode of the camera to solid. Otherwise the Window is
        // messed up.
        if (errorOccurred)
            this.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
    }

    #endregion // PUBLIC_METHODS



    #region UNTIY_MONOBEHAVIOUR_METHODS

    void Start()
    {
        // We register IQCARErrorHandler functions with the TrackerBehaviour.
        this.GetComponent<TrackerBehaviour>().
            RegisterInitializationErrorHandler(this);
    }


    void OnGUI()
    {
        // On error, create a full screen window.
        if (mErrorOccurred)
            GUI.Window(0, new Rect(0, 0, Screen.width, Screen.height),
                                   DrawWindowContent, WINDOW_TITLE);
    }

    #endregion // UNTIY_MONOBEHAVIOUR_METHODS



    #region PRIVATE_METHODS

    private void DrawWindowContent(int id)
    {
        // Create text area with a 10 pixel distance from other controls and
        // window border.
        GUI.Label(new Rect(10, 25, Screen.width - 20, Screen.height - 95),
                    mErrorText);

        // Create centered button with 50/50 size and 10 pixel distance from
        // other controls and window border.
        if (GUI.Button(new Rect(Screen.width / 2 - 75, Screen.height - 60,
                                    150, 50), "Close"))
            Application.Quit();
    }

    #endregion // PRIVATE_METHODS
}
