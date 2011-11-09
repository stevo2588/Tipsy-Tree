/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using UnityEngine;

public class QCARUtilities
{
    #region NESTED

    // This struct contains a collection of constant variables and makes them
    // available to all editor classes.
    public struct GlobalVars
    {
        // Paths to QCAR specific assets. "REGEXED_..." paths are used to
        // identify assets that have been renamed by Unity.
        public const string DAT_PATH =
            "Assets/StreamingAssets/QCAR/qcar-resources.dat";
        public const string REGEXED_DAT_PATH =
            "Assets/StreamingAssets/QCAR/qcar-resources[ ][0-9][0-9]*[.]dat";
        public const string CONFIG_XML_PATH =
            "Assets/StreamingAssets/QCAR/config.xml";
        public const string REGEXED_CONFIG_XML_PATH =
            "Assets/StreamingAssets/QCAR/config[ ][0-9][0-9]*[.]xml";
        // Paths to materials and textures that are used for Trackables and
        // Virtual Buttons.
        public const string PATH_TO_TARGET_TEXTURES =
            "Assets/Editor/QCAR/ImageTargetTextures/";
        public const string PATH_TO_REFERENCE_MATERIAL =
            "Assets/Qualcomm Augmented Reality/Materials/DefaultTarget.mat";
        public const string PATH_TO_MASK_MATERIAL =
            "Assets/Qualcomm Augmented Reality/Materials/DepthMask.mat";
        public const string VIRTUAL_BUTTON_MATERIAL_PATH =
            "Assets/Editor/QCAR/VirtualButtonTextures/" +
            "VirtualButtonPreviewMaterial.mat";
        // Path to the property file which stores QCAR specific settings.
        public const string PROPERTIES_FILE_PATH =
            "Assets/Editor/QCAR/Properties/prop.dat";
        // Default name used for Trackables that are not part of the config.xml
        // file yet.
        public const string DEFAULT_NAME = "--- EMPTY ---";
        // The theoretical maximum of Frame Markers that can be used in an
        // application.
        public const int MAX_NUM_FRAME_MARKERS = 512;
    }

    #endregion // NESTED



    #region PUBLIC_METHODS

    // Parses well formed strings to a Size vector.
    public static bool SizeFromStringArray(out Vector2 result,
                                           string[] valuesToParse)
    {
        result = Vector2.zero;

        // Check if we have the same number of elements for the Vector type and
        // the string array.
        bool areParamsOk = false;
        if (valuesToParse != null)
        {
            if (valuesToParse.Length == 2)
            {
                areParamsOk = true;
            }
        }

        if (!areParamsOk)
        {
            return false;
        }

        try
        {
            result = new Vector2(float.Parse(valuesToParse[0]),
                                 float.Parse(valuesToParse[1]));
        }
        catch
        {
            return false;
        }

        return true;
    }


    // Parses well formed strings to a Transform vector.
    public static bool TransformFromStringArray(out Vector3 result,
                                                string[] valuesToParse)
    {
        result = Vector3.zero;

        // Check if we have the same number of elements for the Vector type and
        // the string array.
        bool areParamsOk = false;
        if (valuesToParse != null)
        {
            if (valuesToParse.Length == 3)
            {
                areParamsOk = true;
            }
        }

        if (!areParamsOk)
        {
            return false;
        }

        try
        {
            result = new Vector3(float.Parse(valuesToParse[0]),
                                 float.Parse(valuesToParse[2]),
                                 float.Parse(valuesToParse[1]));
        }
        catch
        {
            return false;
        }

        return true;
    }


    // Parses well formed strings to a Rectangle vector.
    public static bool RectangleFromStringArray(out Vector4 result,
                                                string[] valuesToParse)
    {
        result = Vector4.zero;

        // Check if we have the same number of elements for the Vector type and
        // the string array.
        bool areParamsOk = false;
        if (valuesToParse != null)
        {
            if (valuesToParse.Length == 4)
            {
                areParamsOk = true;
            }
        }

        if (!areParamsOk)
        {
            return false;
        }

        try
        {
            result = new Vector4(float.Parse(valuesToParse[0]),
                                 float.Parse(valuesToParse[1]),
                                 float.Parse(valuesToParse[2]),
                                 float.Parse(valuesToParse[3]));
        }
        catch
        {
            return false;
        }

        return true;
    }


    // Parses well formed strings to a Orientation Quaternion.
    // This function is QCAR specific. It changes some of the number signs when
    // parsing.
    public static bool OrientationFromStringArray(out Quaternion result,
                                                  string[] valuesToParse)
    {
        result = Quaternion.identity;

        bool areParamsOk = false;
        if (valuesToParse != null)
        {
            if (valuesToParse.Length == 5)
            {
                areParamsOk = true;
            }
            else if (valuesToParse.Length == 4)
            {
                Debug.LogError("Direct parsing of Quaternions is not " +
                               "supported. Use Axis-Angle Degrees (AD:) or " +
                               "Axis-Angle Radians (AR:) instead.");
            }
        }

        if (!areParamsOk)
        {
            return false;
        }

        try
        {
            float angle = float.Parse(valuesToParse[4]);
            Vector3 axis = new Vector3(-float.Parse(valuesToParse[1]),
                                       float.Parse(valuesToParse[3]),
                                       -float.Parse(valuesToParse[2]));

            if (string.Compare(valuesToParse[0], "ad:", true) == 0)
            {
                result = Quaternion.AngleAxis(angle, axis);
            }
            else if (string.Compare(valuesToParse[0], "ar:", true) == 0)
            {
                result = Quaternion.AngleAxis(Mathf.Rad2Deg * angle, axis);
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        return true;
    }

    #endregion // PUBLIC_METHODS
}