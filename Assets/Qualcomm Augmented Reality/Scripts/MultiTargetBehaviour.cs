/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

// A trackable that is made up of multiple targets with a fixed spatial
// relation.
public class MultiTargetBehaviour : TrackableBehaviour
{
	#region CONSTRUCTION

    public MultiTargetBehaviour()
    {
        // Remove as soon as this is solved by type
        mTrackableType = TrackableType.MULTI_TARGET;
    }

    #endregion // CONSTRUCTION
}
