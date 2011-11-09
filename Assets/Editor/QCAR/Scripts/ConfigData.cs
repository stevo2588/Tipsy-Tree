/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using System;
using System.Collections.Generic;
using UnityEngine;

// This class is used to store and access data that is read from a config.xml
// file.
public class ConfigData
{
    #region NESTED

    // Representation of a Virtual Button node in the config.xml file.
    public struct VirtualButton
    {
        public string name;
        public bool enabled;
        public Vector4 rectangle;
        public VirtualButtonBehaviour.Sensitivity sensitivity;
    }

    // Representation of an Image Target node in the config.xml file.
    public struct ImageTarget
    {
        public Vector2 size;
        public List<VirtualButton> virtualButtons;
    }

    // Representation of a Multi Target Part node in the config.xml file.
    public struct MultiTargetPart
    {
        public string name;
        public Vector3 translation;
        public Quaternion rotation;
    }

    // Representation of a Multi Target node in the config.xml file.
    public struct MultiTarget
    {
        public List<MultiTargetPart> parts;
    }

    // Representation of a Frame Marker node in the config.xml file.
    public struct FrameMarker
    {
        public string name;
        public Vector2 size;
    }

    #endregion // NESTED



    #region PROPERTIES

    // Returns the number of Image Targets currently present in the config data.
    public int NumImageTargets
    {
        get
        {
            return imageTargets.Count;
        }
    }

    // Returns the number of Multi Targets currently present in the config data.
    public int NumMultiTargets
    {
        get
        {
            return multiTargets.Count;
        }
    }

    // Returns the number of Frame Markers currently present in the config data.
    public int NumFrameMarkers
    {
        get
        {
            return frameMarkers.Length;
        }
    }

    //Returns the overall number of Trackables currently present in the config data.
    public int NumTrackables
    {
        get
        {
            return (NumImageTargets + NumMultiTargets + getNumDefinedFrameMarkers());
        }
    }

    #endregion // PROPERTIES



    #region PRIVATE_MEMBER_VARIABLES

    private FrameMarker[] frameMarkers;
    private bool[] frameMarkersDefined;
    private Dictionary<string, ImageTarget> imageTargets;
    private Dictionary<string, MultiTarget> multiTargets;

    #endregion // PRIVATE_MEMBER_VARIABLES



    #region CONSTRUCTION

    // Constructor of ConfigData class.
    // Creates initializes internal collections.
    public ConfigData()
    {
        frameMarkers =
            new FrameMarker[QCARUtilities.GlobalVars.MAX_NUM_FRAME_MARKERS];
        frameMarkersDefined =
            new bool[QCARUtilities.GlobalVars.MAX_NUM_FRAME_MARKERS];
        imageTargets = new Dictionary<string, ImageTarget>();
        multiTargets = new Dictionary<string, MultiTarget>();

        ClearFrameMarkers();
    }

    // Copy constructor of ConfigData class.
    public ConfigData(ConfigData original)
    {
        // Create Frame Marker array and copy over values from original.
        frameMarkers =
            new FrameMarker[QCARUtilities.GlobalVars.MAX_NUM_FRAME_MARKERS];
        for (int i = 0; i < frameMarkers.Length; ++i)
        {
            frameMarkers[i] = original.frameMarkers[i];
        }

        // Create Frame Marker defined array and copy over values from original.
        frameMarkersDefined =
            new bool[QCARUtilities.GlobalVars.MAX_NUM_FRAME_MARKERS];
        for (int i = 0; i < frameMarkersDefined.Length; ++i)
        {
            frameMarkersDefined[i] = original.frameMarkersDefined[i];
        }

        // Create Image Target dictionary from original.
        imageTargets =
            new Dictionary<string, ImageTarget>(original.imageTargets);

        // Create Multi Target dictionary from original.
        multiTargets =
            new Dictionary<string, MultiTarget>(original.multiTargets);
    }

    #endregion //CONSTRUCTION



    #region PUBLIC_METHODS

    // Set attributes of the Image Target with the given name.
    // If the Image Target does not yet exist it is created automatically.
    public void SetImageTarget(ImageTarget item, string name)
    {
        imageTargets[name] = item;
    }


    // Set attributes of the Multi Target with the given name.
    // If the Multi Target does not yet exist it is created automatically.
    public void SetMultiTarget(MultiTarget item, string name)
    {
        multiTargets[name] = item;
    }


    // Set attributes of the Frame Marker with the given id.
    public bool SetFrameMarker(FrameMarker item, int id)
    {
        if (id < 0 || id >= frameMarkers.Length)
            return false;

        frameMarkers[id] = item;
        frameMarkersDefined[id] = true;
        return true;
    }


    // Add Virtual Button to the Image Target with the given imageTargetName.
    public bool AddVirtualButton(VirtualButton item, string imageTargetName)
    {
        try
        {
            ImageTarget it = imageTargets[imageTargetName];
            it.virtualButtons.Add(item);
        }
        catch
        {
            return false;
        }

        return true;
    }


    // Add Multi Target Part to the Multi Target with the given multiTargetName.
    public bool AddMultiTargetPart(MultiTargetPart item, string multiTargetName)
    {
        try
        {
            MultiTarget mt = multiTargets[multiTargetName];
            mt.parts.Add(item);
        }
        catch
        {
            return false;
        }

        return true;
    }


    // Clear all data.
    public void ClearAll()
    {
        ClearImageTargets();
        ClearMultiTargets();
        ClearFrameMarkers();
    }


    // Clear all Image Target data.
    public void ClearImageTargets()
    {
        imageTargets.Clear();
    }


    // Clear all Multi Target data.
    public void ClearMultiTargets()
    {
        multiTargets.Clear();
    }


    // Clear all Frame Marker data.
    public void ClearFrameMarkers()
    {
        for (int i = 0; i < frameMarkers.Length; ++i)
        {
            InvalidateFrameMarker(i);
        }  
    }


    // Clear all Virtual Button data.
    public void ClearVirtualButtons()
    {
        foreach (ImageTarget it in imageTargets.Values)
        {
            it.virtualButtons.Clear();
        }
    }


    // Remove Image Target with the given name.
    // Returns false if Image Target does not exist.
    public bool RemoveImageTarget(string name)
    {
        return imageTargets.Remove(name);
    }


    // Remove Multi Target with the given name.
    // Returns false if Multi Target does not exist.
    public bool RemoveMultiTarget(string name)
    {
        return multiTargets.Remove(name);
    }


    // Clear data of the Frame Marker with the given id and set it to undefined.
    public bool InvalidateFrameMarker(int id)
    {
        if (id < 0 || id >= frameMarkers.Length)
            return false;

        frameMarkers[id] = new FrameMarker();
        frameMarkersDefined[id] = false;

        return true;
    }


    // Creates a new Image Target with the data of the Image Target with the
    // given name.
    // Returns false if Image Target does not exist.
    public bool GetImageTarget(string name, out ImageTarget it)
    {
        try
        {
            it = imageTargets[name];
        }
        catch(KeyNotFoundException)
        {
            it = new ImageTarget();
            return false;
        }
        catch(Exception e)
        {
            throw e;
        }
        
        return true;
    }


    // Creates a new Multi Target with the data of the Multi Target with the
    // given name.
    // Returns false if Multi Target does not exist.
    public bool GetMultiTarget(string name, out MultiTarget mt)
    {
        try
        {
            mt = multiTargets[name];
        }
        catch (KeyNotFoundException)
        {
            mt = new MultiTarget();
            return false;
        }
        catch (Exception e)
        {
            throw e;
        }

        return true;
    }


    // Creates a new Frame Marker with the data of the Frame Marker with the
    // given id.
    // Returns false if Frame Marker does not exist.
    public bool GetFrameMarker(int id, out FrameMarker fm)
    {
        fm = new FrameMarker();
        bool defined = false;

        try
        {
            fm = frameMarkers[id];
            defined = frameMarkersDefined[id];
        }
        catch
        {
            throw;
        }

        return defined;
    }


    // Creates a new Virtual Button with the data of the Virtual Button with the
    // given name that is a child of the Image Target with the name
    // imageTargetName.
    // Returns false if Virtual Button does not exist.
    public bool GetVirtualButton(string name,
                                 string imageTargetName,
                                 out VirtualButton vb)
    {
        vb = new VirtualButton();

        try
        {
            ImageTarget it;
            if (!GetImageTarget(imageTargetName, out it))
            {
                return false;
            }

            List<VirtualButton> vbs = it.virtualButtons;
            for (int i = 0; i < vbs.Count; ++i)
            {
                if (vbs[i].name == name)
                {
                    vb = vbs[i];
                    return true;
                }
            }
        }
        catch
        {}

        return false;
    }


    // Copy all Image Target names into the given string array.
    // The index defines at which location to start copying.
    public void CopyImageTargetNames(string[] arrayToFill, int index)
    {
        try
        {
            imageTargets.Keys.CopyTo(arrayToFill, index);
        }
        catch
        {
            throw;
        }
    }


    // Copy all Multi Target names into the given string array.
    // The index defines at which location to start copying.
    public void CopyMultiTargetNames(string[] arrayToFill, int index)
    {
        try
        {
            multiTargets.Keys.CopyTo(arrayToFill, index);
        }
        catch
        {
            throw;
        }
    }

    #endregion // PUBLIC_METHODS



    #region PRIVATE_METHODS

    // Returns the number of defined Frame Markers.
    private int getNumDefinedFrameMarkers()
    {
        int numMarkers = 0;

        for (int i = 0; i < frameMarkersDefined.Length; ++i)
        {
            if (frameMarkersDefined[i])
                numMarkers++;
        }

        return numMarkers;
    }

    #endregion // PRIVATE_METHODS
}
