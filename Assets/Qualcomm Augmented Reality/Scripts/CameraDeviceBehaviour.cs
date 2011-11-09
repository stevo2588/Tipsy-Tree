/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class CameraDeviceBehaviour : MonoBehaviour
{
    #region NESTED

    public enum FocusMode
    {
        FOCUS_MODE_AUTO,        // Default focus mode
        FOCUS_MODE_FIXED,       // Fixed focus mode
        FOCUS_MODE_INFINITY,    // Focus set to infinity
        FOCUS_MODE_MACRO        // Macro mode for close up focus
    }

    #endregion // NESTED



    #region PRIVATE_MEMBERS

    private Dictionary<Image.PIXEL_FORMAT, Image> mCameraImages;

    #endregion // PRIVATE_MEMBERS

    

    #region PUBLIC_METHODS

    // Activate or deactivate the camera device flash.
    // Returns false if flash is not available or can't be activated.
    public bool SetFlashTorchMode(bool on)
    {
        return (cameraDeviceSetFlashTorchMode(on ? 1 : 0) != 0);
    }


    // Start the auto focus (using the current mode).
    // Returns false if auto focus is not available or can't be activated.
    public bool StartAutoFocus()
    {
        return (cameraDeviceStartAutoFocus() != 0);
    }


    // Stop the auto focus.
    // Returns false if auto focus is not available or can't be activated.
    public bool StopAutoFocus()
    {
        return (cameraDeviceStopAutoFocus() != 0);
    }


    // Set an auto focus mode.
    // Returns false if auto focus is not available or can't be activated.
    public bool SetAutoFocusMode(FocusMode mode)
    {
        return (cameraDeviceSetAutoFocusMode((int)mode) != 0);
    }


    // Enables or disables the request of the camera image in the desired pixel
    // format. Returns true on success, false otherwise. Note that this may
    // result in processing overhead. Image are accessed using GetCameraImage.
    // Note that there may be a delay of several frames until the camera image
    // becomes availables.
    public bool SetFrameFormat(Image.PIXEL_FORMAT format, bool enabled)
    {
        if (enabled)
        {
            if (!mCameraImages.ContainsKey(format))
            {
                if (qcarSetFrameFormat((int)format, 1) == 0)
                {
                    Debug.LogError("Failed to set frame format");
                    return false;
                }

                Image newImage = new Image();
                newImage.PixelFormat = format;
                mCameraImages.Add(format, newImage);
                return true;
            }
        }
        else
        {
            if (mCameraImages.ContainsKey(format))
            {
                if (qcarSetFrameFormat((int)format, 0) == 0)
                {
                    Debug.LogError("Failed to set frame format");
                    return false;
                }

                return mCameraImages.Remove(format);
            }
        }

        return true;
    }


    // Returns a camera images for the requested format. Returns null if
    // this image is not available. You must call SetFrameFormat before
    // accessing the corresponding camera image.
    public Image GetCameraImage(Image.PIXEL_FORMAT format)
    {
        // Has the format been requested:
        if (mCameraImages.ContainsKey(format))
        {
            // Check the image is valid:
            Image image = mCameraImages[format];
            if (image.IsValid())
            {
                return image;
            }
        }

        // No valid image of this format:
        return null;
    }


    // Returns the container of all requested images. The images may or may 
    // not be initialized. Please use GetCameraImage for a list of
    // available and valid images. Used only by the TrackerBehaviour.
    public Dictionary<Image.PIXEL_FORMAT, Image> GetAllImages()
    {
        return mCameraImages;
    }

    #endregion // PUBLIC_METHODS



    #region CONSTRUCTION

    public CameraDeviceBehaviour()
    {
        mCameraImages = new Dictionary<Image.PIXEL_FORMAT, Image>();
    }

    #endregion // CONSTRUCTION



    #region NATIVE_FUNCTIONS

#if !UNITY_EDITOR

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int cameraDeviceSetFlashTorchMode(int on);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int cameraDeviceStartAutoFocus();

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int cameraDeviceStopAutoFocus();

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int cameraDeviceSetAutoFocusMode(int focusMode);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int qcarSetFrameFormat(int format, int enabled);

#else

    private static int cameraDeviceSetFlashTorchMode(int on) { return 1; }

    
    private static int cameraDeviceStartAutoFocus() { return 1; }


    private static int cameraDeviceStopAutoFocus() { return 1; }


    private static int cameraDeviceSetAutoFocusMode(int focusMode) { return 1; }


    private static int qcarSetFrameFormat(int format, int enabled) { return 1; }

#endif

    #endregion // NATIVE_FUNCTIONS
}
