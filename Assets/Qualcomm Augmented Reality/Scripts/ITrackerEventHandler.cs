/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

// Interface for handling tracker events.
public interface ITrackerEventHandler
{
    // Called after all the trackable objects have been updated:
    void OnTrackablesUpdated();
}
