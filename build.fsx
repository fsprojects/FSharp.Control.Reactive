#r @"lib\FAKE\tools\FakeLib.dll"

open Fake 
open Fake.AssemblyInfoFile
open Fake.MSBuild

!! "./**/packages.config"
|> Seq.iter (RestorePackage (fun t -> { t with ToolPath = ".nuget/nuget.exe" }))

(* properties *)
let projectName = "FSharp.Reactive"
let version = if isLocalBuild then "2.1." + System.DateTime.UtcNow.ToString("yMMdd") else buildVersion
let projectSummary = "A F#-friendly wrapper for the Reactive Extensions."
let projectDescription = "A F#-friendly wrapper for the Reactive Extensions."
let authors = ["Ryan Riley"; "Steffen Forkmann"]
let mail = "ryan.riley@panesofglass.org"
let homepage = "https://github.com/fsharp/FSharp.Reactive"

(* Directories *)
let buildDir = "./build/"
let packagesDir = "./packages/"
let deployDir = "./deploy/"
let testDir = "./test/"

let nugetDir = "./nuget/"
let nugetLibDir = nugetDir @@ "lib/net40"

(* Tools *)
let nugetPath = ".Nuget/nuget.exe"
let nunitVersion = GetPackageVersion packagesDir "NUnit.Runners"
let nunitPath = sprintf "%sNUnit.Runners.%s/Tools" packagesDir nunitVersion

(* files *)
let appReferences =
    !! "src/**/*.fsproj" 

let testReferences =
    !! "tests/**/*.fsproj" 

let filesToZip =
    !! (buildDir + "/**/*.*") -- "*.zip"

(* Targets *)
Target "Clean" (fun _ ->
    CleanDirs [buildDir; testDir; deployDir; nugetDir; nugetLibDir]
)

Target "BuildApp" (fun _ -> 
    if not isLocalBuild then
        [ Attribute.Version(version)
          Attribute.Title(projectName)
          Attribute.Description(projectDescription)
          Attribute.Guid("ED23F688-C0D0-4102-93D5-0D832633F66D")
        ]
        |> CreateFSharpAssemblyInfo "src/AssemblyInfo.fs"

    MSBuildRelease buildDir "Build" appReferences
        |> Log "AppBuild-Output: "
)

Target "BuildTest" (fun _ -> 
    MSBuildDebug testDir "Build" testReferences
        |> Log "TestBuild-Output: "
)

Target "Test" (fun _ ->
    !! (testDir + "/*.Tests.dll")
        |> NUnit (fun p -> 
            {p with 
                ToolPath = nunitPath; 
                DisableShadowCopy = true;
                OutputFile = testDir + "TestResults.xml" })
)

Target "CopyLicense" (fun _ ->
    [ "LICENSE.txt" ] |> CopyTo buildDir
)

Target "BuildNuGet" (fun _ ->
    [ buildDir + "FSharp.Reactive.dll"
      buildDir + "FSharp.Reactive.pdb" ]
        |> CopyTo nugetLibDir

    let rxVersion = GetPackageVersion packagesDir "Rx-Linq"
    NuGet (fun p ->
        {p with
            Authors = authors
            Project = projectName
            Description = projectDescription
            Version = version
            OutputPath = nugetDir
            Dependencies = ["Rx-Linq", rxVersion]
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
Target "Default" DoNothing

(* Build Order *)
"Clean"
    ==> "BuildApp" <=> "BuildTest" <=> "CopyLicense"
    ==> "Test"
    ==> "BuildNuGet"
    ==> "DeployZip"
    ==> "Deploy"

"Default" <== ["Deploy"]

// start build
RunTargetOrDefault "Default"

