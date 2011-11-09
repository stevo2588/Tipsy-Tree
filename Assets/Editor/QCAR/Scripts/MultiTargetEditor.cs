/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MultiTargetBehaviour))]
public class MultiTargetEditor : Editor
{
    #region PUBLIC_METHODS

    // Updates MultiTarget parts with the values stored in the "prtConfigs"
    // array. Deletes all parts and recreates them.
    public static void UpdateParts(MultiTargetBehaviour mt,
                                   ConfigData.MultiTargetPart[] prtConfigs)
    {
        Transform childTargets = mt.transform.Find("ChildTargets");

        if (childTargets != null)
        {
            Object.DestroyImmediate(childTargets.gameObject);
        }

        GameObject newObject = new GameObject();
        newObject.name = "ChildTargets";
        newObject.transform.parent = mt.transform;
        newObject.hideFlags = HideFlags.NotEditable;

        newObject.transform.localPosition = Vector3.zero;
        newObject.transform.localRotation = Quaternion.identity;
        newObject.transform.localScale = Vector3.one;

        childTargets = newObject.transform;

        Material maskMaterial =
            (Material)AssetDatabase.LoadAssetAtPath(
                QCARUtilities.GlobalVars.PATH_TO_MASK_MATERIAL,
                typeof(Material));

        int numParts = prtConfigs.Length;
        for (int i = 0; i < numParts; ++i)
        {
            ConfigData.ImageTarget itConfig;

            if (!SceneManager.Instance.GetImageTarget(prtConfigs[i].name, out itConfig) &&
                prtConfigs[i].name != QCARUtilities.GlobalVars.DEFAULT_NAME)
            {
                Debug.LogError("No image target named " + prtConfigs[i].name);
                return;
            }

            Vector2 size = itConfig.size;
            Vector3 scale = new Vector3(size.x * 0.1f, 1, size.y * 0.1f);

            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = prtConfigs[i].name;
            plane.transform.parent = childTargets.transform;

            plane.transform.localPosition = prtConfigs[i].translation;
            plane.transform.localRotation = prtConfigs[i].rotation;
            plane.transform.localScale = scale;

            UpdateMaterial(plane);

            plane.hideFlags = HideFlags.NotEditable;

            MaskOutBehaviour script =
                (MaskOutBehaviour)plane.AddComponent(typeof(MaskOutBehaviour));
            script.maskMaterial = maskMaterial;
        }
    }

    #endregion // PUBLIC_METHODS



    #region UNITY_EDITOR_METHODS

    // Initializes the Multi Target when it is drag-dropped into the scene.
    public void OnEnable()
    {
        MultiTargetBehaviour mtb = (MultiTargetBehaviour)target;

        // We don't want to initialize if this is a prefab.
        if (EditorUtility.GetPrefabType(mtb) == PrefabType.Prefab)
        {
            return;
        }

        // Only setup target if it has not been set up previously.
        if (!mtb.mInitializedInEditor)
        {
            ConfigData.MultiTarget mtConfig;

            SceneManager.Instance.GetMultiTarget(mtb.TrackableName, out mtConfig);

            List<ConfigData.MultiTargetPart> prtConfigs = mtConfig.parts;

            UpdateParts(mtb, prtConfigs.ToArray());
            mtb.mInitializedInEditor = true;
        }

        // Cache the current scale of the target:
        mtb.mPreviousScale = mtb.transform.localScale;

        // Make sure the scene and config.xml file are synchronized.
        if (!SceneManager.Instance.SceneInitialized)
        {
            SceneManager.Instance.InitScene();
        }

        // Let the scene manager know:
        SceneManager.Instance.SceneUpdated();
    }


    // Checks if the transformation of the Multi Target has been changed by
    // Unity transform-handles in scene view.
    // This is also called when user changes attributes in Inspector.
    public void OnSceneGUI()
    {
        TrackableBehaviour trackableBehaviour = (TrackableBehaviour)target;

        if (trackableBehaviour.transform.localScale.x != 1.0f ||
            trackableBehaviour.transform.localScale.y != 1.0f ||
            trackableBehaviour.transform.localScale.z != 1.0f)
        {
            Debug.LogError("You cannot scale a Multi target in the editor. " +
                           "Please edit the config.xml file to scale this " +
                           "target.");
            trackableBehaviour.transform.localScale =
                new Vector3(1.0f, 1.0f, 1.0f);
        }
    }


    // Lets the user choose a Multi Target from a drop down list. Multi Target
    // must be defined in the "config.xml" file.
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MultiTargetBehaviour multiTargetBehaviour =
            (MultiTargetBehaviour)target;

        if (EditorUtility.GetPrefabType(multiTargetBehaviour) ==
            PrefabType.Prefab)
        {
            GUILayout.Label("You can't choose a target for a prefab.");
        }
        else if (SceneManager.Instance.GetNumAvailableTrackables(
            target.GetType()) > 0)
        {

            string[] namesList = SceneManager.Instance.GetMultiTargetNames();
            int currentTrackableIndex =
                GetIndexFromName(multiTargetBehaviour.TrackableName, namesList);

            int newTrackableIndex = EditorGUILayout.Popup("Multi Target",
                                                          currentTrackableIndex,
                                                          namesList);

            if (newTrackableIndex != currentTrackableIndex)
            {
                multiTargetBehaviour.TrackableName =
                    namesList[newTrackableIndex];
            }
        }
        else
        {
            if (GUILayout.Button("No targets defined. Press here for target " +
                                 "creation!"))
            {
                SceneManager.Instance.GoToARPage();
            }
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(multiTargetBehaviour);

            // If name has changed we apply the correct values from the config
            // file.
            TrackableAccessor accessor =
                AccessorFactory.Create(multiTargetBehaviour);
            accessor.ApplyConfigValues();
            SceneManager.Instance.SceneUpdated();
        }
    }

    #endregion // UNITY_EDITOR_METHODS



    #region PRIVATE_METHODS

    // This method returns a valid index even if no target was found.
    private static int GetIndexFromName(string name, string[] availableNames)
    {
        int index = 0;
        for (int i = 0; i < availableNames.Length; ++i)
        {
            if (string.Compare(availableNames[i], name, true) == 0)
            {
                index = i;
            }
        }
        return index;
    }


    private static void UpdateMaterial(GameObject go)
    {
        // Load reference material
        string referenceMaterialPath =
            QCARUtilities.GlobalVars.PATH_TO_REFERENCE_MATERIAL;
        Material referenceMaterial =
            (Material)AssetDatabase.LoadAssetAtPath(referenceMaterialPath,
                                                    typeof(Material));
        if (referenceMaterial == null)
        {
            Debug.LogError("Could not find reference material at " +
                           referenceMaterialPath +
                           " please reimport Unity package.");
            return;
        }

        // Load texture from texture folder
        string pathToTexture = QCARUtilities.GlobalVars.PATH_TO_TARGET_TEXTURES;
        string textureFile = pathToTexture + go.name + "_scaled";

        if (File.Exists(textureFile + ".png"))
            textureFile += ".png";
        else if (File.Exists(textureFile + ".jpg"))
            textureFile += ".jpg";

        Texture2D targetTexture =
            (Texture2D)AssetDatabase.LoadAssetAtPath(textureFile,
                                                     typeof(Texture2D));
        if (targetTexture == null)
        {
            // If the texture is null we simply assign a default material
            go.GetComponent<Renderer>().sharedMaterial = referenceMaterial;
            return;
        }

        // We create a new material based on the reference material
        Material materialForTargetTexture = new Material(referenceMaterial);
        materialForTargetTexture.mainTexture = targetTexture;
        materialForTargetTexture.name = targetTexture.name + "Material";
        materialForTargetTexture.mainTextureScale = new Vector2(-1, -1);

        go.GetComponent<Renderer>().sharedMaterial = materialForTargetTexture;

        // Cleanup assets that have been created temporarily.
        EditorUtility.UnloadUnusedAssets();
    }

    #endregion // PRIVATE_METHODS
}