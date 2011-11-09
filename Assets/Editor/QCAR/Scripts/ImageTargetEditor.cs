/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ImageTargetBehaviour))]
public class ImageTargetEditor : Editor
{
    #region PUBLIC_METHODS

    // Recalculates the aspect ratio of the Image Target from a size vector.
    // Automatically updates mesh as well.
    public static void UpdateAspectRatio(ImageTargetBehaviour it, Vector2 size)
    {
        it.AspectRatio = size[1] / size[0];
        UpdateMesh(it);
    }


    // Updates the scale values in the transform component from a given size.
    public static void UpdateScale(ImageTargetBehaviour it, Vector2 size)
    {
        // Update the scale:

        float childScaleFactor = it.GetSize()[0] / size[0];

        if (it.AspectRatio <= 1.0f)
        {
            it.transform.localScale = new Vector3(size[0], size[0], size[0]);
        }
        else
        {
            it.transform.localScale = new Vector3(size[1], size[1], size[1]);
        }

        // Check if 3D content should keep its size or if it should be scaled
        // with the target.
        if (it.mPreserveChildSize)
        {
            foreach (Transform child in it.transform)
            {
                child.localPosition =
                    new Vector3(child.localPosition.x * childScaleFactor,
                                child.localPosition.y * childScaleFactor,
                                child.localPosition.z * childScaleFactor);

                child.localScale =
                    new Vector3(child.localScale.x * childScaleFactor,
                                child.localScale.y * childScaleFactor,
                                child.localScale.z * childScaleFactor);
            }
        }
    }


    // Assign material and texture to Image Target.
    public static void UpdateMaterial(ImageTargetBehaviour it)
    {
        // Load reference material.
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

        // Load texture from texture folder. Textures have per convention the
        // same name as Image Targets + "_scaled" as postfix.
        string pathToTexture = QCARUtilities.GlobalVars.PATH_TO_TARGET_TEXTURES;
        string textureFile = pathToTexture + it.TrackableName + "_scaled";

        if (File.Exists(textureFile + ".png"))
            textureFile += ".png";
        else if (File.Exists(textureFile + ".jpg"))
            textureFile += ".jpg";

        Texture2D targetTexture =
            (Texture2D)AssetDatabase.LoadAssetAtPath(textureFile,
                                                     typeof(Texture2D));
        if (targetTexture == null)
        {
            // If the texture is null we simply assign a default material.
            it.GetComponent<Renderer>().sharedMaterial = referenceMaterial;
            return;
        }

        // We create a new material that is based on the reference material but
        // also contains a texture.
        Material materialForTargetTexture = new Material(referenceMaterial);
        materialForTargetTexture.mainTexture = targetTexture;
        materialForTargetTexture.name = targetTexture.name + "Material";
        materialForTargetTexture.mainTextureScale = new Vector2(1, 1);

        it.GetComponent<Renderer>().sharedMaterial = materialForTargetTexture;

        // Cleanup assets that have been created temporarily.
        EditorUtility.UnloadUnusedAssets();
    }


    // Update Virtual Buttons from configuration data.
    public static void UpdateVirtualButtons(ImageTargetBehaviour it,
                                            ConfigData.VirtualButton[] vbs)
    {
        for (int i = 0; i < vbs.Length; ++i)
        {
            // Developer is responsible for deleting Virtual Buttons that
            // are not specified in newly imported config.xml files.
            VirtualButtonBehaviour[] vbBehaviours =
                it.GetComponentsInChildren<VirtualButtonBehaviour>();

            bool vbInScene = false;
            for (int j = 0; j < vbBehaviours.Length; ++j)
            {
                // If the Virtual Button specified in the config.xml file
                // is already part of the scene we fill it with new values.
                if (vbBehaviours[j].VirtualButtonName == vbs[i].name)
                {
                    vbBehaviours[j].enabled = vbs[i].enabled;
                    vbBehaviours[j].SensitivitySetting = vbs[i].sensitivity;
                    VirtualButtonEditor.SetPosAndScaleFromButtonArea(
                        new Vector2(vbs[i].rectangle[0], vbs[i].rectangle[1]),
                        new Vector2(vbs[i].rectangle[2], vbs[i].rectangle[3]),
                        it,
                        vbBehaviours[j]);
                    vbInScene = true;
                }
            }
            if (vbInScene)
            {
                continue;
            }

            // If Virtual Button is not part of the scene we create
            // a new one and add it as a direct child of the ImageTarget.
            ImageTargetEditor.AddVirtualButton(it, vbs[i]);
        }
    }


    // Add Virtual Buttons that are specified in the configuration data.
    public static void AddVirtualButtons(ImageTargetBehaviour it,
                                         ConfigData.VirtualButton[] vbs)
    {
        for (int i = 0; i < vbs.Length; ++i)
        {
            AddVirtualButton(it, vbs[i]);
        }
    }

    #endregion // PUBLIC_METHODS



    #region UNITY_EDITOR_METHODS

    // Initializes the Image Target when it is drag-dropped into the scene.
    public void OnEnable()
    {
        ImageTargetBehaviour itb = (ImageTargetBehaviour) target;

        // We don't want to initialize if this is a prefab.
        if (EditorUtility.GetPrefabType(itb) == PrefabType.Prefab)
        {
            return;
        }

        // Only setup target if it has not been set up previously.
        if (!itb.mInitializedInEditor)
        {
            ConfigData.ImageTarget itConfig;

            SceneManager.Instance.GetImageTarget(itb.TrackableName, out itConfig);

            UpdateAspectRatio(itb, itConfig.size);
            UpdateScale(itb, itConfig.size);
            UpdateMaterial(itb);
            itb.TrackableName = QCARUtilities.GlobalVars.DEFAULT_NAME;
            itb.mInitializedInEditor = true;
        }

        // Cache the current scale of the target:
        itb.mPreviousScale = itb.transform.localScale;

        // Make sure the scene and config.xml file are synchronized.
        if (!SceneManager.Instance.SceneInitialized)
        {
            SceneManager.Instance.InitScene();
        }

        // Let the scene manager know:
        SceneManager.Instance.SceneUpdated();
    }


    // Lets the user choose a Image Target from a drop down list. Image Target
    // must be defined in the "config.xml" file.
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ImageTargetBehaviour itb = (ImageTargetBehaviour)target;

        if (EditorUtility.GetPrefabType(itb) == PrefabType.Prefab)
        {
            GUILayout.Label("You can't choose a target for a prefab.");
        }
        else if (SceneManager.Instance.GetNumAvailableTrackables(
            target.GetType()) > 0)
        {
            string[] namesList = SceneManager.Instance.GetImageTargetNames();
            int currentTrackableIndex =
                GetIndexFromName(itb.TrackableName, namesList);

            int newTrackableIndex = EditorGUILayout.Popup("Image Target",
                                                          currentTrackableIndex,
                                                          namesList);

            itb.mPreserveChildSize =
                EditorGUILayout.Toggle("Preserve child size",
                                       itb.mPreserveChildSize);

            if (newTrackableIndex != currentTrackableIndex)
            {
                itb.TrackableName = namesList[newTrackableIndex];

                ConfigData.ImageTarget itConfig;

                SceneManager.Instance.GetImageTarget(itb.TrackableName, out itConfig);

                // Update the aspect ratio and mesh used for visualisation:
                UpdateAspectRatio(itb, itConfig.size);

                // Update the scale:
                UpdateScale(itb, itConfig.size);

                // Update the material:
                UpdateMaterial(itb);

                // Remove currently set Virtual Buttons from target
                VirtualButtonBehaviour[] vbBehaviours =
                    itb.GetComponentsInChildren<VirtualButtonBehaviour>();

                foreach (VirtualButtonBehaviour vb in vbBehaviours)
                {
                    Debug.Log("Destroying VBs.");
                    Debug.Log("VB Array Length: " +
                              itConfig.virtualButtons.Count);
                    DestroyImmediate(vb.gameObject);
                }

                // Update Virtual Buttons
                AddVirtualButtons(itb, itConfig.virtualButtons.ToArray());
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
            EditorUtility.SetDirty(itb);

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


    private static void AddVirtualButton(ImageTargetBehaviour it,
                                         ConfigData.VirtualButton vb)
    {
        VirtualButtonBehaviour newVBBehaviour =
            it.CreateVirtualButton(vb.name, new Vector2(0.0f, 0.0f),
                           new Vector2(1.0f, 1.0f));

        VirtualButtonEditor.SetPosAndScaleFromButtonArea(
            new Vector2(vb.rectangle[0], vb.rectangle[1]),
            new Vector2(vb.rectangle[2], vb.rectangle[3]),
            it,
            newVBBehaviour);

        VirtualButtonEditor.CreateVBMesh(newVBBehaviour);

        // Load default material.
        VirtualButtonEditor.CreateMaterial(newVBBehaviour);

        newVBBehaviour.enabled = vb.enabled;

        // Add Component to destroy VirtualButton meshes at runtime.
        newVBBehaviour.gameObject.AddComponent<TurnOffBehaviour>();

        // Make sure Virtual Button is correctly aligned with Image Target
        newVBBehaviour.UpdatePose();
    }


    private static void UpdateMesh(ImageTargetBehaviour it)
    {
        GameObject itObject = it.gameObject;

        MeshFilter meshFilter = itObject.GetComponent<MeshFilter>();
        if (!meshFilter)
        {
            meshFilter = itObject.AddComponent<MeshFilter>();
        }

        Vector3 p0, p1, p2, p3;

        if (it.AspectRatio <= 1.0f)
        {
            p0 = new Vector3(-0.5f, 0, -it.AspectRatio * 0.5f);
            p1 = new Vector3(-0.5f, 0, it.AspectRatio * 0.5f);
            p2 = new Vector3(0.5f, 0, -it.AspectRatio * 0.5f);
            p3 = new Vector3(0.5f, 0, it.AspectRatio * 0.5f);
        }
        else
        {
            float aspectRationInv = 1.0f / it.AspectRatio;

            p0 = new Vector3(-aspectRationInv * 0.5f, 0, -0.5f);
            p1 = new Vector3(-aspectRationInv * 0.5f, 0, 0.5f);
            p2 = new Vector3(aspectRationInv * 0.5f, 0, -0.5f);
            p3 = new Vector3(aspectRationInv * 0.5f, 0, 0.5f);
        }

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { p0, p1, p2, p3 };
        mesh.triangles = new int[]  {
                                        0,1,2,
                                        2,1,3
                                    };

        mesh.normals = new Vector3[mesh.vertices.Length];
        mesh.uv = new Vector2[]{
                new Vector2(0,0),
                new Vector2(0,1),
                new Vector2(1,0),
                new Vector2(1,1)
                };

        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = itObject.GetComponent<MeshRenderer>();
        if (!meshRenderer)
        {
            meshRenderer = itObject.AddComponent<MeshRenderer>();
        }

        // Cleanup assets that have been created temporarily.
        EditorUtility.UnloadUnusedAssets();
    }

    #endregion // PRIVATE_METHODS
}