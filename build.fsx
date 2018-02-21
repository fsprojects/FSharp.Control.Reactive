// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#I "packages/build/FAKE/tools"
#r "NuGet.Core.dll"
#r "FakeLib.dll"
open System
open System.IO
open Fake 
open Fake.Git
open Fake.ReleaseNotesHelper

// --------------------------------------------------------------------------------------
// Provide project-specific details below
// --------------------------------------------------------------------------------------

let projectSource = "src/FSharp.Control.Reactive"
let projectTesting = "src/FSharp.Control.Reactive.Testing"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted 
let gitHome = "git@github.com:fsprojects"
// The name of the project on GitHub
let gitName = "FSharp.Control.Reactive"
let gitRaw = environVarOrDefault "gitRaw" "https://raw.github.com/fsprojects"

let buildDir = Path.Combine(Environment.CurrentDirectory, "bin")


// --------------------------------------------------------------------------------------
// The rest of the file includes standard build steps 
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let (!!) includes = (!! includes).SetBaseDirectory __SOURCE_DIRECTORY__
let release = parseReleaseNotes (IO.File.ReadAllLines "RELEASE_NOTES.md")
let isAppVeyorBuild = environVar "APPVEYOR" |> isNull |> not
let isTaggedBuild = environVarOrDefault "APPVEYOR_REPO_TAG" "false" |> Boolean.Parse
let versionSuffix =
    if isTaggedBuild then ""
    elif buildVersion = "LocalBuild" then "-LocalBuild"
    else "-beta" + buildVersion
let nugetVersion = release.NugetVersion + versionSuffix

// Generate assembly info files with the right version & up-to-date information
Target "BuildVersion" (fun _ ->
    Shell.Exec("appveyor", sprintf "UpdateBuild -Version \"%s\"" nugetVersion) |> ignore
)

// --------------------------------------------------------------------------------------
// Clean build results & restore NuGet packages

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp"]
)

Target "CleanDocs" (fun _ ->
    CleanDirs ["docs/output"]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    // We need to invoke restore in order to create the .NetCore obj files
    DotNetCli.Build (fun defaults ->
        { defaults with
            Project = projectSource
            Configuration = "Release" })
    DotNetCli.Build (fun defaults ->
        { defaults with
            Project = projectTesting
            Configuration = "Release" })
)

Target "CopyLicense" (fun _ ->
    [ "LICENSE.txt" ] |> CopyTo "bin"
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner

Target "RunTests" (fun _ ->
    try
        DotNetCli.Test(fun p ->
            { p with
                Project = "tests"
                Configuration = "Release"
                TimeOut = TimeSpan.FromMinutes 20. })
    finally
        AppVeyor.UploadTestResultsXml AppVeyor.TestResultsType.NUnit "bin"
)

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target "Pack" (fun _ ->
    DotNetCli.Pack (fun p ->
        { p with
            Project = projectSource
            OutputPath = buildDir
            AdditionalArgs =
              [ "--no-build"
                sprintf "/p:Version=%s" release.NugetVersion
                //"/p:ReleaseNotes=" + (toLines release.Notes)
              ]
        })

    DotNetCli.Pack (fun p ->
        { p with
            Project = projectTesting
            OutputPath = buildDir
            AdditionalArgs =
              [ "--no-build"
                "--no-dependencies"
                sprintf "/p:Version=%s" release.NugetVersion
                //"/p:ReleaseNotes=" + (toLines release.Notes)
              ]
        })
)

Target "Push" (fun _ ->
    Paket.Push (fun p -> 
        { p with
            WorkingDir = buildDir }) 
)

// --------------------------------------------------------------------------------------
// Generate the documentation

Target "GenerateReferenceDocs" (fun _ ->
    if not <| executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"; "--define:REFERENCE"] [] then
      failwith "generating reference documentation failed"
)

Target "GenerateHelp" (fun _ ->
    if not <| executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"; "--define:HELP"] [] then
      failwith "generating help documentation failed"
)

Target "GenerateDocs" DoNothing

// --------------------------------------------------------------------------------------
// Release Scripts

Target "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    CleanDir tempDocsDir
    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

    CopyRecursive "docs/output" tempDocsDir true |> tracefn "%A"
    StageAll tempDocsDir
    Commit tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
    Branches.push tempDocsDir
)

Target "Release" (fun _ ->
    StageAll ""
    Commit "" (sprintf "Bump version to %s" release.NugetVersion)
    Branches.push ""

    Branches.tag "" release.NugetVersion
    Branches.pushTag "" "origin" release.NugetVersion
)

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing

"Clean"
  =?> ("BuildVersion", isAppVeyorBuild)
  ==> "CopyLicense"
  ==> "Build"
  ==> "RunTests"
  ==> "All"
  =?> ("GenerateReferenceDocs",isLocalBuild && not isMono)
  =?> ("GenerateDocs"         ,isLocalBuild && not isMono)
  =?> ("ReleaseDocs"          ,isLocalBuild && not isMono)

"All" 
  ==> "Pack"
  ==> "Push"

"CleanDocs"
  ==> "GenerateHelp"
  ==> "GenerateReferenceDocs"
  ==> "GenerateDocs"
    
"ReleaseDocs"
  ==> "Release"

"Push"
  ==> "Release"

RunTargetOrDefault "All"
