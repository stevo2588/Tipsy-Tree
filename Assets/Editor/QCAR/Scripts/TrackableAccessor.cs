/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/


public abstract class TrackableAccessor
{
    #region PROTECTED_MEMBER_VARIABLES

    // Every accessor instance has only one dedicated TrackableBehaviour
    // instance that it is assigned to. It is referenced by this variable.
    protected TrackableBehaviour mTarget = null;

    #endregion // PROTECTED_MEMBER_VARIABLES



    #region PUBLIC_METHODS

    // This method is called when new configuration values are available and
    // need to be applied to Trackable objects in the scene.
    public abstract void ApplyConfigValues();

    #endregion // PUBLIC_METHODS
}
