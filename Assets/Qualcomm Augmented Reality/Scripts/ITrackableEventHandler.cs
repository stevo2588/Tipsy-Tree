/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

// Interface for handling trackable state changes.
public interface ITrackableEventHandler
{
    // Called when the trackable state has changed.
    void OnTrackableStateChanged(TrackableBehaviour.Status previousStatus,
                                    TrackableBehaviour.Status newStatus);
}
