/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;

// The TrackerBehaviour class handles tracking and triggers native video
// background rendering. The class updates all Trackables in the scene.
[RequireComponent(typeof(Camera))]
public class TrackerBehaviour : MonoBehaviour
{
    #region NESTED

    // InitError is an error value that is returned by QCAR if something goes
    // wrong at initialization.
    public enum InitError
    {
        // Device settings could not be downloaded.
        INIT_CANNOT_DOWNLOAD_DEVICE_SETTINGS = -3,
        // Device is not supported by QCAR.
        INIT_DEVICE_NOT_SUPPORTED = -2,
        // Another (unknown) initialization errors has occured.
        INIT_ERROR = -1
    }

    // The world center mode defines how the relative coordinates between
    // Trackables and camera are translated into Unity world coordinates.
    // If a world center is present the virtual camera in the Unity scene is
    // transformed with respect to that.
    // The world center mode is set through the Unity inspector.
    public enum WorldCenterMode
    {
        // User defines a single Trackable that defines the world center.
        USER,
        // Tracker uses the first Trackable that comes into view as the world
        // center (world center changes during runtime).
        AUTO,
        // Do not define a world center but only move Trackables with respect
        // to a fixed camera.
        NONE
    }

    // The mode used for camera capturing and video rendering.
    // The camera device mode is set through the Unity inspector.
    public enum CameraDeviceMode
    {
        // Best compromise between speed and quality.
        MODE_DEFAULT = -1,
        // Optimize for speed. Quality of the video background could suffer.
        MODE_OPTIMIZE_SPEED = -2,
        // Optimize for quality. Application performance could go down.
        MODE_OPTIMIZE_QUALITY = -3
    }

    // This struct defines the 2D coordinates of a rectangle. It is used for
    // rectangular Virtual Button definitions.
    // The struct is used internally by the VirtualButtonBehaviour.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RectangleData
    {
        public float leftTopX;
        public float leftTopY;
        public float rightBottomX;
        public float rightBottomY;
    }

    // State of the camera.
    private enum CameraState
    {
        UNINITED,        // Camera is not yet initialized.
        DEVICE_INITED,   // Camera device is initialized.
        RENDERING_INITED // Video rendering is initialized.
    }

    // Hints are used to push the tracker into certain behavior. Hints (as the
    // name suggests) are not always guaranteed to work.
    private enum QCARHint
    {
        // Specify the number of Image Targets that are handled by the tracker
        // at once.
        HINT_MAX_SIMULTANEOUS_IMAGE_TARGETS = 0,
        // Detection of Trackables is continued in the next frame if they could
        // not be found within a certain time interval.
        HINT_IMAGE_TARGET_MULTI_FRAME_ENABLED = 1,
        // Specifies the maximum time the detector should look for Trackables.
        HINT_IMAGE_TARGET_MILLISECONDS_PER_MULTI_FRAME = 2,
    }

    // Defines pixel formats that are supported by the QCAR extension.
    private enum PixelFormat
    {
        UNKNOWN_FORMAT = 0, // Unknown pixel format.
        RGB565 = 1,         // RGB565 format (5 bits red, 6 green, 5 blue).
        RGB888 = 2,         // RGB888 format (8 bits red, 8 green, 8 blue).
        GRAYSCALE = 4,      // Grayscale image format.
        YUV = 8             // YUV format (separated color and luminance channels).
    }

    // This struct stores 3D pose information as a position-vector,
    // orientation-Quaternion pair. The pose is given relatively to the camera.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PoseData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public Vector3 position;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public Quaternion orientation;
    }

    // This struct stores general Trackable data like its 3D pose, its status
    // and its unique id.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TrackableData
    {
        public PoseData pose;
        public TrackableBehaviour.Status status;
        public int id;
    }

    // This struct stores Virtual Button data like its current status (pressed
    // or not pressed) and its unique id.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct VirtualButtonData
    {
        public int id;
        public int isPressed;
    }

    // This struct stores 2D integer vectors.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Vec2I
    {
        public int x;
        public int y;
        
        public Vec2I(int v1, int v2)
        {
            x = v1;
            y = v2;
        }
    }

    // This struct stores Video Background configuration data. It stores if
    // background rendering is enabled, if it happens synchronously and it
    // stores position and size of the video background on the screen.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct VideoBGCfgData
    {
        public int enabled;
        public int synchronous;
        public Vec2I position;
        public Vec2I size;
    }

    // This struct stores video mode data. This includes the width and height of
    // the frame and the framerate of the camera.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct VideoModeData
    {
        public int width;
        public int height;
        public float frameRate;
    }

    // This struct stores data of an image header. It includes the width and
    // height of the image, the byte stride in the buffer, the buffer size
    // (which can differ from the image size e.g. when image is converted to a
    // power of two size) and the format of the image
    // (e.g. RGB565, grayscale, etc.).
   [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ImageHeaderData
    {
        public int width;
        public int height;
        public int stride;
        public int bufferWidth;
        public int bufferHeight;
        public int format;
        public int reallocate;
        public int updated;
        public IntPtr data;
    }

    #endregion // NESTED



    #region PROPERTIES

    // This property is used to query the active world center mode.
    public WorldCenterMode WorldCenterModeSetting
    {
        get { return mWorldCenterMode; }
    }

    // This property is used to query the world center Trackable
    // (will return null in "NONE" mode).
    public TrackableBehaviour WorldCenter
    {
        get { return mWorldCenter; }
    }

    #endregion // PROPERTIES



    #region PRIVATE_MEMBER_VARIABLES

    [SerializeField]
    private CameraDeviceMode CameraDeviceModeSetting =
                    CameraDeviceMode.MODE_DEFAULT;

    [SerializeField]
    private int MaxSimultaneousImageTargets = 1;

    // split detection of multiple targets over multiple frames
    [SerializeField]
    private bool MultiFrameEnabled = true;

    // tie the framerate to the camera framerate
    [SerializeField]
    private bool SynchronousVideo = false;

    [SerializeField]
    [HideInInspector]
    private WorldCenterMode mWorldCenterMode = WorldCenterMode.AUTO;
    [SerializeField]
    [HideInInspector]
    private TrackableBehaviour mWorldCenter = null;

    private List<IQCARErrorHandler> mErrorHandlers =
                    new List<IQCARErrorHandler>();
    private List<ITrackerEventHandler> mTrackerEventHandlers =
                    new List<ITrackerEventHandler>();

    private bool mIsInitialized = false;

    private CameraDeviceBehaviour mCameraDevice;
    private CameraState mCameraState = CameraState.UNINITED;
    private Color mClearColor = new Color(0, 0, 0, 0);
    private Material mClearMaterial;
    private Rect mViewportRect;
    private int mClearBuffers;

    private int mAbsoluteNumTrackables;
    private int mAbsoluteNumVirtualButtons;
    private IntPtr mTrackablePtr = IntPtr.Zero;
    private IntPtr mVirtualButtonPtr = IntPtr.Zero;
    private TrackableData[] mTrackableDataArray;

    private Dictionary<int, List<TrackableBehaviour>> mTrackableBehaviourDict =
                    new Dictionary<int, List<TrackableBehaviour>>();

    private Dictionary<int, VirtualButtonBehaviour> mVBBehaviourDict =
                    new Dictionary<int, VirtualButtonBehaviour>();
    
    private List<TrackableBehaviour> mTrackableFoundQueue =
                    new List<TrackableBehaviour>();

    private bool mVBDataArrayDirty = false;

    private IntPtr mImageHeaderData = IntPtr.Zero;
    private int mNumImageHeaders = 0;

    private bool mHasStartedOnce = false;

    #endregion // PRIVATE_MEMBER_VARIABLES



    #region PUBLIC_METHODS

    // This method registers a new error handler at the Tracker.
    // Initialization error handlers are used to display error messages if
    // something goes wrong during the initialization of the native part of the
    // QCAR extension.
    public void RegisterInitializationErrorHandler(
                                IQCARErrorHandler errorHandler)
    {
        mErrorHandlers.Add(errorHandler);
    }


    // This method unregisters a given error handler.
    // Returns false if the error handler does not exist.
    public bool UnregisterInitializationErrorHandler(
                                IQCARErrorHandler errorHandler)
    {
        return mErrorHandlers.Remove(errorHandler);
    }

    // This method registers a new Tracker event handler at the Tracker.
    // These handlers are called as soon as ALL Trackables have been updated
    // in this frame.
    public void RegisterTrackerEventHandler(
                                ITrackerEventHandler trackerEventHandler)
    {
        mTrackerEventHandlers.Add(trackerEventHandler);
    }


    // This method unregisters a Tracker event handler.
    // Returns "false" if event handler does not exist.
    public bool UnregisterTrackerEventHandler(
                                ITrackerEventHandler trackerEventHandler)
    {
        return mTrackerEventHandlers.Remove(trackerEventHandler);
    }


    // Registers a Virtual Button at native code. This method is called
    // implicitly by the ImageTargetBehaviour.CreateVirtualButton method. This
    // method should not be called by user code.
    public bool RegisterVirtualButton(VirtualButtonBehaviour vb,
                                        string imageTargetName)
    {
        TrackerBehaviour.RectangleData rectData =
                new TrackerBehaviour.RectangleData();

        Vector2 leftTop, rightBottom;
        vb.CalculateButtonArea(out leftTop, out rightBottom);
        rectData.leftTopX = leftTop.x;
        rectData.leftTopY = leftTop.y;
        rectData.rightBottomX = rightBottom.x;
        rectData.rightBottomY = rightBottom.y;

        IntPtr rectPtr = Marshal.AllocHGlobal(
            Marshal.SizeOf(typeof(TrackerBehaviour.RectangleData)));
        Marshal.StructureToPtr(rectData, rectPtr, false);

        bool registerWorked =
            (createVirtualButton(imageTargetName, vb.VirtualButtonName,
                                    rectPtr) != 0);

        if (registerWorked)
        {
            int id = virtualButtonGetId(imageTargetName, vb.VirtualButtonName);

            // Initialize the id of the button:
            vb.InitializeID(id);

            // Check we don't have an entry for this id:
            if (!mVBBehaviourDict.ContainsKey(id))
            {
                // Add:
                mVBBehaviourDict.Add(id, vb);
                mVBDataArrayDirty = true;
            }
        }

        return registerWorked;
    }


    // Unregister a Virtual Button at native code. This method is called
    // implicitly by the ImageTargetBehaviour.DestroyVirtualButton method. This
    // method should not be called by user code.
    public bool UnregisterVirtualButton(VirtualButtonBehaviour vb,
                                            string imageTargetName)
    {
        int id = virtualButtonGetId(imageTargetName, vb.VirtualButtonName);

        bool unregistered = false;

        if (destroyVirtualButton(imageTargetName, vb.VirtualButtonName) != 0)
        {
            if (mVBBehaviourDict.Remove(id))
            {
                unregistered = true;
                mVBDataArrayDirty = true;
            }
        }

        if (!unregistered)
        {
            Debug.LogError("UnregisterVirtualButton: Failed to destroy " + 
                            "the Virtual Button.");
        }

        return unregistered;
    }


    // This method is used to set the world center mode in the Unity editor.
    // Switching modes is not supported at runtime.
    public void SetWorldCenterMode(WorldCenterMode value)
    {
        if (!Application.isEditor)
            return;

        mWorldCenterMode = value;
    }


    // This method is used to set the world center in the Unity editor in
    // "USER" mode. Switching modes is not supported at runtime.
    public void SetWorldCenter(TrackableBehaviour value)
    {
        if (!Application.isEditor)
            return;

        mWorldCenter = value;
    }

    #endregion // PUBLIC_METHODS



    #region UNITY_MONOBEHAVIOUR_METHODS

    // Starts up the QCAR extension with the properties that were set in the
    // Unity inspector.
    void Start()
    {
        ResetCameraClearFlags();
        mClearMaterial = new Material(Shader.Find("Diffuse"));

        mCameraDevice = (CameraDeviceBehaviour)FindObjectOfType(
                                        typeof(CameraDeviceBehaviour));

        qcarSetHint((int) QCARHint.HINT_MAX_SIMULTANEOUS_IMAGE_TARGETS,
                            MaxSimultaneousImageTargets);
        qcarSetHint((int) QCARHint.HINT_IMAGE_TARGET_MULTI_FRAME_ENABLED,
                            MultiFrameEnabled ? 1 : 0);
        SetUnityVersion();

        StartQCAR();

        InitializeTrackables();
        InitializeTrackableContainer();

        InitializeVirtualButtons();
        InitializeVirtualButtonContainer();

        mHasStartedOnce = true;
    }


    // Restart the camera and tracker if the TrackerBehaviour is reenabled.
    // Note that we check specifically that Start() has been called once
    // where QCAR is fully initialized.
    void OnEnable()
    {
        if (mHasStartedOnce)
        {
            cameraDeviceInitCamera();
            ConfigureVideoBackground();
            cameraDeviceSelectVideoMode((int)CameraDeviceModeSetting);
            cameraDeviceStartCamera();
            startTracker();
        }
    }

    // Updates the scene with new tracking data. Calls registered
    // ITrackerEventHandlers
    void Update()
    {
        if (!mIsInitialized)
            return;

        if (isRendererDirty() == 1)
        {
            ConfigureVideoBackground();
            InitializeProjection();
        }

        mClearMaterial.SetPass(0);

        // Reinitialize the virtual button data container if required:
        if (mVBDataArrayDirty)
        {
            InitializeVirtualButtonContainer();
        }

        UpdateImageContainer();

        updateQCAR(mTrackablePtr, mAbsoluteNumTrackables,
            mVirtualButtonPtr, mAbsoluteNumVirtualButtons,
            mImageHeaderData, mNumImageHeaders,
            (int)Screen.orientation);

        GL.InvalidateState();

        UpdateCameraClearFlags();

        UpdateCameraFrame();

        UpdateTrackables();
        UpdateVirtualButtons();

        foreach (ITrackerEventHandler handler in mTrackerEventHandlers)
        {
            handler.OnTrackablesUpdated();
        }
    }


    // Sets the viewport right before Unity renders 3D content.
    void OnPreRender()
    {
        // Don't set the viewport when running in the editor:
        if (!Application.isEditor)
        {
            GL.Viewport(mViewportRect);
        }
    }

    void OnPostRender()
    {
        // Clear the framebuffer as many times as defined upon init
        if (mClearBuffers > 0)
        {
            GL.Clear(false, true, new Color(0.0f, 0.0f, 0.0f, 1.0f));
            mClearBuffers--;
        }
    }

    // Stops QCAR when the application is sent to the background.
    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            StopQCAR();
            GL.Clear(false, true, new Color(0.0f, 0.0f, 0.0f, 1.0f));
        }
        else
        {
            StartQCAR();
            // Clear any artifacts from the buffers on resume
            mClearBuffers = 8;
        }
    }


    // Stop the tracker and camera when TrackerBehaviour is disabled.
    void OnDisable()
    {
        stopTracker();
        cameraDeviceStopCamera();
        ResetCameraClearFlags();
        cameraDeviceDeinitCamera();
    }


    // Shuts down QCAR and frees unmanaged buffers when the TrackerBehaviour
    // is destroyed:
    void OnDestroy()
    {
        Debug.Log("OnDestroy");
        StopQCAR();
        ResetCameraClearFlags();

        Marshal.FreeHGlobal(mTrackablePtr);
        Marshal.FreeHGlobal(mVirtualButtonPtr);
        Marshal.FreeHGlobal(mImageHeaderData);
    }

    #endregion // UNITY_MONOBEHAVIOUR_METHODS



    #region PRIVATE_METHODS

    private void StartQCAR()
    {
        print("StartQCAR");
        
        // First we check if QCAR initialized correctly
        int errorCode = getInitErrorCode();
        mIsInitialized = false;
        if (!HandleInitializationError(errorCode))
        {
            mIsInitialized = false;
            return;
        }

        cameraDeviceInitCamera();
        ConfigureVideoBackground();
        cameraDeviceSelectVideoMode((int) CameraDeviceModeSetting);
        cameraDeviceStartCamera();
        startTracker();

        // Skip the initalization of the projection matrix when running in the
        // editor:
        if (!Application.isEditor)
        {
            InitializeProjection();
        }

        mIsInitialized = true;
    }


    private void StopQCAR()
    {
        print("StopQCAR");

        stopTracker();
        cameraDeviceStopCamera();
        cameraDeviceDeinitCamera();
    }


    // HandleInitializationError returns false if the error code was "fatal"
    private bool HandleInitializationError(int errorCode)
    {
        // First we check if QCAR initialized correctly
        if (errorCode < 0)
        {
            // If no error handle has been registered we just quit.
            if (mErrorHandlers.Count <= 0)
            {
                print("Error: Failed to initialize QCAR.");
                Application.Quit();
            }

            // If an error handler has been registered it needs to handle
            // application quit as well.
            foreach (IQCARErrorHandler errorHandler in mErrorHandlers)
            {
                errorHandler.SetErrorCode((TrackerBehaviour.InitError)
                                            errorCode);
                errorHandler.SetErrorOccurred(true);
            }

            return false;
        }
        return true;
    }


    private void ResetCameraClearFlags()
    {
        this.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        this.GetComponent<Camera>().backgroundColor = mClearColor;
        mCameraState = CameraState.UNINITED;
        mClearBuffers = 8;
    }


    private void UpdateCameraClearFlags()
    {
        // Specifically handle when running in the editor:
        if (Application.isEditor)
        {
            this.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            this.GetComponent<Camera>().backgroundColor = mClearColor;
            mCameraState = CameraState.UNINITED;

            return;
        }

        // Update camera clear flags if necessary
        switch (mCameraState)
        {
            case CameraState.UNINITED:
                mCameraState = CameraState.DEVICE_INITED;
                break;

            case CameraState.DEVICE_INITED:
                // Check whether QCAR requires a transparent clear color
                if (qcarRequiresAlpha() == 0)
                {
                    // Clear only depth
                    this.GetComponent<Camera>().clearFlags = CameraClearFlags.Depth;
                }
                else
                {
                    // Camera clears both depth and color buffer,
                    // We set the clear color to transparent black as
                    // required by QCAR.
                    this.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
                }

                mCameraState = CameraState.RENDERING_INITED;                
                break;
        }
    }


    private void ConfigureVideoBackground()
    {
        IntPtr configPtr = Marshal.AllocHGlobal(
                            Marshal.SizeOf(typeof(VideoBGCfgData)));
        rendererGetVideoBackgroundCfg(configPtr);
        VideoBGCfgData config = (VideoBGCfgData) Marshal.PtrToStructure
                            (configPtr, typeof(VideoBGCfgData));
        Marshal.FreeHGlobal(configPtr);

        IntPtr videoModePtr = Marshal.AllocHGlobal(
                            Marshal.SizeOf(typeof(VideoModeData)));
        cameraDeviceGetVideoMode((int) CameraDeviceModeSetting, videoModePtr);
        VideoModeData videoMode = (VideoModeData) Marshal.PtrToStructure
                            (videoModePtr, typeof(VideoModeData));
        Marshal.FreeHGlobal(videoModePtr);

        config.enabled = 1;
        config.synchronous = (SynchronousVideo ? 1 : 0);
        config.position = new Vec2I(0, 0);

        if (Screen.width > Screen.height)
        {
            float height = videoMode.height * (Screen.width / (float)
                            videoMode.width);
            config.size = new Vec2I(Screen.width, (int) height);
        }
        else
        {
            float width = videoMode.height * (Screen.height / (float)
                            videoMode.width);
            config.size = new Vec2I((int) width, Screen.height);
        }

        configPtr = Marshal.AllocHGlobal(
                        Marshal.SizeOf(typeof(VideoBGCfgData)));
        Marshal.StructureToPtr(config, configPtr, true);
        rendererSetVideoBackgroundCfg(configPtr);

        int viewportX = config.position.x + (Screen.width - config.size.x) / 2;
        int viewportY = config.position.y + (Screen.height - config.size.y) / 2;
        mViewportRect = new Rect(viewportX, viewportY,
                                    config.size.x, config.size.y);

        print("viewport:\n" + mViewportRect);
    }


    private void InitializeProjection()
    {
        float[] projMatrixArray = new float[16];
        IntPtr projMatrixPtr = Marshal.AllocHGlobal(
                    Marshal.SizeOf(typeof(float)) * projMatrixArray.Length);

        getProjectionGL(GetComponent<Camera>().nearClipPlane, GetComponent<Camera>().farClipPlane,
                            projMatrixPtr, (int)Screen.orientation);

        Marshal.Copy(projMatrixPtr, projMatrixArray, 0, projMatrixArray.Length);
        Matrix4x4 projectionMatrix = Matrix4x4.identity;
        for (int i = 0; i < 16; i++)
            projectionMatrix[i] = projMatrixArray[i];

        print("projection:\n" + projectionMatrix);

        this.GetComponent<Camera>().projectionMatrix = projectionMatrix;

        Marshal.FreeHGlobal(projMatrixPtr);
    }


    private void InitializeTrackables()
    {
        TrackableBehaviour[] trackableBehaviours = (TrackableBehaviour[])
                    FindObjectsOfType(typeof(TrackableBehaviour));

        for (int i = 0; i < trackableBehaviours.Length; i++)
        {
            TrackableBehaviour trackable = trackableBehaviours[i];

            if (trackable.TrackableName == null)
            {
                Debug.LogError("Error: Trackable at " + i + " has no name.");
                continue;
            }

            int id = trackableGetId(trackable.TrackableName);
            
            if (id == -1)
            {
                Debug.LogError("Error: Trackable named " +
                                trackable.TrackableName + " does not exist.");
            }
            else
            {
                trackable.InitializeID(id);
                if (!mTrackableBehaviourDict.ContainsKey(id))
                {
                    mTrackableBehaviourDict[id] =
                                new List<TrackableBehaviour>();
                }
                if (!mTrackableBehaviourDict[id].Contains(trackable))
                {
                    mTrackableBehaviourDict[id].Add(trackable);
                    Debug.Log("Found Trackable named " + trackable.TrackableName +
                                " with id " + trackable.TrackableID);
                }
            }
        }
    }


    private void InitializeVirtualButtons()
    {
        VirtualButtonBehaviour[] vbBehaviours = (VirtualButtonBehaviour[])
                    FindObjectsOfType(typeof(VirtualButtonBehaviour));

        for (int i = 0; i < vbBehaviours.Length; i++)
        {
            VirtualButtonBehaviour virtualButton = vbBehaviours[i];

            if (virtualButton.VirtualButtonName == null)
            {
                Debug.LogError("Error: VirtualButton at " + i +
                                " has no name.");
                continue;
            }

            ImageTargetBehaviour imageTarget = virtualButton.GetImageTarget();

            if (imageTarget == null)
            {
                Debug.LogError("Error: VirtualButton named " +
                                virtualButton.VirtualButtonName +
                                " is not attached to an ImageTarget.");
                continue;
            }

            int id = virtualButtonGetId(imageTarget.TrackableName,
                        virtualButton.VirtualButtonName);

            if (id == -1)
            {
                Debug.LogError("Error: VirtualButton named " +
                        virtualButton.VirtualButtonName + " does not exist.");
            }
            else
            {
                //  Duplicate check:
                if (!mVBBehaviourDict.ContainsKey(id))
                {
                    // OK:
                    virtualButton.InitializeID(id);
                    mVBBehaviourDict.Add(id, virtualButton);
                    Debug.Log("Found VirtualButton named " +
                            virtualButton.VirtualButtonName + " with id " +
                            virtualButton.ID);
                }
            }
        }
    }


    private void InitializeTrackableContainer()
    {
        // Destroy if the container has been allocated.
        Marshal.FreeHGlobal(mTrackablePtr);

        mAbsoluteNumTrackables = getNumTrackables();
        mTrackablePtr = Marshal.AllocHGlobal(Marshal.SizeOf(
                typeof(TrackableData)) * mAbsoluteNumTrackables);

        mTrackableDataArray = new TrackableData[mAbsoluteNumTrackables];

        print("Absolute number of trackables: " + mAbsoluteNumTrackables);
    }


    private void InitializeVirtualButtonContainer()
    {
        // Destroy if the container has been allocated.
        Marshal.FreeHGlobal(mVirtualButtonPtr);

        mAbsoluteNumVirtualButtons = getNumVirtualButtons();
        mVirtualButtonPtr = Marshal.AllocHGlobal(Marshal.SizeOf(
                typeof(VirtualButtonData)) * mAbsoluteNumVirtualButtons);

        mVBDataArrayDirty = false;
    }


    private void UpdateImageContainer()
    {
        // Reallocate the data container if the number of requested images has
        // changed, or if the container is not allocated
        if (mNumImageHeaders != mCameraDevice.GetAllImages().Count ||
           (mCameraDevice.GetAllImages().Count > 0 && mImageHeaderData == IntPtr.Zero))
        {

            mNumImageHeaders = mCameraDevice.GetAllImages().Count;

            Marshal.FreeHGlobal(mImageHeaderData);
            mImageHeaderData = Marshal.AllocHGlobal(Marshal.SizeOf(
                                typeof(ImageHeaderData)) * mNumImageHeaders);
        }

        // Update the image info:
        int i = 0;
        foreach (Image image in mCameraDevice.GetAllImages().Values)
        {
            IntPtr imagePtr = new IntPtr(mImageHeaderData.ToInt32() + i *
                   Marshal.SizeOf(typeof(ImageHeaderData)));

            ImageHeaderData imageHeader = new ImageHeaderData();
            imageHeader.width = image.Width;
            imageHeader.height = image.Height;
            imageHeader.stride = image.Stride;
            imageHeader.bufferWidth = image.BufferWidth;
            imageHeader.bufferHeight = image.BufferHeight;
            imageHeader.format = (int)image.PixelFormat;
            imageHeader.reallocate = 0;
            imageHeader.updated = 0;
            imageHeader.data = image.UnmanagedData;

            Marshal.StructureToPtr(imageHeader, imagePtr, false);
            ++i;
        }
    }


    private void UpdateCameraFrame()
    {
        // Unmarshal the image data:
        int i = 0;
        foreach (Image image in mCameraDevice.GetAllImages().Values)
        {
            IntPtr imagePtr = new IntPtr(mImageHeaderData.ToInt32() + i *
                   Marshal.SizeOf(typeof(ImageHeaderData)));
            ImageHeaderData imageHeader = (ImageHeaderData)
                Marshal.PtrToStructure(imagePtr, typeof(ImageHeaderData));

            // Copy info back to managed Image instance:
            image.Width = imageHeader.width;
            image.Height = imageHeader.height;
            image.Stride = imageHeader.stride;
            image.BufferWidth = imageHeader.bufferWidth;
            image.BufferHeight = imageHeader.bufferHeight;
            image.PixelFormat = (Image.PIXEL_FORMAT) imageHeader.format;

            // Reallocate if required:
            if (imageHeader.reallocate == 1)
            {
                image.Pixels = new byte[qcarGetBufferSize(image.BufferWidth,
                                                    image.BufferHeight,
                                                    (int)image.PixelFormat)];

                Marshal.FreeHGlobal(image.UnmanagedData);

                image.UnmanagedData = Marshal.AllocHGlobal(qcarGetBufferSize(image.BufferWidth,
                                    image.BufferHeight,
                                    (int)image.PixelFormat));

                // Note we don't copy the data this frame as the unmanaged
                // buffer was not filled.
            }
            else if (imageHeader.updated == 1)
            {
                // Copy data:
                image.CopyPixelsFromUnmanagedBuffer();
            }

            ++i;
        }
    }


    private void UpdateTrackables()
    {

        // If running within the Editor:
        if (Application.isEditor)
        {
            UpdateTrackablesEditor();
            return;
        }
        
        // Unmarshal the trackable data
        for (int i = 0; i < mAbsoluteNumTrackables; i++)
        {
            IntPtr trackablePtr = new IntPtr(mTrackablePtr.ToInt32() + i *
                    Marshal.SizeOf(typeof(TrackableData)));
            TrackableData trackableData = (TrackableData)
                    Marshal.PtrToStructure(trackablePtr, typeof(TrackableData));
            mTrackableDataArray[i] = trackableData;
        }

        // Add newly found trackables to the queue, remove lost ones
        foreach (TrackableData trackableData in mTrackableDataArray)
        {
            List<TrackableBehaviour> trackableBehaviours;
            if (mTrackableBehaviourDict.TryGetValue(
                    trackableData.id, out trackableBehaviours))
            {
                if ((trackableData.status == TrackableBehaviour.Status.DETECTED
                     || trackableData.status ==
                     TrackableBehaviour.Status.TRACKED))
                {
                    foreach (TrackableBehaviour trackableBehaviour in
                                trackableBehaviours)
                    {
                        if (!mTrackableFoundQueue.Contains(trackableBehaviour))
                        {
                            mTrackableFoundQueue.Add(trackableBehaviour);
                        }
                    }
                }
                else
                {
                    foreach (TrackableBehaviour trackableBehaviour in
                                trackableBehaviours)
                    {
                        if (mTrackableFoundQueue.Contains(trackableBehaviour))
                        {
                            mTrackableFoundQueue.Remove(trackableBehaviour);
                        }
                    }
                }
            }
        }

        // Remove disabled trackables from queue
        for (int i = mTrackableFoundQueue.Count - 1; i >= 0; i--)
        {
            TrackableBehaviour trackableBehaviour = mTrackableFoundQueue[i];
            if (trackableBehaviour.enabled == false)
            {
                mTrackableFoundQueue.Remove(trackableBehaviour);
            }
        }

        // Position the camera if necessary
        if (mWorldCenterMode == WorldCenterMode.USER && mWorldCenter != null &&
                    mWorldCenter.enabled)
        {
            foreach (TrackableData trackableData in mTrackableDataArray)
            {
                if (trackableData.id == mWorldCenter.TrackableID &&
                    (trackableData.status == TrackableBehaviour.Status.DETECTED
                     || trackableData.status ==
                     TrackableBehaviour.Status.TRACKED))
                {
                    PositionCamera(mWorldCenter, trackableData.pose);
                    break;
                }
            }
        }
        else if (mWorldCenterMode == WorldCenterMode.AUTO)
        {
            if (mTrackableFoundQueue.Count > 0)
            {
                TrackableBehaviour trackableBehaviour = mTrackableFoundQueue[0];
                foreach (TrackableData trackableData in mTrackableDataArray)
                {
                    if (trackableData.id == trackableBehaviour.TrackableID)
                    {
                        PositionCamera(trackableBehaviour, trackableData.pose);
                    }
                }
            }
        }

        // Position each trackable
        foreach (TrackableData trackableData in mTrackableDataArray)
        {
            List<TrackableBehaviour> trackableBehaviours;
            if (mTrackableBehaviourDict.TryGetValue(trackableData.id,
                    out trackableBehaviours))
            {
                foreach (TrackableBehaviour trackableBehaviour in
                            trackableBehaviours)
                {
                    if (trackableBehaviour == mWorldCenter &&
                            mWorldCenterMode == WorldCenterMode.USER)
                        continue;

                    if ((trackableData.status ==
                         TrackableBehaviour.Status.DETECTED
                         || trackableData.status ==
                         TrackableBehaviour.Status.TRACKED) &&
                         trackableBehaviour.enabled)
                    {
                        PositionTrackable(trackableBehaviour,
                            trackableData.pose);
                    }
                }
            }
        }

        // Update each trackable
        foreach (TrackableData trackableData in mTrackableDataArray)
        {
            List<TrackableBehaviour> trackableBehaviours;
            if (mTrackableBehaviourDict.TryGetValue(trackableData.id,
                        out trackableBehaviours))
            {
                foreach (TrackableBehaviour trackableBehaviour in
                            trackableBehaviours)
                {
                    if (trackableBehaviour.enabled)
                    {
                        trackableBehaviour.OnTrackerUpdate(
                                                trackableData.status);
                    }
                }
            }
        }
    }


    private void UpdateTrackablesEditor()
    {
        // When running within the Unity editor:
        TrackableBehaviour[] trackableBehaviours = (TrackableBehaviour[])
        FindObjectsOfType(typeof(TrackableBehaviour));

        // Simulate all trackables were tracked successfully:    
        for (int i = 0; i < trackableBehaviours.Length; i++)
        {
            TrackableBehaviour trackable = trackableBehaviours[i];
            if (trackable.enabled)
            {
                trackable.OnTrackerUpdate(TrackableBehaviour.Status.TRACKED);
            }
        }
    }


    private void PositionCamera(TrackableBehaviour trackableBehaviour,
                                    PoseData camToTargetPose)
    {
            GetComponent<Camera>().transform.localPosition =
                    trackableBehaviour.transform.rotation *
                    Quaternion.AngleAxis(90, Vector3.left) *
                    Quaternion.Inverse(camToTargetPose.orientation) *
                    (-camToTargetPose.position) +
                    trackableBehaviour.transform.position;

            GetComponent<Camera>().transform.rotation =
                    trackableBehaviour.transform.rotation *
                    Quaternion.AngleAxis(90, Vector3.left) *
                    Quaternion.Inverse(camToTargetPose.orientation);
    }


    private void PositionTrackable(TrackableBehaviour trackableBehaviour,
                                        PoseData camToTargetPose)
    {
            trackableBehaviour.transform.position =
                    GetComponent<Camera>().transform.TransformPoint(camToTargetPose.position);

            trackableBehaviour.transform.rotation =
                    GetComponent<Camera>().transform.rotation *
                    camToTargetPose.orientation *
                    Quaternion.AngleAxis(270, Vector3.left);
    }


    private void UpdateVirtualButtons()
    {
        for (int i = 0; i < mAbsoluteNumVirtualButtons; i++)
        {
            IntPtr vbPtr = new IntPtr(mVirtualButtonPtr.ToInt32() + i *
                                Marshal.SizeOf(typeof(VirtualButtonData)));
            VirtualButtonData vbData = (VirtualButtonData)
                    Marshal.PtrToStructure(vbPtr, typeof(VirtualButtonData));

            VirtualButtonBehaviour vb = null;
            if (mVBBehaviourDict.TryGetValue(vbData.id, out vb))
            {
                ImageTargetBehaviour it = vb.GetImageTarget();
                if (it != null && it.enabled && vb.enabled)
                {
                    vb.OnTrackerUpdated(vbData.isPressed > 0);
                }
            }
        }
    }


    private void SetUnityVersion()
    {
        int major  = 0;
        int minor  = 0;
        int change = 0;

        // Use non-numeric values as tokens for split
        string versionPattern = "[^0-9]";

        // Split Unity version string into multiple parts
        string[] unityVersionBits = Regex.Split(Application.unityVersion,
                                                versionPattern);

        // Sanity check if nothing went wrong
        if (unityVersionBits.Length >= 3)
        {
            major  = int.Parse(unityVersionBits[0]);
            minor  = int.Parse(unityVersionBits[1]);
            change = int.Parse(unityVersionBits[2]);
        }

        setUnityVersionNative(major, minor, change);
    }

    #endregion // PRIVATE_METHODS



    #region NATIVE_FUNCTIONS

#if !UNITY_EDITOR

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int getInitErrorCode();

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int isRendererDirty();

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int qcarSetHint(int hint, int value);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int qcarSetFrameFormat(int format, int enabled);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int qcarGetBitsPerPixel(int format);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int qcarGetBufferSize(int width, int height,
                                    int format);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int qcarRequiresAlpha();

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int cameraDeviceInitCamera();

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int cameraDeviceDeinitCamera();

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int cameraDeviceStartCamera();

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int cameraDeviceStopCamera();

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int cameraDeviceGetNumVideoModes();

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern void cameraDeviceGetVideoMode(int idx,
                                    [In, Out]IntPtr videoMode);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int cameraDeviceSelectVideoMode(int idx);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int startTracker();

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int stopTracker();

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int updateQCAR([In, Out]IntPtr trackableDataArray,
                                    int trackableArrayLength,
                                    [In, Out]IntPtr vbDataArray,
                                    int vbArrayLength,
                                    [In, Out]IntPtr imageHeaderDataArray,
                                    int imageHeaderArrayLength,
                                    int screenOrientation);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int getNumTrackables();

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int getProjectionGL(float nearClip, float farClip,
                                    [In, Out]IntPtr projMatrix,
                                    int screenOrientation);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int trackableGetId(String nTrackableName);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int getNumVirtualButtons();

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int virtualButtonGetId(String trackableName,
                                    String virtualButtonName);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int createVirtualButton(String trackableName,
                                    String virtualButtonName,
                                    [In, Out]IntPtr rectData);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern int destroyVirtualButton(String trackableName,
                                    String virtualButtonName);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern void rendererSetVideoBackgroundCfg(
                                    [In, Out]IntPtr bgCfg);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern void rendererGetVideoBackgroundCfg(
                                    [In, Out]IntPtr bgCfg);

    [DllImport(QCARMacros.PLATFORM_DLL)]
    private static extern void setUnityVersionNative(int major, int minor,
                                    int change);

#else

    // Stubs used when running within the editor:

    private static int getInitErrorCode() { return 0; }


    private static int isRendererDirty() { return 0; }


    private static int qcarSetHint(int hint, int value) { return 0; }


    private static int qcarSetFrameFormat(int format, int enabled) { return 0; }


    private static int qcarGetBitsPerPixel(int format) { return 0; }


    private static int qcarGetBufferSize(int width, int height,
                                    int format) { return 0; }


    private static int qcarRequiresAlpha() { return 0; }


    private static int cameraDeviceInitCamera() { return 0; }


    private static int cameraDeviceDeinitCamera() { return 0; }


    private static int cameraDeviceStartCamera() { return 0; }


    private static int cameraDeviceStopCamera() { return 0; }


    private static int cameraDeviceGetNumVideoModes() { return 0; }


    private static void cameraDeviceGetVideoMode(int idx,
                                    [In, Out]IntPtr videoMode) { }


    private static int cameraDeviceSelectVideoMode(int idx) { return 0; }


    private static int startTracker() { return 0; }


    private static int stopTracker() { return 0; }


    private static int updateQCAR([In, Out]IntPtr trackableDataArray,
                                    int trackableArrayLength,
                                    [In, Out]IntPtr vbDataArray,
                                    int vbArrayLength,
                                    [In, Out]IntPtr imageHeaderDataArray,
                                    int imageHeaderArrayLength,
                                    int screenOrientation) { return 0; }


    private static int getNumTrackables() { return 0; }


    private static int getProjectionGL(float nearClip, float farClip,
                                    [In, Out]IntPtr projMatrix,
                                    int screenOrientation) { return 0; }


    private static int trackableGetId(String nTrackableName) { return 0; }


    private static int getNumVirtualButtons() { return 0; }


    private static int virtualButtonGetId(String trackableName,
                                    String virtualButtonName) { return 0; }


    private static int createVirtualButton(String trackableName,
                                    String virtualButtonName,
                                    [In, Out]IntPtr rectData) { return 0; }


    private static int destroyVirtualButton(String trackableName,
                                    String virtualButtonName) { return 0; }


    private static void rendererSetVideoBackgroundCfg(
                                    [In, Out]IntPtr bgCfg) { }


    private static void rendererGetVideoBackgroundCfg(
                                    [In, Out]IntPtr bgCfg) { }


    private static void setUnityVersionNative(int major, int minor,
                                    int change) { }

#endif

    #endregion // NATIVE_FUNCTIONS
}
