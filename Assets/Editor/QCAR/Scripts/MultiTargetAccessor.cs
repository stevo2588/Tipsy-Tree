/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using System.Collections.Generic;
using UnityEditor;

public class MultiTargetAccessor : TrackableAccessor
{
    #region CONSTRUCTION

    // The one MultiTargetBehaviour instance this accessor belongs to is set in
    // the constructor.
    public MultiTargetAccessor(MultiTargetBehaviour target)
    {
        mTarget = target;
    }

    #endregion // CONSTRUCTION



    #region PUBLIC_METHODS

    // This method is called when new configuration values are available and
    // need to be applied to Multi Target objects in the scene.
    public override void ApplyConfigValues()
    {
        // Prefabs should not be editable
        if (EditorUtility.GetPrefabType(mTarget) == PrefabType.Prefab)
        {
            return;
        }

        MultiTargetBehaviour mtb = (MultiTargetBehaviour)mTarget;

        ConfigData.MultiTarget mtConfig;

        if (!SceneManager.Instance.GetMultiTarget(mtb.TrackableName, out mtConfig))
        {
            mtb.TrackableName = QCARUtilities.GlobalVars.DEFAULT_NAME;
        }

        List<ConfigData.MultiTargetPart> prtConfigs = mtConfig.parts;

        MultiTargetEditor.UpdateParts(mtb, prtConfigs.ToArray());
    }

    #endregion // PUBLIC_METHODS
}
