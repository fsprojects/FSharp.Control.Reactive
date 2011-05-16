#I "./packages/FAKE.1.56.6/tools"
#r "FakeLib.dll"

open Fake 

(* properties *)
let projectName = "FSharp.Reactive"
let version = "1.1"  
let projectSummary = "A F#-friendly wrapper for the Reactive Extensions."
let projectDescription = "A F#-friendly wrapper for the Reactive Extensions."
let authors = ["Ryan Riley"]
let mail = "ryan.riley@panesofglass.org"
let homepage = "https://github.com/panesofglass/FSharp.Reactive"

(* Directories *)
let buildDir = "./build/"
let packagesDir = "./packages/"
let docsDir = "./docs/" 
let deployDir = "./deploy/"
let testDir = "./test/"
let targetPlatformDir = getTargetPlatformDir "4.0.30319"

(* Params *)
let target = getBuildParamOrDefault "target" "All"

(* Tools *)
let fakePath = "./packages/FAKE.1.56.6/tools"
let nunitPath = "./tools/Nunit"

(* files *)
let appReferences =
    !+ @"src\**\FSharp.Reactive.fsproj" 
        |> Scan

let testReferences =
    !+ @"src\**\FSharp.Reactive.Tests.fsproj" 
        |> Scan

let filesToZip =
  !+ (buildDir + "/**/*.*")     
      -- "*.zip"
      |> Scan      

(* Targets *)
Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir; deployDir; docsDir]
)

Target "BuildApp" (fun _ -> 
    AssemblyInfo (fun p -> 
          {p with
             CodeLanguage = FSharp
             AssemblyVersion = version
             AssemblyTitle = projectName
             AssemblyDescription = projectDescription
             Guid = "ED23F688-C0D0-4102-93D5-0D832633F66D"
             OutputFileName = "./src/FSharp.Reactive/AssemblyInfo.fs"})

    MSBuildRelease buildDir "Build" appReferences
      |> Log "AppBuild-Output: "
)

Target "BuildTest" (fun _ -> 
    MSBuildDebug testDir "Build" testReferences
      |> Log "TestBuild-Output: "
)

Target "Test" (fun _ ->
    !+ (testDir + "/*.Tests.dll")
      |> Scan
      |> NUnit (fun p -> 
          {p with 
             ToolPath = nunitPath; 
             DisableShadowCopy = true; 
             OutputFile = testDir + "TestResults.xml" }) 
)

Target "GenerateDocumentation" (fun _ ->
    !+ (buildDir + "*.dll")      
      |> Scan
      |> Docu (fun p ->
          {p with
             ToolPath = fakePath + "/docu.exe"
             TemplatesPath = "./lib/templates"
             OutputPath = docsDir })
)

Target "CopyLicense" (fun _ ->
    [ "LICENSE.txt" ] |> CopyTo buildDir
)

Target "ZipDocumentation" (fun _ ->    
    !+ (docsDir + "/**/*.*")
        |> Scan
        |> Zip docsDir (deployDir + sprintf "Documentation-%s.zip" version)
)

Target "Deploy" (fun _ ->
    !+ (buildDir + "/**/*.*")
        -- "*.zip"
        |> Scan
        |> Zip buildDir (deployDir + sprintf "%s-%s.zip" projectName version)
)

Target "All" DoNothing

(* Build Order *)
"Clean"
  ==> "BuildApp" <=> "BuildTest" <=> "CopyLicense"
  ==> "Test" <=> "GenerateDocumentation"
  ==> "ZipDocumentation"
  ==> "Deploy"

"All" <== ["Deploy"]

// start build
Run target
