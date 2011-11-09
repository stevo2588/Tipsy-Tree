/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using System.Timers;
using UnityEngine;

// The ConfigData Manager handles operations on the ConfigData (e.g. sync with
// config.xml file, sync with scene).
public class ConfigDataManager
{
    #region PROPTERTIES

    // Returns an instance of a ConfigDataManager (thread safe)
    public static ConfigDataManager Instance
    {
        get
        {
            // Make sure only one instance of SceneManager is created.
            if (mInstance == null)
            {
                lock (typeof(ConfigDataManager))
                {
                    if (mInstance == null)
                    {
                        mInstance = new ConfigDataManager();
                    }
                }
            }
            return mInstance;
        }
    }

    // Guarantees that config values are not changed wile active.
    public bool LockData
    {
        get
        {
            return mLockData;
        }
        set
        {
            mLockData = value;
        }
    }

    // Wraps ConfigData property for mutex.
    public int NumImageTargets
    {
        get
        {
            int num = 0;
            lock (mConfigData)
            {
                num = mConfigData.NumImageTargets;
            }
            return num;
        }
    }

    // Wraps ConfigData property for mutex.
    public int NumMultiTargets
    {
        get
        {
            int num = 0;
            lock (mConfigData)
            {
                num = mConfigData.NumMultiTargets;
            }
            return num;
        }
    }

    // Wraps ConfigData property for mutex.
    public int NumFrameMarkers
    {
        get
        {
            int num = 0;
            lock (mConfigData)
            {
                num = mConfigData.NumFrameMarkers;
            }
            return num;
        }
    }

    // If this property is set to true the config.xml file always contains the
    // same markers that are also part of the scene. If set to false markers
    // are kept in the config.xml file once they have been defined. This means
    // that developers need to remove markers manually from the file if they are
    // not needed anymore.
    public bool SyncMarkersSceneAndConfig
    {
        get
        {
            return mSyncMarkersSceneAndConfig;
        }

        set
        {
            mSyncMarkersSceneAndConfig = value;
        }
    }

    #endregion // PROPERTIES



    #region PRIVATE_MEMBER_VARIABLES

    // The config data object contains the data that is part of the config.xml
    // file.
    private ConfigData mConfigData = null;

    // Tells the timed thread to do a write.
    private bool mDoAsyncWrite;

    // Is true if a reading operation is in progress.
    private bool mReadInProgress;

    // Signals the data manager to not update config data while active.
    private bool mLockData;

    // Synchronize Markers of the Unity scene and the config.xml file
    // (see SyncMarkersSceneAndConfig - Property).
    private bool mSyncMarkersSceneAndConfig = true;

    // The timer class that is used to trigger a periodic method call.
    private Timer mUpdateTimer = null;

    // Singleton: Still uses lazy initialization:
    // Private static variables initialized on first reference to class.
    private static ConfigDataManager mInstance;

    #endregion // PRIVATE_MEMBER_VARIABLES



    #region CONSTRUCTION

    // Private constructor. Class is implemented as a singleton.
    private ConfigDataManager()
    {
        mConfigData = new ConfigData();

        mDoAsyncWrite = false;
        mReadInProgress = false;
        mLockData = false;

        mUpdateTimer = new Timer(20.0);
        mUpdateTimer.Elapsed += new ElapsedEventHandler(TimedUpdate);
        mUpdateTimer.Enabled = true;
    }

    #endregion // CONSTRUCTION



    #region PUBLIC_METHODS

    // Write config file. Must be called from Unity thread, will cause errors
    // otherwise.
    public void DoWrite(TrackableBehaviour[] trackables)
    {
        UpdateConfigData(trackables);

        // We only set a Boolean here and do the actual operation in a separate
        // thread.
        mDoAsyncWrite = true;
    }

    
    // Read config file.
    public void DoRead()
    {
        // Set read in progress so that the two operations do not process the
        // config.xml file at the same time.
        mReadInProgress = true;

        // Reading the config.xml file is NOT done asynchronously.
        ReadConfigData();

        mReadInProgress = false;
    }

    
    // Wraps ConfigData method for mutex.
    public void CopyImageTargetNames(string[] arrayToFill, int index)
    {
        lock (mConfigData)
        {
            mConfigData.CopyImageTargetNames(arrayToFill, index);
        }
    }


    // Wraps ConfigData method for mutex.
    public void CopyMultiTargetNames(string[] arrayToFill, int index)
    {
        lock (mConfigData)
        {
            mConfigData.CopyMultiTargetNames(arrayToFill, index);
        }
    }


    // Wraps ConfigData method for mutex.
    public bool GetImageTarget(string name, out ConfigData.ImageTarget it)
    {
        bool success = false;
        lock (mConfigData)
        {
            success = mConfigData.GetImageTarget(name, out it);
        }
        return success;
    }


    // Wraps ConfigData method for mutex.
    public bool GetMultiTarget(string name, out ConfigData.MultiTarget mt)
    {
        bool success = false;
        lock (mConfigData)
        {
            success = mConfigData.GetMultiTarget(name, out mt);
        }
        return success;
    }


    // Wraps ConfigData method for mutex.
    public bool GetFrameMarker(int id, out ConfigData.FrameMarker fm)
    {
        bool success = false;
        lock (mConfigData)
        {
            success = mConfigData.GetFrameMarker(id, out fm);
        }
        return success;
    }


    // Wraps ConfigData method for mutex.
    public bool GetVirtualButton(string name,
                                 string imageTargetName,
                                 out ConfigData.VirtualButton vb)
    {
        bool success = false;
        lock (mConfigData)
        {
            success =
                mConfigData.GetVirtualButton(name, imageTargetName, out vb);
        }
        return success;
    }

    #endregion // PUBLIC_METHODS



    #region PRIVATE_METHODS

    // Method reads config data from config.xml file.
    private void ReadConfigData()
    {
        lock (mConfigData)
        {
            mConfigData.ClearAll();

            // Parse config.xml file data.
            ConfigParser.Instance.fileToStruct(
                QCARUtilities.GlobalVars.CONFIG_XML_PATH, mConfigData);
        }
    }

    
    // Method writes config data to config.xml file.
    private void AsyncWriteConfigData()
    {
        ConfigData writeBuffer = null;
        lock (mConfigData)
        {
            // We work on a buffer copy so that the buffer is not messed up
            // while it is written to the file.
            writeBuffer = new ConfigData(mConfigData);
        }

        // Write config.xml file.
        ConfigParser.Instance.structToFile(
            QCARUtilities.GlobalVars.CONFIG_XML_PATH, writeBuffer);
    }


    // Method runs in a separate thread and handles read and write operations
    // asynchronously.
    private void TimedUpdate(object source, ElapsedEventArgs e)
    {
        if (mDoAsyncWrite && (!mReadInProgress) && (!mLockData))
        {
            AsyncWriteConfigData();

            mDoAsyncWrite = false;
            SceneManager.Instance.ConfigFileWritten();
        }
    }


    // Update the config data buffer with new data.
    private void UpdateConfigData(TrackableBehaviour[] trackables)
    {
        // Sanity check. If no trackables have been set we return.
        if (trackables == null)
        {
            return;
        }
        
        // Lock config data buffer to make sure it is not changed while it is
        // updated with scene data.
        lock (mConfigData)
        {
            // We clear the frame markers from the config data if scene and
            // config.xml file synchronization is active.
            if (mSyncMarkersSceneAndConfig)
            {
                mConfigData.ClearFrameMarkers();
            }

            foreach (TrackableBehaviour trackable in trackables)
            {
                string trackableName = trackable.TrackableName;

                // We ignore Trackables with default or empty names.
                if (trackableName == QCARUtilities.GlobalVars.DEFAULT_NAME ||
                    trackableName == "")
                {
                    continue;
                }

                if (trackable.GetType() == typeof(ImageTargetBehaviour))
                {
                    ImageTargetBehaviour it = (ImageTargetBehaviour)trackable;

                    ConfigData.ImageTarget itConfig;

                    // If there is no image target with the requested name in the configuration we ignore the target.
                    if (!SceneManager.Instance.GetImageTarget(it.TrackableName, out itConfig))
                    {
                        continue;
                    }

                    itConfig.size = it.GetSize();

                    // Clear virtual buttons before processing.
                    itConfig.virtualButtons.Clear();

                    // Process Virtual Button list.
                    VirtualButtonBehaviour[] vbs =
                        it.GetComponentsInChildren<VirtualButtonBehaviour>();
                    foreach (VirtualButtonBehaviour vb in vbs)
                    {
                        Vector2 leftTop;
                        Vector2 rightBottom;
                        if (!vb.CalculateButtonArea(out leftTop,
                                                    out rightBottom))
                        {
                            // Invalid Button
                            continue;
                        }

                        ConfigData.VirtualButton vbConfig =
                            new ConfigData.VirtualButton();

                        vbConfig.name = vb.VirtualButtonName;
                        vbConfig.enabled = vb.enabled;
                        vbConfig.rectangle = new Vector4(leftTop.x,
                                                         leftTop.y,
                                                         rightBottom.x,
                                                         rightBottom.y);
                        vbConfig.sensitivity = vb.SensitivitySetting;

                        itConfig.virtualButtons.Add(vbConfig);
                    }

                    mConfigData.SetImageTarget(itConfig, it.TrackableName);
                }
                else if (trackable.GetType() == typeof(MarkerBehaviour))
                {
                    MarkerBehaviour fm = (MarkerBehaviour)trackable;

                    ConfigData.FrameMarker fmConfig;

                    SceneManager.Instance.GetFrameMarker(fm.MarkerID, out fmConfig);

                    fmConfig.name = fm.TrackableName;
                    fmConfig.size = fm.GetSize();

                    mConfigData.SetFrameMarker(fmConfig, fm.MarkerID);
                }
            }
        }
    }

    #endregion // PRIVATE_METHODS
}