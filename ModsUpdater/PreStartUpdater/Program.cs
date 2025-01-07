using System;
using System.Collections.Generic;
using Mono.Cecil;

public static class PreStartUpdaterPatcher
{
    // List of assemblies to patch
    public static IEnumerable<string> TargetDLLs { get; } = new string[0] ;

    // Patches the assemblies
    public static void Patch(AssemblyDefinition assembly)
    {
        // Patcher code here
    }

    public static void Finish() {
        System.Diagnostics.Trace.WriteLine("We finished patching !!! Wooooooooooo");
        // Step 1. Check for a .pendingUpdates inside my mod folder
        // Step 2. Read from it, couples of PathsToBeDeleted and ZipToBeUnzipped
        // Step3 done


        // From the other side;
        // on game start, if pendingupdate is here:
        //      delete it, delete the zips, delete the patcher
        // when clicking th eupdate btn, we need to :
        // Download the remote update
        // Create/update .pendingUpdates (json) with PathToBeDeleted and ZipToBeUnzipped of that mod
        // (opt) put the preloader inside the patchers folder
        // disable upd btn; set upd text to pending restart
    }
}