using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
 
public static class PostBuildTrigger
{
    // Frameworks Ids  -  These ids have been generated by creating a project using Xcode then
    // extracting the values from the generated project.pbxproj.  The format of this
    // file is not documented by Apple so the correct algorithm for generating these
    // ids is unknown. They also differ from project to project, so it may not matter.

	const string FRAMEWORK_CORETELEPHONY = "CoreTelephony.framework";
	const string FRAMEWORK_ID_CORETELEPHONY = "4266CDD118907E7C00C4E70B";
	const string FRAMEWORK_FILEREFID_CORETELEPHONY = "4266CDD018907E7C00C4E70B";

    const string FRAMEWORK_IAD = "iAd.framework";
    const string FRAMEWORK_ID_IAD = "4266CE1C18907F5400C4E70B";
    const string FRAMEWORK_FILEREFID_IAD = "4266CE1B18907F5400C4E70B";

	const string FRAMEWORK_MOBILECORESERVICES = "MobileCoreServices.framework";
	const string FRAMEWORK_ID_MOBILECORESERVICES = "4266CDD318907E8500C4E70B";
	const string FRAMEWORK_FILEREFID_MOBILECORESERVICES = "4266CDD218907E8500C4E70B";

	const string FRAMEWORK_SYSTEMCONFIGURATION = "SystemConfiguration.framework";
	const string FRAMEWORK_ID_SYSTEMCONFIGURATION = "4266CDD518907E8F00C4E70B";
	const string FRAMEWORK_FILEREFID_SYSTEMCONFIGURATION = "4266CDD418907E8F00C4E70B";

	const string DEFAULT_FRAMEWORKS_FOLDER = "System/Library/Frameworks";
	
	const string DEFAULT_UNITY_IPHONE_PROJECT_NAME = "Unity-iPhone.xcodeproj";

    // custom class: framework
	public struct framework
    {
        public string sName;
	   	public string sId;
        public string sFileId;
		public string sPath;
		public bool sWeak;

        public framework(string name, string myId, string fileid, string path, bool weak)
        {
            sName = name;
            sId = myId;
            sFileId = fileid;
			sPath = path;
			sWeak = weak;
        }
    }

    /// Processbuild Function
    [PostProcessBuild] // this attribute causes the method to be automatically executed
	public static void OnPostProcessBuild(BuildTarget target, string path)
    {
		Debug.Log("New Post Processing Build: OnPostProcessBuild: path = " + path);

		Debug.Log("OnPostProcessBuild - START");

		// 1: Proceed only if this is an iOS build

#if UNITY_IPHONE
		
		string xcodeprojPath = Path.Combine(path, DEFAULT_UNITY_IPHONE_PROJECT_NAME);
		
      	Debug.Log("We found xcodeprojPath to be : " + xcodeprojPath);

		Dictionary<string, framework> dictFrameworks = new Dictionary<string, framework> ();

		// List of all the frameworks to be added to the project
		dictFrameworks.Add(FRAMEWORK_CORETELEPHONY, new framework (FRAMEWORK_CORETELEPHONY, FRAMEWORK_ID_CORETELEPHONY, FRAMEWORK_FILEREFID_CORETELEPHONY, null, false));
        dictFrameworks.Add(FRAMEWORK_IAD, new framework (FRAMEWORK_IAD, FRAMEWORK_ID_IAD, FRAMEWORK_FILEREFID_IAD, null, false));
        dictFrameworks.Add(FRAMEWORK_MOBILECORESERVICES, new framework (FRAMEWORK_MOBILECORESERVICES, FRAMEWORK_ID_MOBILECORESERVICES, FRAMEWORK_FILEREFID_MOBILECORESERVICES, null, false));
        dictFrameworks.Add(FRAMEWORK_SYSTEMCONFIGURATION, new framework (FRAMEWORK_SYSTEMCONFIGURATION, FRAMEWORK_ID_SYSTEMCONFIGURATION, FRAMEWORK_FILEREFID_SYSTEMCONFIGURATION, null, false));

		// 2: process our project

        updateXcodeProject(xcodeprojPath, dictFrameworks);
		
#else
        // 3: We do nothing if not iPhone
        Debug.Log("OnPostProcessBuild - Warning: No PostProcessing required. This is not an iOS build.");
#endif
        Debug.Log("OnPostProcessBuild - END");
    }
	
    // MAIN FUNCTION
	// xcodeproj_filename - filename of the Xcode project to change
	// frameworks - list of Apple standard frameworks to add to the project
	public static void updateXcodeProject(string xcodeprojPath, Dictionary<string, framework> dictFrameworks)
	{
		// STEP 1 :
        // Create an array of strings by reading in all lines from the xcode project file.

        string project = xcodeprojPath + "/project.pbxproj";
        string[] lines = System.IO.File.ReadAllLines(project);

        // STEP 2 :
		// Loop through the project file text and find out if all the required frameworks already exist.

        int i = 0;
        bool bFound = false;
		bool bEnd = false;

		bool existsCORETELEPHONY = false;
        bool existsIAD = false;
		bool existsMOBILECORESERVICES = false;
		bool existsSYSTEMCONFIGURATION = false;

		Debug.Log ("total frameworks required = " + dictFrameworks.Count);

        while (!bFound && !bEnd)
        {
            if (lines[i].Length > 5 && (string.Compare(lines[i].Substring(3, 3), "End") == 0) )
                bEnd = true;

			if (lines [i].Contains (FRAMEWORK_CORETELEPHONY)) {
				existsCORETELEPHONY = true;
				dictFrameworks.Remove (FRAMEWORK_CORETELEPHONY);
			}
            else if (lines [i].Contains (FRAMEWORK_IAD)) {
                existsIAD = true;
                dictFrameworks.Remove (FRAMEWORK_IAD);
            }
			else if (lines [i].Contains (FRAMEWORK_MOBILECORESERVICES)) {
				existsMOBILECORESERVICES = true;
				dictFrameworks.Remove (FRAMEWORK_MOBILECORESERVICES);
			}
			else if (lines [i].Contains (FRAMEWORK_SYSTEMCONFIGURATION)) {
				existsSYSTEMCONFIGURATION = true;
				dictFrameworks.Remove (FRAMEWORK_SYSTEMCONFIGURATION);
			}

			bFound = existsCORETELEPHONY && existsIAD && existsMOBILECORESERVICES && existsSYSTEMCONFIGURATION;

			++i;
        }

		Debug.Log ("frameworks to add = " + dictFrameworks.Count);

        if (bFound)
		{
            Debug.Log("OnPostProcessBuild - WARNING: The frameworks required by MobileAppTracker are already present in the Xcode project. Nothing to add.");
		}
        else
		{
            // STEP 3 :
			// Edit the project.pbxproj and include the missing frameworks required by MobileAppTracker.

            FileStream filestr = new FileStream(project, FileMode.Create); //Create new file and open it for read and write, if the file exists overwrite it.
            filestr.Close();

            StreamWriter fCurrentXcodeProjFile = new StreamWriter(project); // will be used for writing
			
            // As we iterate through the list we'll record which section of the
            // project.pbxproj we are currently in
            string section = string.Empty;
			
            // We use this boolean to decide whether we have already added the list of
			// build files to the link line.  This is needed because there could be multiple
			// build targets and they are not named in the project.pbxproj
			bool bFrameworks_build_added = false;

            i = 0;

            foreach (string line in lines)
			{
                fCurrentXcodeProjFile.WriteLine(line);

//////////////////////////////////
//  STEP 1 : Include Frameworks  //
//////////////////////////////////

// Each section starts with a comment such as : /* Begin PBXBuildFile section */'
                if ( line.Length > 7 && string.Compare(line.Substring(3, 5), "Begin") == 0  )
                {
                    section = line.Split(' ')[2];

                    Debug.Log("NEW_SECTION: " + section);

                    if (section == "PBXBuildFile")
                    {
						// Add one entry for each framework to the PBXBuildFile section
						// Loop over pairs with foreach
						foreach (KeyValuePair<string, framework> pair in dictFrameworks)
						{
							framework fr = pair.Value;
                            add_build_file(fCurrentXcodeProjFile, fr.sId, fr.sName, fr.sFileId, fr.sWeak);
						}
                    }

                    if (section == "PBXFileReference")
                    {
						// Add one entry for each framework to the PBXFileReference section
						// Loop over pairs with foreach
						foreach (KeyValuePair<string, framework> pair in dictFrameworks)
						{
							framework fr = pair.Value;
                            add_framework_file_reference(fCurrentXcodeProjFile, fr.sFileId, fr.sName, fr.sPath);
						}
                    }

                    if (line.Length > 5 && string.Compare(line.Substring(3, 3), "End") == 0)
					{
                        section = string.Empty;
					}
                }

// The PBXResourcesBuildPhase section is what appears in XCode as 'Link
// Binary With Libraries'.  As with the frameworks we make the assumption the
// first target is always 'Unity-iPhone' as the name of the target itself is
// not listed in project.pbxproj                

                if (section == "PBXFrameworksBuildPhase" 
					&& line.Trim().Length > 4 
					&& string.Compare(line.Trim().Substring(0, 5) , "files") == 0 
					&& !bFrameworks_build_added)
                {
					// Add one entry for each framework to the PBXFrameworksBuildPhase section
					// Loop over pairs with foreach
					foreach (KeyValuePair<string, framework> pair in dictFrameworks)
					{
						framework fr = pair.Value;
                        add_frameworks_build_phase(fCurrentXcodeProjFile, fr.sId, fr.sName);
					}

                    bFrameworks_build_added = true;
                }

// The PBXGroup is the section that appears in XCode as 'Copy Bundle Resources'.
                if (section == "PBXGroup" 
					&& line.Trim().Length > 7 
					&& string.Compare(line.Trim().Substring(0, 8) , "children") == 0 
					&& lines[i-2].Trim().Split(' ').Length > 0 
					&& string.Compare(lines[i-2].Trim().Split(' ')[2] , "CustomTemplate" ) == 0 )
                {
					Debug.Log("Adding frameworks in PBXGroup");
					// Loop over pairs with foreach
					foreach (KeyValuePair<string, framework> pair in dictFrameworks)
					{
						framework fr = pair.Value;
						Debug.Log(fr.sName);
                        add_group(fCurrentXcodeProjFile, fr.sFileId, fr.sName);
					}
                }

//////////////////////////////
//  STEP 2 : Build Options  //
//////////////////////////////
				
				if (section == "XCBuildConfiguration" 
					&& line.StartsWith("\t\t\t\tOTHER_CFLAGS"))
                {
					Debug.Log("OnPostProcessBuild - Adding FRAMEWORK_SEARCH_PATHS");
					
					// Add "." to the framework search path
                    fCurrentXcodeProjFile.Write("\t\t\t\tFRAMEWORK_SEARCH_PATHS = (\n\t\t\t\t\t\"$(inherited)\",\n\t\t\t\t\t\"\\\"$(SRCROOT)/" + "." + "\\\"\",\n\t\t\t\t);\n");
                }
                ++i;
            }

            fCurrentXcodeProjFile.Close();
        }
    }

    /////////////////
    ///////////
    // ROUTINES
    ///////////
    /////////////////

    // Adds a line into the PBXBuildFile section
    private static void add_build_file(StreamWriter file, string id, string name, string fileref, bool weak)
    {
        Debug.Log("OnPostProcessBuild - Adding build file " + name);
        string subsection = "Frameworks";
		
		string settings = weak ? "settings = {ATTRIBUTES = (Weak, ); }; " : string.Empty;
		
        file.Write("\t\t" + id + " /* " + name + " in " + subsection + " */ = {isa = PBXBuildFile; fileRef = " + fileref + " /* " + name + " */; " + settings + "};\n");
    }

    // Adds a line into the PBXBuildFile section
	private static void add_framework_file_reference(StreamWriter file, string id, string name, string path)
    {
        Debug.Log("OnPostProcessBuild - Adding framework file reference " + name);
		
		string sourceTree = null;
		
		if(null == path)
		{
			sourceTree = "SDKROOT";
			path = DEFAULT_FRAMEWORKS_FOLDER; // all the frameworks come from here
		}
		else
		{
			sourceTree = "\"<group>\"";
		}
		
        if (name == "libsqlite3.0.dylib")           // except for lidsqlite
            path = "usr/lib";
	
	    file.Write("\t\t" + id + " /* " + name + " */ = {isa = PBXFileReference; lastKnownFileType = wrapper.framework; name = " + name + "; path = \"" + path + "/" + name + "\"; sourceTree = "+ sourceTree + "; };\n");
    }

    // Adds a line into the PBXFrameworksBuildPhase section
    private static void add_frameworks_build_phase(StreamWriter file, string id, string name)
    {
        Debug.Log("OnPostProcessBuild - Adding build phase " + name);

        file.Write("\t\t\t\t" + id + " /* " + name + " in Frameworks */,\n");
    }

    // Adds a line into the PBXGroup section
    private static void add_group(StreamWriter file, string id, string name)
    {
        Debug.Log("OnPostProcessBuild - Add group " + name);

        file.Write("\t\t\t\t" + id + " /* " + name + " */,\n");
    }
}