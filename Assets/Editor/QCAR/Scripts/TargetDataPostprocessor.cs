/*==============================================================================
            Copyright (c) 2010-2011 QUALCOMM Incorporated.
            All Rights Reserved.
            Qualcomm Confidential and Proprietary
==============================================================================*/

using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// The target data postprocessor class generates callbacks on file import of
// QCAR related files (tracking dataset, configuration file,
// Image Target textures)
public class TargetDataPostprocessor : AssetPostprocessor
{
    #region NESTED

    // The import state defines how a file has been modified on import.
    public enum ImportState
    {
        NONE,    // Default state. File was not imported.
        ADDED,   // File has not existed before and was therefore added.
        RENAMED, // File has existed before and was automatically renamed.
        DELETED  // File was not imported and an existing copy was kept.
    }

    #endregion // NESTED



    #region PRIVATE_MEMBER_VARIABLES

    // There is a state variable for every QCAR related file type that defines
    // how the import was handled.
    private static ImportState mWasConfigFileUpdated = ImportState.NONE;
    private static ImportState mWereTexturesUpdated  = ImportState.NONE;
    private static ImportState mWasDATFileUpdated    = ImportState.NONE;

    // When assets that already exist are imported in Unity they are renamed
    // automatically. The QCAR environment can only work with a single tracking
    // dataset and a single configuration file so this is not desired. If there
    // is an existing file either the newly imported one or the existing one is
    // deleted. The variables below store the pathsto the files that are
    // removed.
    private static string mUnwantedConfigXMLPath = null;
    private static string mUnwantedDATPath       = null;

    // This variable is set to true while the file import dialog is shown.
    // During that time imports are simply ignored.
    private static bool mProcessingReplacement = false;

    #endregion // PRIVATE_MEMBER_VARIABLES



    #region UNITY_EDITOR_METHODS

    // This method is called by Unity whenever assets are updated (deleted,
    // moved or added).
    public static void OnPostprocessAllAssets(string[] importedAssets,
                                              string[] deletedAssets,
                                              string[] movedAssets,
                                              string[] movedFromAssetPaths)
    {
        // We ignore new requests until we finished replacement actions.
        if (mProcessingReplacement)
        {
            return;
        }

        mWasConfigFileUpdated = ImportState.NONE;
        mWereTexturesUpdated  = ImportState.NONE;
        mWasDATFileUpdated    = ImportState.NONE;

        bool replacementDetected = false;

        // We are using regular expression to determine if a file has been
        // renamed by Unity.
        Regex configFileRegex = new Regex(
            QCARUtilities.GlobalVars.REGEXED_CONFIG_XML_PATH,
            RegexOptions.IgnoreCase);
        Regex datFileRegex = new Regex(
            QCARUtilities.GlobalVars.REGEXED_DAT_PATH,
            RegexOptions.IgnoreCase);

        // Check if there are relevant files that have been imported.
        foreach (string importedAsset in importedAssets)
        {
            if (importedAsset.IndexOf(
                QCARUtilities.GlobalVars.CONFIG_XML_PATH,
                System.StringComparison.OrdinalIgnoreCase) != -1)
            {
                mWasConfigFileUpdated = ImportState.ADDED;
            }
            else if (importedAsset.IndexOf(
                QCARUtilities.GlobalVars.PATH_TO_TARGET_TEXTURES,
                System.StringComparison.OrdinalIgnoreCase) != -1)
            {
                mWereTexturesUpdated = ImportState.ADDED;
            }
            else if (importedAsset.IndexOf(
                QCARUtilities.GlobalVars.DAT_PATH,
                System.StringComparison.OrdinalIgnoreCase) != -1)
            {
                mWasDATFileUpdated = ImportState.ADDED;
            }
            else if (configFileRegex.IsMatch(importedAsset))
            {
                replacementDetected = true;
                mWasConfigFileUpdated = ImportState.RENAMED;
                mUnwantedConfigXMLPath = importedAsset;
            }
            else if (datFileRegex.IsMatch(importedAsset))
            {
                replacementDetected = true;
                mWasDATFileUpdated = ImportState.RENAMED;
                mUnwantedDATPath = importedAsset;
            }
        }

        // Check if there are relevant files that have been deleted.
        foreach (string deletedAsset in deletedAssets)
        {
            if (deletedAsset.IndexOf(
                QCARUtilities.GlobalVars.CONFIG_XML_PATH,
                System.StringComparison.OrdinalIgnoreCase) != -1)
            {
                mWasConfigFileUpdated = ImportState.DELETED;
            }
            else if (deletedAsset.IndexOf(
                QCARUtilities.GlobalVars.PATH_TO_TARGET_TEXTURES,
                System.StringComparison.OrdinalIgnoreCase) != -1)
            {
                mWereTexturesUpdated = ImportState.DELETED;
            }
            else if (deletedAsset.IndexOf(
                QCARUtilities.GlobalVars.DAT_PATH,
                System.StringComparison.OrdinalIgnoreCase) != -1)
            {
                mWasDATFileUpdated = ImportState.DELETED;
            }
        }

        // If a file needs to be replaced we delay the update callback call
        // until the user has made a decision between keeping the old or the
        // new files.
        if (replacementDetected)
        {
            mProcessingReplacement = true;
            ImportConflictWindow.ShowWindow();
        }
        else
        {
            bool filesUpdated = ((mWasConfigFileUpdated != ImportState.NONE) ||
                                 (mWereTexturesUpdated  != ImportState.NONE) ||
                                 (mWasDATFileUpdated    != ImportState.NONE));

            // We only alert the SceneManager if files have actually been
            // changed.
            if (filesUpdated)
            {
                SceneManager.Instance.FilesUpdated(mWasConfigFileUpdated,
                                                   mWereTexturesUpdated,
                                                   mWasDATFileUpdated);
            }
        }
    }

    // This method is called from the ImportConflictWindow class after the user
    // has chosen what should happen with newly imported files.
    public static void HandleFileReplacements(bool replaceOld)
    {
        if (replaceOld)
        {
            if (mWasConfigFileUpdated == ImportState.RENAMED)
            {
                if (AssetDatabase.DeleteAsset(
                    QCARUtilities.GlobalVars.CONFIG_XML_PATH))
                {
                    string renameError = AssetDatabase.RenameAsset(
                        mUnwantedConfigXMLPath, "config");
                    if (renameError != "")
                    {
                        Debug.LogError("Rename operation returns: " +
                                       renameError);
                    }
                }
                else
                {
                    Debug.LogError("Could not delete asset with name: " +
                                   QCARUtilities.GlobalVars.CONFIG_XML_PATH);
                }
                mWasConfigFileUpdated = ImportState.ADDED;
            }

            if (mWasDATFileUpdated == ImportState.RENAMED)
            {
                if (AssetDatabase.DeleteAsset(
                    QCARUtilities.GlobalVars.DAT_PATH))
                {
                    string renameError = AssetDatabase.RenameAsset(
                        mUnwantedDATPath, "qcar-resources");
                    if (renameError != "")
                    {
                        Debug.LogError("Rename operation returns: " +
                                       renameError);
                    }
                }
                else
                {
                    Debug.LogError("Could not delete asset with name: " +
                                   QCARUtilities.GlobalVars.DAT_PATH);
                }
                mWasDATFileUpdated = ImportState.ADDED;
            }
        }
        else
        {
            if (mWasConfigFileUpdated == ImportState.RENAMED)
            {
                AssetDatabase.DeleteAsset(mUnwantedConfigXMLPath);
                mWasConfigFileUpdated = ImportState.ADDED;
            }

            if (mWasDATFileUpdated == ImportState.RENAMED)
            {
                AssetDatabase.DeleteAsset(mUnwantedDATPath);
                mWasDATFileUpdated = ImportState.ADDED;
            }
        }
        mUnwantedConfigXMLPath = null;
        mUnwantedDATPath = null;

        SceneManager.Instance.FilesUpdated(mWasConfigFileUpdated,
                                           mWereTexturesUpdated,
                                           mWasDATFileUpdated);
        mProcessingReplacement = false;
    }

    #endregion // UNITY_EDITOR_METHODS
}