/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// The scene manager is responsible for synchronizing scene and config.xml file
public class SceneManager
{
    #region PROPERTIES

    // Returns an instance of a SceneManager (thread safe)
    public static SceneManager Instance
    {
        get
        {
            // Make sure only one instance of SceneManager is created.
            if (mInstance == null)
            {
                lock (typeof(SceneManager))
                {
                    if (mInstance == null)
                    {
                        mInstance = new SceneManager();
                    }
                }
            }
            return mInstance;
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
            return ConfigDataManager.Instance.SyncMarkersSceneAndConfig;
        }

        set
        {
            ConfigDataManager.Instance.SyncMarkersSceneAndConfig = value;
            // To store non-behaviour properties we have to serialize/write
            // them manually.
            WriteProperties();
        }
    }

    // Returns if the scene has already been initialized. This initialization
    // happens only once in a SceneManager lifetime.
    public bool SceneInitialized
    {
        get
        {
            return mSceneInitialized;
        }
    }    

    #endregion // PROPERTIES



    #region PRIVATE_MEMBER_VARIABLES

    // Delegate that is called by Unity in editor.
    private EditorApplication.CallbackFunction mUpdateCallback = null;
    // These booleans are used to tell the SceneManager that it is time to write
    // or read values to or from the config.xml file.
    private bool mDoSerialization = false;
    private bool mDoDeserialization = false;
    // If we have just written a new config.xml file we ignore the callback that
    // tells us that a new file has been generated.
    private bool mIgnoreDeserializationRequest = false;
    // This variable is used to check if the scene has been initialized.
    private bool mSceneInitialized = false;
    // Since it is not possible to open up the QCAR web page from a button press
    // in the Unity editor directly this is handled in the EditorUpdate
    // callback.
    private bool mGoToARPage = false;
    // The LastNum... variables are used to check for QCARObject deletions in
    // the scene.
    private int mLastNumTrackables = 0;
    private int mLastNumVirtualButtons = 0;
    // The timer objects are used to calculate time deltas for writing the
    // config.xml file.
    private DateTimeOffset mPreviousTime = DateTimeOffset.UtcNow;
    private TimeSpan mRefreshThreshold;
    private bool mConfigFileWritten = false;

    // Singleton: Still uses lazy initialization:
    // Private static variables initialized on first reference to class.
    private static SceneManager mInstance;
    private const int SECONDS_TO_WAIT = 5;

    #endregion // PRIVATE_MEMBER_VARIABLES



    #region CONSTRUCTION

    private SceneManager()
    {
        mUpdateCallback = new EditorApplication.CallbackFunction(EditorUpdate);
        if (EditorApplication.update == null)
        {
            EditorApplication.update += mUpdateCallback;
        }
        else if (!EditorApplication.update.Equals(mUpdateCallback))
        {
            EditorApplication.update += mUpdateCallback;
        }

        mRefreshThreshold = new TimeSpan(0, 0, 0, SECONDS_TO_WAIT, 0);

        // We force a config.xml read operation before the SceneManager is used
        // to avoid inconsistencies on Unity startup.
        mDoDeserialization = true;

        // Make sure that the scene is initialized whenever a new instance of
        // the SceneManager is created.
        mSceneInitialized = false;

        // Unity does not allow to store global editor settings in a comfortable
        // way so we store them in a file.
        ReadProperties();
    }

    #endregion // CONSTRUCTION



    #region PUBLIC_METHODS

    // Initializes scene with content of the config.xml file (ensures that 
    // changes to the file that happened while Unity was not running are
    // applied).
    public void InitScene()
    {
        TrackableBehaviour[] trackables =
            (TrackableBehaviour[])UnityEngine.Object.FindObjectsOfType(
                typeof(TrackableBehaviour));
        VirtualButtonBehaviour[] virtualButtons = (VirtualButtonBehaviour[])
            UnityEngine.Object.FindObjectsOfType(
                typeof(VirtualButtonBehaviour));

        ConfigDataManager.Instance.DoRead();

        // We apply the newly read values to the scene.
        UpdateTrackables(trackables);

        // Cache number of trackables and buttons:
        mLastNumTrackables = trackables.Length;
        mLastNumVirtualButtons = virtualButtons.Length;

        mSceneInitialized = true;
    }

    
    // Update method triggered by Unity.
    // Used to check scene content changes.
    // Be aware that this update callback only is called once a SceneManager
    // instance has been created.
    // Be aware that the instance is destroyed if the SceneManager file is
    // recompiled.
    public void EditorUpdate()
    {
        TrackableBehaviour[] trackables =
            (TrackableBehaviour[])UnityEngine.Object.FindObjectsOfType(
                typeof(TrackableBehaviour));
        VirtualButtonBehaviour[] virtualButtons =
            (VirtualButtonBehaviour[])UnityEngine.Object.FindObjectsOfType(
                typeof(VirtualButtonBehaviour));

        // Continuously check if number of targets in scene has been lowered.
        if (ElementsDeleted(trackables, virtualButtons))
        {
            mDoSerialization = true;
        }

        // Correct scales of Trackables.
        if (CorrectTrackableScales(trackables))
        {
            mDoSerialization = true;
        }

        // Correct Virtual Button poses. Serialize on update.
        if (VirtualButtonEditor.CorrectPoses(virtualButtons))
        {
            mDoSerialization = true;
        }

        // We do deserialization and serialization in this order to avoid
        // overwriting the config.xml file.
        if (mDoDeserialization)
        {
            ConfigDataManager.Instance.DoRead();

            // We apply the newly read values to the scene.
            UpdateTrackables(trackables);

            mDoDeserialization = false;
        }

        if (mDoSerialization)
        {
            ValidateScene(trackables);

            ConfigDataManager.Instance.DoWrite(trackables);

            mDoSerialization = false;
        }

        if (mConfigFileWritten)
        {
            // The Asset Database refresh is a costly operation and should only
            // be called in a given interval.
            if (DateTimeOffset.UtcNow - mPreviousTime >= mRefreshThreshold)
            {
                AssetDatabase.Refresh();
                mConfigFileWritten = false;
                mPreviousTime = DateTimeOffset.UtcNow;
            }
        }

        if (mGoToARPage)
        {
            mGoToARPage = false;
            System.Diagnostics.Process.Start(
                "https://ar.qualcomm.com/qdevnet/projects");
        }
    }

    
    public string[] GetImageTargetNames()
    {
        string[] itNames = new string[ConfigDataManager.Instance.NumImageTargets + 1];
        itNames[0] = QCARUtilities.GlobalVars.DEFAULT_NAME;
        ConfigDataManager.Instance.CopyImageTargetNames(itNames, 1);
        return itNames;
    }


    public bool GetImageTarget(string name, out ConfigData.ImageTarget it)
    {
        bool itDefined = false;
        itDefined = ConfigDataManager.Instance.GetImageTarget(name, out it);

        if (!itDefined)
        {
            // Apply default values if name cannot be found.
            it.size = new Vector2(200.0f, 200.0f);
            it.virtualButtons = new List<ConfigData.VirtualButton>();
        }

        return itDefined;
    }

    
    public string[] GetMultiTargetNames()
    {
        string[] itNames = new string[ConfigDataManager.Instance.NumMultiTargets + 1];
        itNames[0] = QCARUtilities.GlobalVars.DEFAULT_NAME;
        ConfigDataManager.Instance.CopyMultiTargetNames(itNames, 1);
        return itNames;
    }

    
    public bool GetMultiTarget(string name, out ConfigData.MultiTarget mt)
    {
        bool mtDefined = false;

        mtDefined = ConfigDataManager.Instance.GetMultiTarget(name, out mt);

        if (!mtDefined)
        {
            // Apply default values if name cannot be found.
            mt.parts = CreateDefaultParts();
        }

        return mtDefined;
    }


    public int GetNextFrameMarkerID()
    {
        MarkerBehaviour[] markers =
            (MarkerBehaviour[])UnityEngine.Object.FindObjectsOfType(
                typeof(MarkerBehaviour));

        if (markers.Length <= 0)
        {
            return 0;
        }
        if (markers.Length >= QCARUtilities.GlobalVars.MAX_NUM_FRAME_MARKERS)
        {
            Debug.LogWarning("Too many frame markers in scene.");
            return (QCARUtilities.GlobalVars.MAX_NUM_FRAME_MARKERS - 1);
        }

        int freeMarkerID = 0;
        bool idIsFree = false;
        while (!idIsFree)
        {
            idIsFree = true;
            for (int i = 0; i < markers.Length; ++i)
            {
                // If marker is in scene it is not free.
                if (markers[i].MarkerID == freeMarkerID)
                {
                    idIsFree = false;
                    ++freeMarkerID;
                    break;
                }

                // If marker is defined in config it is also not free.
                ConfigData.FrameMarker fm;
                if (ConfigDataManager.Instance.GetFrameMarker(freeMarkerID, out fm))
                {
                    idIsFree = false;
                    ++freeMarkerID;
                    break;
                }
            }
        }
        return freeMarkerID;
    }

    
    public bool GetFrameMarker(int id, out ConfigData.FrameMarker fm)
    {
        //ConfigData.FrameMarker fm = new ConfigData.FrameMarker();

        bool fmDefined = false;

        fmDefined = ConfigDataManager.Instance.GetFrameMarker(id, out fm);

        if (!fmDefined)
        {
            // Initialize Frame Marker with default values if it is not defined
            // in backlog.
            fm.size = new Vector2(60.0f, 60.0f);
            fm.name = MarkerAccessor.MarkerNamePrefix + id.ToString();
        }

        return fmDefined;
    }

    
    // Method to be called by editors that change QCARBehaviours
    public void SceneUpdated()
    {
        mDoSerialization = true;
    }


    // Method to be called by data postprocessor on import of new QCAR
    // related files.
    public void FilesUpdated(TargetDataPostprocessor.ImportState newConfig,
                             TargetDataPostprocessor.ImportState newTextures,
                             TargetDataPostprocessor.ImportState newDataset)
    {
        if (newConfig == TargetDataPostprocessor.ImportState.ADDED)
        {
            if (mIgnoreDeserializationRequest)
            {
                mIgnoreDeserializationRequest = false;
            }
            else
            {
                mDoDeserialization = true;
            }
        }

        if (newTextures == TargetDataPostprocessor.ImportState.ADDED)
        {
            Debug.Log("Textures have been updated.");
        }

        if (newDataset == TargetDataPostprocessor.ImportState.ADDED)
        {
            Debug.Log("Dataset has been updated.");
        }

        // Only if not all data files have been deleted in one go we warn the
        // user about possible issues.
        if (!((newConfig == TargetDataPostprocessor.ImportState.DELETED) &&
              (newTextures == TargetDataPostprocessor.ImportState.DELETED) &&
              (newDataset == TargetDataPostprocessor.ImportState.DELETED)))
        {
            if (newConfig == TargetDataPostprocessor.ImportState.DELETED)
            {
                Debug.LogWarning("You deleted config.xml!\n" +
                    "Files may be out of sync!\n" +
                    "Please reimport full target set if you encounter " +
                    "problems.");
            }

            if (newTextures == TargetDataPostprocessor.ImportState.DELETED)
            {
                Debug.LogWarning("You deleted texture files!\n" +
                    "Files may be out of sync!\n" +
                    "Please reimport full target set if you encounter " +
                    "problems.");
            }

            if (newDataset == TargetDataPostprocessor.ImportState.DELETED)
            {
                Debug.LogWarning("You deleted qcar-resources.dat!\n" +
                    "Files may be out of sync!\n" +
                    "Please reimport full target set if you encounter " +
                    "problems.");
            }
        }
    }


    // This method is used by ConfigDataManager to inform SceneManager about
    // config.xml file write updates
    public void ConfigFileWritten()
    {   
        // We ignore the next deserialization request because it was most
        // likely caused by Scene Manager.
        mIgnoreDeserializationRequest = true;

        mConfigFileWritten = true;
    }


    // This is function enables an asynchronous call to open the QCAR help page.
    public void GoToARPage()
    {
        mGoToARPage = true;
    }


    // Returns the number of Trackables that can be used in the scene
    // by Trackable type.
    public int GetNumAvailableTrackables(Type trackableType)
    {
        if (trackableType == typeof(MarkerBehaviour))
        {
            return ConfigDataManager.Instance.NumFrameMarkers;
        }
        else if (trackableType == typeof(ImageTargetBehaviour))
        {
            return ConfigDataManager.Instance.NumImageTargets;
        }
        else if (trackableType == typeof(MultiTargetBehaviour))
        {
            return ConfigDataManager.Instance.NumMultiTargets;
        }
        else
        {
            Debug.LogWarning(trackableType.ToString() +
                             " is not derived from TrackableBehaviour.");
            return -1;
        }
    }

    #endregion // PUBLIC_METHODS



    #region PRIVATE_METHODS

    // Create Multi Target from default Image Targets
    private List<ConfigData.MultiTargetPart> CreateDefaultParts()
    {
        List<ConfigData.MultiTargetPart> prts =
            new List<ConfigData.MultiTargetPart>(6);

        // Get default Image Target and use it as template for MT parts.
        ConfigData.ImageTarget it;
        GetImageTarget("", out it);

        // We assume a square default target.
        float offset = it.size.x * 0.5f;

        // Front
        ConfigData.MultiTargetPart frontPart = new ConfigData.MultiTargetPart();
        frontPart.translation = new Vector3(0, offset, 0);
        frontPart.rotation = Quaternion.AngleAxis(0, new Vector3(1, 0, 0));
        frontPart.name = QCARUtilities.GlobalVars.DEFAULT_NAME;
        prts.Add(frontPart);

        // Back
        ConfigData.MultiTargetPart backPart = new ConfigData.MultiTargetPart();
        backPart.translation = new Vector3(0, -offset, 0);
        backPart.rotation = Quaternion.AngleAxis(180, new Vector3(1, 0, 0));
        backPart.name = QCARUtilities.GlobalVars.DEFAULT_NAME;
        prts.Add(backPart);

        // Left
        ConfigData.MultiTargetPart leftPart = new ConfigData.MultiTargetPart();
        leftPart.translation = new Vector3(-offset, 0, 0);
        leftPart.rotation = Quaternion.AngleAxis(90, new Vector3(0, 0, 1));
        leftPart.name = QCARUtilities.GlobalVars.DEFAULT_NAME;
        prts.Add(leftPart);

        // Right
        ConfigData.MultiTargetPart rightPart = new ConfigData.MultiTargetPart();
        rightPart.translation = new Vector3(offset, 0, 0);
        rightPart.rotation = Quaternion.AngleAxis(-90, new Vector3(0, 0, 1));
        rightPart.name = QCARUtilities.GlobalVars.DEFAULT_NAME;
        prts.Add(rightPart);

        // Top
        ConfigData.MultiTargetPart topPart = new ConfigData.MultiTargetPart();
        topPart.translation = new Vector3(0, 0, offset);
        topPart.rotation = Quaternion.AngleAxis(90, new Vector3(1, 0, 0));
        topPart.name = QCARUtilities.GlobalVars.DEFAULT_NAME;
        prts.Add(topPart);

        // Bottom
        ConfigData.MultiTargetPart btmPart = new ConfigData.MultiTargetPart();
        btmPart.translation = new Vector3(0, 0, -offset);
        btmPart.rotation = Quaternion.AngleAxis(-90, new Vector3(1, 0, 0));
        btmPart.name = QCARUtilities.GlobalVars.DEFAULT_NAME;
        prts.Add(btmPart);

        return prts;
    }


    // Updates trackables in scene from config data.
    private void UpdateTrackables(TrackableBehaviour[] trackables)
    {
        // Config data should not change during that process.
        ConfigDataManager.Instance.LockData = true;
        foreach (TrackableBehaviour trackable in trackables)
        {
            TrackableAccessor configApplier = AccessorFactory.Create(trackable);
            configApplier.ApplyConfigValues();
        }
        ConfigDataManager.Instance.LockData = false;
    }


    // Write global settings to file.
    private void WriteProperties()
    {
        using (FileStream stream =
            new FileStream(QCARUtilities.GlobalVars.PROPERTIES_FILE_PATH,
                           FileMode.Create))
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(
                    ConfigDataManager.Instance.SyncMarkersSceneAndConfig);
                writer.Close();
            }
        }
    }


    // Read global settings from file.
    private void ReadProperties()
    {
        try
        {
            using (FileStream stream =
                new FileStream(QCARUtilities.GlobalVars.PROPERTIES_FILE_PATH,
                               FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    ConfigDataManager.Instance.SyncMarkersSceneAndConfig =
                        reader.ReadBoolean();
                    reader.Close();
                }
            }
        }
        catch
        {
            // If some error turns up (e.g. file does not exist) we just
            // recreate the file with the current property state.
            WriteProperties();
        }
    }


    // Was number of scene elements reduced?
    private bool ElementsDeleted(TrackableBehaviour[] trackables,
                                 VirtualButtonBehaviour[] vbs)
    {
        bool elementsDeleted = false;

        if (trackables.Length < mLastNumTrackables ||
            vbs.Length < mLastNumVirtualButtons)
        {
            elementsDeleted = true;
        }
        mLastNumTrackables = trackables.Length;
        mLastNumVirtualButtons = vbs.Length;

        return elementsDeleted;
    }


    // Validate scene elements.
    private void ValidateScene(TrackableBehaviour[] trackables)
    {
        //Before we serialize we check for duplicates and provide a warning.
        for (int i = 0; i < trackables.Length; ++i)
        {
            string tNameA = trackables[i].TrackableName;

            // Ignore default names...
            if (tNameA == QCARUtilities.GlobalVars.DEFAULT_NAME)
                continue;

            for (int j = i + 1; j < trackables.Length; ++j)
            {
                string tNameB = trackables[j].TrackableName;

                // Ignore default names...
                if (tNameB == QCARUtilities.GlobalVars.DEFAULT_NAME)
                    continue;

                if (tNameA == tNameB)
                {
                    Debug.LogWarning("Duplicate Trackables detected: \"" +
                                     tNameA +
                                     "\". Augmentations will be merged.");
                }
            }
        }

        // Validate all Virtual Buttons in the scene.
        VirtualButtonEditor.Validate();
    }


    // Correct scales of Trackables (make them uniform).
    private bool CorrectTrackableScales(TrackableBehaviour[] trackables)
    {
        bool scaleCorrected = false;
        foreach (TrackableBehaviour trackable in trackables)
        {
            if (trackable.CorrectScale())
            {
                scaleCorrected = true;
            }
        }
        return scaleCorrected;
    }

    #endregion // PRIVATE_METHODS
}
