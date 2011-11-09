/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using UnityEngine;

// A custom handler that implements the ITrackerEventHandler interface.
public class TrackerEventHandler : MonoBehaviour,
                                   ITrackerEventHandler
{
    #region UNTIY_MONOBEHAVIOUR_METHODS

    void Start()
    {
        TrackerBehaviour trackerBehaviour = GetComponent<TrackerBehaviour>();
        if (trackerBehaviour)
        {
            trackerBehaviour.RegisterTrackerEventHandler(this);
        }
    }

    #endregion // UNTIY_MONOBEHAVIOUR_METHODS



    #region PUBLIC_METHODS

    // Implementation of the ITrackerEventHandler function called after all
    // trackables have changed.
    public void OnTrackablesUpdated()
    {
        //Debug.Log("trackables updated");
    }

    #endregion // PUBLIC_METHODS
}
