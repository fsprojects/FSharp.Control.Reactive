#I "./packages/FAKE.1.64.6/tools"
#r "FakeLib.dll"

open Fake 

(* properties *)
let projectName = "FSharp.Reactive"
let version = if isLocalBuild then "2.0." + System.DateTime.UtcNow.ToString("yMMdd") else buildVersion
let projectSummary = "A F#-friendly wrapper for the Reactive Extensions."
let projectDescription = "A F#-friendly wrapper for the Reactive Extensions."
let authors = ["Ryan Riley"; "Steffen Forkmann"]
let mail = "ryan.riley@panesofglass.org"
let homepage = "https://github.com/panesofglass/FSharp.Reactive"

(* Directories *)
let buildDir = "./build/"
let packagesDir = "./packages/"
let docsDir = "./docs/" 
let deployDir = "./deploy/"
let testDir = "./test/"

let targetPlatformDir = getTargetPlatformDir "4.0.30319"

let nugetDir = "./nuget/"
let nugetLibDir = nugetDir @@ "lib/net40"
let nugetDocsDir = nugetDir @@ "docs"

let rxVersion = GetPackageVersion packagesDir "Rx-Main"

(* Params *)
let target = getBuildParamOrDefault "target" "All"

(* Tools *)
let fakePath = "./packages/FAKE.1.64.6/tools"
let nugetPath = "./.nuget/nuget.exe"
let nunitPath = "./packages/NUnit.Runners.2.6.0.12051/tools"

(* files *)
let appReferences =
    !+ "src/**/*.fsproj" 
        |> Scan

let testReferences =
    !+ "tests/**/*.fsproj" 
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
    if isLocalBuild then
        Git.Submodule.init "" ""

    AssemblyInfo (fun p -> 
          {p with
             CodeLanguage = FSharp
             AssemblyVersion = version
             AssemblyTitle = projectName
             AssemblyDescription = projectDescription
             Guid = "ED23F688-C0D0-4102-93D5-0D832633F66D"
             OutputFileName = "./src/AssemblyInfo.fs"})

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
    !+ (buildDir + "FSharp.Reactive.dll")
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

Target "BuildNuGet" (fun _ ->
    CleanDirs [nugetDir; nugetLibDir; nugetDocsDir]

    XCopy (docsDir |> FullName) nugetDocsDir
    [ buildDir + "FSharp.Reactive.dll"
      buildDir + "FSharp.Reactive.pdb"
      buildDir + "System.Reactive.Core.dll"
      buildDir + "System.Reactive.Interfaces.dll"
      buildDir + "System.Reactive.Linq.dll" ]
        |> CopyTo nugetLibDir

    NuGet (fun p ->
        {p with
            Authors = authors
            Project = projectName
            Description = projectDescription
            Version = version
            OutputPath = nugetDir
            Dependencies = ["Rx-Linq", RequireExactly rxVersion]
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            ToolPath = nugetPath
            Publish = hasBuildParam "nugetkey" })
        "FSharp.Reactive.nuspec"

    [nugetDir + sprintf "FSharp.Reactive.%s.nupkg" version]
        |> CopyTo deployDir
)

Target "DeployZip" (fun _ ->
    !! (buildDir + "/**/*.*")
    |> Zip buildDir (deployDir + sprintf "%s-%s.zip" projectName version)
)

FinalTarget "CloseTestRunner" (fun _ ->
    ProcessHelper.killProcess "nunit-agent.exe"
)

Target "Deploy" DoNothing
Target "All" DoNothing

(* Build Order *)
"Clean"
  ==> "BuildApp" <=> "BuildTest" <=> "CopyLicense"
  ==> "Test" <=> "GenerateDocumentation"
  ==> "ZipDocumentation"
  ==> "BuildNuGet"
  ==> "DeployZip"
  ==> "Deploy"

"All" <== ["Deploy"]

// start build
Run target
