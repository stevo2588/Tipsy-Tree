/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using UnityEditor;
using UnityEngine;

public class ImageTargetAccessor : TrackableAccessor
{
    #region CONSTRUCTION

    // The one ImageTargetBehaviour instance this accessor belongs to is set in
    // the constructor.
    public ImageTargetAccessor(ImageTargetBehaviour target)
    {
        mTarget = target;
    }

    #endregion // CONSTRUCTION



    #region PUBLIC_METHODS

    // This method is called when new configuration values are available and
    // need to be applied to Image Target objects in the scene.
    public override void ApplyConfigValues()
    {
        // Prefabs should not be changed
        if (EditorUtility.GetPrefabType(mTarget) == PrefabType.Prefab)
        {
            Debug.Log("Prefab object found.");
            return;
        }

        // Update the aspect ratio, visualization and scale of the target:
        ImageTargetBehaviour itb = (ImageTargetBehaviour)mTarget;

        ConfigData.ImageTarget itConfig;

        if (!SceneManager.Instance.GetImageTarget(itb.TrackableName, out itConfig))
        {
            itb.TrackableName = QCARUtilities.GlobalVars.DEFAULT_NAME;
        }

        ImageTargetEditor.UpdateAspectRatio(itb, itConfig.size);
        ImageTargetEditor.UpdateScale(itb, itConfig.size);
        ImageTargetEditor.UpdateMaterial(itb);
        ImageTargetEditor.UpdateVirtualButtons(itb,
            itConfig.virtualButtons.ToArray());
    }

    #endregion // PUBLIC_METHODS
}
