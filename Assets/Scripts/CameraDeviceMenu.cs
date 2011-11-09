/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using UnityEngine;
using System.Collections;
using System;

[RequireComponent(typeof(CameraDeviceBehaviour))]
public class CameraDeviceMenu : MonoBehaviour
{
    #region NESTED

    // Defines which menu to show.
    private enum MenuMode
    {
        MENU_OFF,            // Do not show a menu (default).
        MENU_CAMERA_OPTIONS, // Show camera device options.
        MENU_FOCUS_MODES     // Show focus mode menu.
    }

    #endregion // NESTED



    #region PRIVATE_MEMBER_VARIABLES

    // Currently active menu.
    private MenuMode mMenuToShow = MenuMode.MENU_OFF;
    // Check if a menu button has been pressed.
    private bool mButtonPressed = false;
    // Check if flash is currently enabled.
    private bool mFlashEnabled = false;
    // Check if auto focus is currently enabled.
    private bool mAutoFocusEnabled = false;
    // Contains the currently set auto focus mode.
    private CameraDeviceBehaviour.FocusMode mAutoFocusMode =
        CameraDeviceBehaviour.FocusMode.FOCUS_MODE_AUTO;
    // Contains the rectangle for the camera options menu.
    private Rect mAreaRect;

    #endregion // PRIVATE_MEMBER_VARIABLES



    #region UNTIY_MONOBEHAVIOUR_METHODS

    public void Start()
    {
        // Setup position and size of the camera menu.
        int areaWidth = Screen.width;
        int areaHeight = Screen.height / 5;
        int areaLeft = 0;
        int areaTop = Screen.height - areaHeight;
        mAreaRect = new Rect(areaLeft, areaTop, areaWidth, areaHeight);
    }


    public void Update()
    {
        // If the touch event results from a button press it is ignored.
        if (!mButtonPressed)
        {
            // If finger is removed from screen.
            if (Input.GetMouseButtonUp(0))
            {
                // If menu is not rendered.
                if (mMenuToShow == MenuMode.MENU_OFF)
                {
                    // Show menu.
                    mMenuToShow = MenuMode.MENU_CAMERA_OPTIONS;
                }
                // If menu is already open.
                else
                {
                    // Close menu
                    mMenuToShow = MenuMode.MENU_OFF;
                }
            }
        }
        else
        {
            mButtonPressed = false;
        }
    }


    // Draw menus.
    public void OnGUI()
    {
        switch (mMenuToShow)
        {
            case MenuMode.MENU_CAMERA_OPTIONS:
                DrawMenu();
                break;

            case MenuMode.MENU_FOCUS_MODES:
                DrawFocusModes();
                break;

            default:
                break;
        }
    }

    #endregion // UNTIY_MONOBEHAVIOUR_METHODS



    #region PRIVATE_METHODS

    // Draw menu to control camera device.
    private void DrawMenu()
    {
        // Setup style for buttons.
        GUIStyle buttonGroupStyle = new GUIStyle(GUI.skin.button);
        buttonGroupStyle.stretchWidth = true;
        buttonGroupStyle.stretchHeight = true;

        GUILayout.BeginArea(mAreaRect);

        GUILayout.BeginHorizontal(buttonGroupStyle);

        // Turn flash on or off.
        if (GUILayout.Button("Toggle Flash", buttonGroupStyle))
        {
            if (!mFlashEnabled)
            {
                // Turn on flash if it is currently disabled.
                this.GetComponent<CameraDeviceBehaviour>().SetFlashTorchMode(true);
                mFlashEnabled = true;
            }
            else
            {
                // Turn off flash if it is currently enabled.
                this.GetComponent<CameraDeviceBehaviour>().SetFlashTorchMode(false);
                mFlashEnabled = false;
            }

            mMenuToShow = MenuMode.MENU_OFF;
            mButtonPressed = true;
        }
        // Turn auto focus on or off.
        if (GUILayout.Button("Autofocus", buttonGroupStyle))
        {
            if (!mAutoFocusEnabled)
            {
                // Turn on flash if it is currently disabled.
                this.GetComponent<CameraDeviceBehaviour>().StartAutoFocus();
                mAutoFocusEnabled = true;
            }
            else
            {
                // Turn off flash if it is currently enabled.
                this.GetComponent<CameraDeviceBehaviour>().StopAutoFocus();
                mAutoFocusEnabled = false;
            }

            mMenuToShow = MenuMode.MENU_OFF;
            mButtonPressed = true;
        }
        // Choose focus mode.
        if (GUILayout.Button("Focus Modes", buttonGroupStyle))
        {
            mMenuToShow = MenuMode.MENU_FOCUS_MODES;
            mButtonPressed = true;
        }

        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }


    // Draw menu to let user choose a focus mode.
    private void DrawFocusModes()
    {
        CameraDeviceBehaviour.FocusMode newMode;
        newMode = EnumOptionList(mAutoFocusMode);

        // We set the new value only if the mode has changed.
        if (newMode != mAutoFocusMode)
        {
            this.GetComponent<CameraDeviceBehaviour>().SetAutoFocusMode(newMode);
            mAutoFocusMode = newMode;

            mMenuToShow = MenuMode.MENU_OFF;
            mButtonPressed = true;
        }
    }


    // Helper function to automatically create an option list of an enum object.
    private static CameraDeviceBehaviour.FocusMode EnumOptionList(
                                    CameraDeviceBehaviour.FocusMode setMode)
    {
        Type modeType = setMode.GetType();

        // Get possible enum values.
        CameraDeviceBehaviour.FocusMode[] modes =
            (CameraDeviceBehaviour.FocusMode[])Enum.GetValues(modeType);

        // Setup style for list.
        GUIStyle optionListStyle = new GUIStyle(GUI.skin.button);
        optionListStyle.stretchHeight = true;
        optionListStyle.stretchWidth = true;

        // Setup style for toggles.
        // We use "button" style as template because default toggles are too
        // small.
        GUIStyle toggleStyle = new GUIStyle(GUI.skin.button);
        toggleStyle.stretchHeight = true;
        toggleStyle.stretchWidth = true;
        toggleStyle.normal.textColor = Color.gray;
        toggleStyle.onNormal.textColor = Color.gray;
        toggleStyle.focused.textColor = Color.gray;
        toggleStyle.onFocused.textColor = Color.gray;
        toggleStyle.active.textColor = Color.gray;
        toggleStyle.onActive.textColor = Color.gray;
        toggleStyle.hover.textColor = Color.gray;
        toggleStyle.onHover.textColor = Color.gray;

        // Setup style for active toggle.
        // Setting active values for the toggle Style does not work so we create
        // another style.
        GUIStyle activeToggleStyle = new GUIStyle(toggleStyle);
        activeToggleStyle.normal.textColor = Color.white;
        activeToggleStyle.onNormal.textColor = Color.white;
        activeToggleStyle.focused.textColor = Color.white;
        activeToggleStyle.onFocused.textColor = Color.white;
        activeToggleStyle.active.textColor = Color.white;
        activeToggleStyle.onActive.textColor = Color.white;
        activeToggleStyle.hover.textColor = Color.white;
        activeToggleStyle.onHover.textColor = Color.white;


        CameraDeviceBehaviour.FocusMode newMode = setMode;

        // We render the menu over the full screen.
        GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));

        GUILayout.BeginVertical();

        foreach (CameraDeviceBehaviour.FocusMode mode in modes)
        {
            if (mode == setMode)
            {
                GUILayout.Toggle(true, mode.ToString(), activeToggleStyle);
            }
            else
            {
                if (GUILayout.Toggle(false, mode.ToString(), toggleStyle))
                {
                    newMode = mode;
                }
            }
        }

        GUILayout.EndVertical();

        GUILayout.EndArea();

        return newMode;
    }

    #endregion // PRIVATE_METHODS
}
