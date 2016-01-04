
<img src="Docs/LogoWhiteWithText.png?raw=true" alt="Projeny" width="250px" height="246px"/>
## Project and Package Manager for Unity3D

### <a id="introduction"></a>Introduction ###

The purpose of Projeny is to allow your Unity3D project to easily scale in size without heavily impacting development time.

Projeny allows you to:

* Share any unity assets (code, scenes, prefabs, etc.) across multiple different unity projects without copy and pasting
* Instantly switch between platforms
* Easily upgrade or downgrade installed asset store packages
* Optimize compile time of your project by getting Unity to only recompile the code that changes most often
* Split up your project into discrete packages, so that you can manage the dependencies between each, instead of having one giant unity project of inter-related files
* Declare dependencies between packages, so that you always get the packages that you need without needing to hunt down missing libraries or broken links
* Generate a more intelligent Visual Studio solution than the Unity default, using package dependencies to create csproj dependencies

See below for details on how Projeny achieves all these features.

This project is open source.  If you're interested in helping, great!  There's still a number of features we'd like to support eventually (in particular we need help with supporting OSX!).  See <a href="#feature-ideas">this section</a> for the full list.

NOTE: Projeny requires Unity3D 5.3.1 or higher, since it makes use of the `-buildTarget` command line switch is only fixed in 5.3.1

## Table Of Contents

* <a href="#installation">Installation</a>
* <a href="#introduction">Introduction</a>
* Theory
    * <a href="#overview">Overview</a>
    * <a href="#shared-files">Shared files between projects</a>
    * <a href="#organization">Organization and re-usability</a>
    * <a href="#platform-switching">Near instant platform switching</a>
    * <a href="#compilation-time-optimization">Compile time optimization</a>
    * <a href="#platform-specific-folders">Platform specific package folders</a>
    * <a href="#dependency-management">Dependency Management of Packages</a>
    * <a href="#visual-studio-generation">Intelligent Visual Studio Solution Generation</a>
* Usage
    * <a href="#common-workflows">Common Workflows</a>
    * <a href="#command-line-reference">Command Line Reference</a>
    * <a href="#gotchas">Gotchas / Miscellaneous Tips and Tricks</a>
    * <a href="#projectini">Project.ini reference</a>
    * <a href="#packageini">Package.ini reference</a>
    * <a href="#visual-studio-generation-usage">Visual Studio Solution Generation</a>
* <a href="#appendix">Appendix</a>
* <a href="#release-notes">Release Notes</a>
* <a href="#license">License</a>

## <a id="installation"></a>Installation

You can either run Projeny directly from source (requires python) or simply download the latest binary.  Note that Projeny is currently only supported on Windows.

- From Binary

    - Go to the [releases](https://github.com/modesttree/projeny/releases) section and download the latest ProjenyInstaller.exe
    - Note that after installation completes, you will need to add the [Install Directory]/Bin directory to your windows path.
    - Follow the rest of the instructions below

- From Source

    - Clone this repo to a place on your hard drive
    - Make sure python 3.x is installed
    - Open UnityPlugin/Projeny.sln in Visual Studio
    - Build in Release Mode

## <a id="overview"></a>Overview

Projeny works by composing your Unity3D projects based on directory links (aka windows junctions aka symbolic links).

This is best shown with an example.  After 

downloading the project and extracting/cloning it to your hard drive, you should see the following directory structure:

* UnityPackages
    * AllMovers
    * CommonShapeMover
    * CubeMover
    * SphereMover
    * Projeny
* UnityProjects
    * AllMovers
        * ProjectSettings
    * CubeMover
        * ProjectSettings
    * SphereMover
        * ProjectSettings
* Projeny

Each folder in the UnityProjects directory represents an actual Unity3D project.  Note that they each have the familiar ProjectSettings directory but they do not yet have an Assets directory.  This is because these projects have not been initialized yet.

Projeny is driven by the command line.  To initialize one of these projects we must run Upm.bat (this stands for Unity Package Manager) which can be found at the root directory.  In order to run it, you need to open a terminal such as Command Prompt or Powershell, so that you can supply arguments to it.

To initialize one of our projects, execute `upm --project CubeMover --updateLinks` (or the shortened form `upm -p CubeMover -ul`)

Now if we look at our CubeMover project again, we see that some new folders have been added

* UnityProjects
    * CubeMover
        * ProjectSettings
        * CubeMover-Windows
            * Assets
                * CubeMover
                * Plugins
                    * CommonShapeMover
                    * Projeny
            * ProjectSettings

The CubeMover-Windows folder is new, and now has the familiar Assets directory.  It also contains a bunch of folders within it such as CubeMover, CommonShapeMover, etc.

To run our project, open the CubeMover-Windows folder in Unity and then open the scene at CubeMover/CubeMain.  After running it you should see the cube move around while changing colors.

So where did all these files come from?  The answer is directory links (aka windows junctions aka symbolic links).  All of these new folders are not really folders in themselves but instead they are simply links to existing folders somewhere else.

In fact, if you're using source control, the entire CubeMover-Windows directory should be excluded from it (by using a .gitignore file, a .svnignore, .p4ignore, or whatever ignore method applies to the source control you're using).  See the sample projects for an example .gitignore file.

We do this because the contents of the CubeMover-Windows folder does not itself contain any real content.  It simply contains temporary files generated by unity (such as the Library folder) and directory links to other directories that actually contain the content.

The directories containing the actual content can be found in the UnityPackages directory.  This is where you will find the CubeMover, CommonShapeMover, and Projeny folders that we see linked to underneath the assets directory.

But how does Projeny know which packages to use for the CubeMover project?  For this it reads from a configuration file which can be found at CubeMover/project.ini.  If you open this file it should read as follows:

```
[Config]
    packages:
        CubeMover
        CommonShapeMover
```

Here we see the list of packages to include for this project underneath the "packages" setting.  This file is all that's required to create a new project with Projeny.  You simply need to add a new directory underneath the UnityProjects directory with the name of your new project, then create a project.ini file that contains the list of packages that you wish to include in this project  (also, if you're using source control, don't forget to include the ignore file to avoid submitting the generated folders)

## <a id="advantages"></a>Advantages of Using Projeny

#### <a id="shared-files"></a>1 - Shared files between projects

By using directory links, you can have multiple unity projects all using the same package folders, without needing to copy and paste each package per project.  You can change a file such as a prefab or a C# file, and that change will apply to all other projects that are using it as well

Previously, the best way to share code between different unity projects was to put the code into a DLL and then output that DLL to all the Unity Projects that you want to use it in.  This works ok but has a number of <a href="#dllgotchas">gotchas</a> that make this approach difficult to do correctly.

Want to just test one part of your game without needing to fire up the entire project?  Just create another unity project and reference only the parts of the game you want to test.

To see this in action, do the following:
* Execute `upm --project SphereMover --updateLinks` (or the shortened form `upm -p SphereMover -ul`)
* Open unity then open the project at UnityProjects/SphereMover/SphereMover-Windows
* Open the scene named SphereMain
* Run this - you should see the sphere move around and change colors.
* The script used here to move both the Sphere here and the Cube in the CubeMover project is the same, and can be found in the shared package CommonShapeMover.

Or as another (more interesting) example:
* Execute `upm --project AllMovers --updateLinks` (or the shortened form `upm -p AllMovers -ul`)
* Open up the unity project at UnityProjects/AllMovers/AllMovers-Windows
* Run the scene AllMovers
* You should see both the cube and sphere in the same scene.
* This is achieved by sharing the prefab for both the cube and the sphere across all these projects.

#### <a id="organization"></a>2 - Organization and re-usability

You can more easily manage many different unity packages that you've created yourself, but also those packages that you've installed through the asset store.  You can build up a big collection of packages that you've purchased through the asset store and placed in your UnityPackages directory, and then easily include or exclude those in your Unity projects by simply selecting or not selecting them in the project.ini file for each project.

#### <a id="platform-switching"></a>3 - Near instant platform switching

You might also have noticed above that there is multiple ProjectSettings folders.  One underneath CubeMover and another underneath CubeMover-Windows.

The reason for this is to allow Projeny to create separate unity project directories for each platform, so that we can jump instantly from one platform to another without needing to wait for unity to process all the assets.

In the example above, we have only initialized the windows unity project "CubeMover-Windows" but we can also create "CubeMover-iOS", "CubeMover-Android", etc. by opening these platforms for this project.

When that occurs, Projeny will create a directory link to the main ProjectSettings directory so that we can have all these different platform-specific projects use the same unity project settings.

To see this in action, open up the CubeMover project in Unity then select the menu item `Projeny -> Change Platform -> iOS`.  The first time you do this will take longer since Unity has to process the files but any subsequent times should be nearly instant, since it should be no different from simply opening another project.

#### <a id="compilation-time-optimization"></a>4 - Compile time optimization

Unity compiles your project in multiple passes.  The first pass compiles all C# files that are in the Plugins/ directory and the second pass covers all other C# files.  If Unity does not detect any changes in the Plugins/ directory then Unity will skip this first pass and only execute the second pass.  (Note: This is a bit of a simplification - see [here](http://docs.unity3d.com/Manual/ScriptCompileOrderFolders.html) for full details)

We can take advantage of this by always putting all stable packages (such as those obtained through asset store) into the Plugins folder and putting the packages that we change frequently directly underneath the Assets folder.

And because we are using directory links, and the entire CubeMover-Windows folder is ignored by source control, changing the location of these directory links is trivial.  You can modify this directory structure multiple times per day based on what you're working on, by simply modifying a few lines in your project.ini file.

For example, if you change the CubeMover/project.ini file from this:

```
[Config]
    packages:
        CubeMover
        CommonShapeMover
```

To this:

```
[Config]
    packages:
        CubeMover
    pluginPackages:
        CommonShapeMover
```

Then the CommonShapeMover package will be placed in the Plugins directory.  Any changes we make to source files within the CubeMover directory will be compiled more quickly since it will not need to re-compile the source files inside CommonShapeMover

#### <a id="platform-specific-folders"></a>5 - Platform specific package folders

Unity 5 adds some helpful features, including the ability to enable/disable a DLL based on platform.  If you add a DLL to your project then click on it you get a bunch of checkboxes in the inspector that allow you to choose which platform this DLL is for.   Projeny allows you to do something similar except for all assets and directories.

This is possible because when Projeny generates your unity project, it can choose a different set of package folders to include based on the current platform, since each platform has its own unity project generated for it.

One example use for this is if you had a large Resources folder that contained a lot of data for a specific platform.  Since unity always includes the contents of the Resources folders for builds, this would cause your platform specific files inside Resources to be included on other platforms for no reason.  Using Projeny, you can simply move the Resources folder to a platform specific package to address this problem

This can also make things easier when it comes to code.  You can have entire package folders be platform specific, then you no longer need to add #ifdef's around entire files to avoid the compile errors on other platforms.

#### <a id="dependency-management"></a>6 - Dependency Management of Packages

When adding a package to the UnityPackages directory, you can also specify the packages that this package depends on.  Then, when Projeny is generating your unity project directory, it can automatically figure out which packages you need.

This is very powerful, because often when working on a new project you want to just be able to ask for package X, and you don't want to have to think about what other packages this package requires.

For example, if you look at the project.ini for the included sample project AllMovers, it reads as follows:

```
[Config]
    packages:
        AllMovers
```

But if you look at the generated directory structure of the unity project you see the following:

* AllMovers
* CubeMover
* Plugins
    * CommonShapeMover
    * Projeny
* SphereMover

How does Projeny know to add all these extra directory links?  It knows this because when it goes to add the AllMovers package, it inspects the package configuration file for it, which you can find at UnityPackages/AllMovers/package.ini and reads as follows:

```
[Config]
    Dependencies:
        CubeMover
        SphereMover
```

This tells Projeny that whenever the AllMovers package is added to a project, the SphereMover and CubeMover packages should be added as well.  This process repeats recursively for the SphereMover and CubeMover packages.  In this case, it find that it also needs to add the CommonShapeMover package, which both SphereMover and CubeMover depend on.

#### <a id="visual-studio-generation"></a>7 - More intelligent Visual Studio Solution generation

Projeny can also take advantage of the dependency information between packages to generate a better Visual Studio project.  In the sample project, Projeny can generate a custom solution file that contains a C# project for each package in the project:

* AllMovers
* CubeMover
* SphereMover
* CommonShapeMover

In this case, the AllMovers C# project will automatically be set to depend on the CubeMover, SphereMover, and CommonShapeMover projects.  The CubeMover C# project will depend on the CommonShapeMover project. Etc.  Because that is what the dependencies are as specified in each package.ini

This can be helpful for code organization but more importantly, this allows you to design dependencies on a module level.  In normal unity projects, every code file could potentially make use of any other code file in your entire project.  For small projects this is not an issue, however, as your project scales in size it is helpful to be able to design code at a module level and avoid having your project devolve into a [Big ball of mud](https://en.wikipedia.org/wiki/Big_ball_of_mud).

For example, it is common to build up a library of re-usable utility functions that you can use in multiple unity projects, such as a math library.  For these cases, it would be important to avoid using game-specific code from within your math library, because then your math library is strongly coupled to your game and can't be used in other projects.  If you compile using the Projeny generated solution file, this would not be a problem, since it would guarantee these dependencies remain intact, even though Unity itself would allow them.

For usage details on this feature see <a href="#visual-studio-generation-usage">this section</a>

## <a id="common-workflows"></a>Common Workflows

#### <a id="workflow-create-package"></a>1. Create a new package
* Go to the UnityPackages directory
* Create a new folder with the name of your package
* Done. You can now refer to this package by its folder name
* (optional) Add a package.ini file to your new folder
    * This can be used to declare some dependencies for this package, or flag it for use with only specific platforms, etc.
    * See <a href="#packageini">here</a> for the full package.ini reference

#### <a id="workflow-create-project"></a>2. Create a new project
* Go to the UnityProjects directory
* Create a new directory with the name of your project
* Create a project.ini file in your new directory
    * Add the names of the packages you want to include inside the project.ini (see <a href="#projectini">here</a> for full project.ini reference)
* Add a .gitignore or a .svnignore or a .p4ignore or whatever file applies to the source control that you're using to your project directory
    * This is important because the actual unity projects themselves should always be fully ignored by source control
    * See the sample projects for an example of a .gitignore to use

#### <a id="workflow-add-package"></a>3. Add a package to an existing project
* Open up the project.ini file for your project
* Add a new line underneath "packages" or "pluginPackages" with the name of the package you want to add (see <a href="#projectini">here</a> for full project.ini reference)
* <a href="#workflow-process-projectini">Update the directory links</a>

#### <a id="workflow-remove-package"></a>4. Remove a package from an existing project
* Open up the project.ini file for your project
* Delete the line with the package you want to remove
* <a href="#workflow-process-projectini">Update the directory links</a>

#### <a id="workflow-update-custom-solution"></a>4. The visual studio solution has become out of sync with the file system.
* This can occur when adding/removing C# files within Unity, when adding/removing C# files directly from the file system, when adding/removing packages to your project.ini, etc.
* From within your Unity project, select the menu item `Projeny -> Custom Solution -> Update`
* Note that you can also do this from the command line with `upm -p YourProject -ucs -uus` (see <a href="#commandline-updateCustomSolution">here</a> for details on these command line flags)

#### <a id="workflow-optimize-compile-time"></a>5. Speeding up the compile time of your project
* Open up the project.ini file for your project
* Move the packages that you are currently working on to be underneath the `packages` category
* Move all other package to be underneath the `packagePlugins` category
* <a href="#workflow-process-projectini">Update the directory links</a>

#### <a id="workflow-process-projectini"></a>6. You've made changes to your project.ini
* From within Unity, execute the menu item `Projeny -> Update Links`
* Note: You can also do this from the command line with `upm -p YourProject -ul` (see <a href="#commandline-updateLinks">here</a> for details on these command line flags)
* (optional) <a href="#workflow-update-custom-solution">Update your custom solution</a> to show the new files

## <a id="command-line-reference"></a>Command Line Reference

What follows is the full list of command line parameters that you can pass to Upm.bat.  Note that you can pass any combination of these and Upm.bat will execute them in a reasonable order.

* #### <a id="commandline-openDocumentation"></a>`--openDocumentation` / `-d`
    * Opens up the documentation page that you are reading

* #### <a id="commandline-project"></a>`--project` / `-p`
    * Selects the project to use for whatever other parameters are given
    * For example, if you run `upm -p AllMovers -ul` this will update all the directory links for the AllMovers project (using the default platform which is windows).
    * Valid values are the names of the directories underneath the UnityProjects directory. 
        * You can view the full list of projects by running the <a href="#commandline-listProjects">`-lp` command</a>
        * You can also pass in one of the project aliases defined in your ProjenyConfig.xml file, to avoid typing the full name every time

* #### <a id="commandline-platform"></a>`--platform` / `-pl`
    * Selects the platform to use for whatever other parameters are given
    * Note that if this parameter is not supplied Projeny will assume windows
    * Valid values are the following:
        * `win` - Windows
        * `webp` - Webplayer
        * `webgl` - WebGL
        * `and` - Android
        * `osx` - Mac
        * `ios` - iOS for use with iPhone or iPad
        * `lin` - Linux
    * For example, if you run `upm -p AllMovers -pl ios -ul` this will update all the directory links within the AllMovers-iOS directory.

* #### <a id="commandline-updateLinks"></a>`--updateLinks` / `-ul`
    * Updates all the directory links for the given project and platform.
    * Projeny will read the project.ini file associated with the given project, then calculate all the packages that it needs to include.  For each package, it will then create a directory link (aka windows junction aka symbolic link) inside either the Assets/ directory or the Assets/Plugins directory
    * Note that in order to run this command you must <a href="#commandline-project">specify a project</a> (or set a default project in ProjenyConfig.xml) and also optionally <a href="#commandline-platform">set a platform</a> (otherwise it will assume windows)

* #### <a id="commandline-listProjects"></a>`--listProjects` / `-lp`
    * Lists the names of all the directories that are underneath the UnityProjects directory, along with the alias for each if one is defined.

* #### <a id="commandline-updateCustomSolution"></a>`--updateCustomSolution` / `-ucs`
    * Generates .csproj files and a .sln file based on the configuration set in the project.ini for the given project and given platform
    * See <a href="#visual-studio-generation">here</a> for more details on this feature.
    * Note that in some cases you will want to run <a href="#commandline-updateUnitySolution">`-uus`</a> at the same time or before executing this command.  This is not necessary all the time but is necessary whenever you add DLL's to your project, add/remove a define in player settings, etc.  Also, if `-uus` has not been run at least once this command will fail
    * Note that in order to run this command you must <a href="#commandline-project">specify a project</a> (or set a default project in ProjenyConfig.xml) and also optionally <a href="#commandline-platform">set a platform</a> (otherwise it will assume windows)

* #### <a id="commandline-updateUnitySolution"></a>`--updateUnitySolution` / `-uus`
    * Runs Unity.exe to generate the standard MonoDevelop solution, so that it can be used by the <a href="#commandline-updateCustomSolution">`-ucs` command</a>.  This is equivalent to opening unity and running the menu item `Assets -> Open C# Project` (except without actually opening visual studio/monodevelop)

* #### <a id="commandline-verbose"></a>`--verbose` / `-v` and <a id="commandline-veryVerbose"></a>`--veryVerbose` / `-vv`
    * These parameters can control how verbose the logging output is to the console.  `-v` will output some extra detail in places and `-vv` will output absolutely everything (including the contents of the unity editor log, visual studio, etc.)

* #### <a id="commandline-buildCustomSolution"></a>`--buildCustomSolution` / `-b`
    * Builds the custom solution
    * This command will fail if the custom solution has not been generated yet using the <a href="#updateCustomSolution">`-ucs` command</a>
    * Note that in order to run this command you must <a href="#commandline-project">specify a project</a> (or set a default project in ProjenyConfig.xml) and also optionally <a href="#commandline-platform">set a platform</a> (otherwise it will assume windows)

* #### <a id="commandline-buildFull"></a>`--buildFull` / `-bf`
    * This command is equivalent to the following: `upm -ul -uus -ucs -b`
        * In other words, it will update the links for the given project/platform, update the custom solution, then build the custom solution
    * Note that in order to run this command you must <a href="#commandline-project">specify a project</a> (or set a default project in ProjenyConfig.xml) and also optionally <a href="#commandline-platform">set a platform</a> (otherwise it will assume windows)

* #### <a id="commandline-openUnity"></a>`--openUnity` / `-ou`
    * Opens unity for the given project/platform
    * Note that in order to run this command you must <a href="#commandline-project">specify a project</a> (or set a default project in ProjenyConfig.xml) and also optionally <a href="#commandline-platform">set a platform</a> (otherwise it will assume windows)

* #### <a id="commandline-openCustomSolution"></a>`--openCustomSolution` / `-ocs`
    * Opens the custom solution for the given project/platform using visual studio
    * Note that this command will require that you set your `VisualStudioIdePath` in ProjenyConfig.xml
    * This command will also fail if the custom solution has not been generated yet using the <a href="#updateCustomSolution">`-ucs` command</a>
    * Note that in order to run this command you must <a href="#commandline-project">specify a project</a> (or set a default project in ProjenyConfig.xml) and also optionally <a href="#commandline-platform">set a platform</a> (otherwise it will assume windows)

* #### <a id="commandline-clearProjectGeneratedFiles"></a>`--clearProjectGeneratedFiles` / `-clp`
    * Deletes all the generates files/directories for the given project.   Note that this will not delete any real content, it will only remove some directory links and some temporary files generated by unity.
    * This command is sometimes useful if you want to do a full reset of your project, which you can do by running `upm -clp -bf`.  This will delete all generated files and then re-generate them again.
    * Note that in order to run this command you must <a href="#commandline-project">specify a project</a> (or set a default project in ProjenyConfig.xml)

* #### <a id="commandline-clearAllProjectGeneratedFiles"></a>`--clearAllProjectGeneratedFiles` / `-cla`
    * This is similar to the `-clp` command except this will be executed for every project in your UnityProjects directory

* #### <a id="commandline-deleteAllLinks"></a>`--deleteAllLinks` / `-dal`
    * Removes all directory links in all projects.  This is the reverse of the <a href="#commandline-initAll">`-ina` command</a>

* #### <a id="commandline-initAll"></a>`--initAll` / `-ina`
    * This is equivalent to running the <a href="#commandline-updateLinks">`-ul` command</a> on all the projects that are underneath the UnityProjects directory

## <a id="gotchas"></a>Gotchas / Miscellaneous Tips and Tricks

* After opening your project for the first time (or when adding new packages) Unity will show the following warning:

```
[Asset] is a symbolic link. Using symlinks in Unity projects may cause your project to become corrupted if you create multiple references to the same asset, use recursive symlinks or use symlinks to share assets between projects used with different versions of Unity. Make sure you know what you are doing.
```

However, we are not doing any of the things that Unity warns about here so this warning can be ignored.

## <a id="projectini"></a>Project.ini reference

The format of the project.ini is as follows:

    [Config]
        packages:
            {PackageName}
            {PackageName}
        packagesPlugins:
            {PackageName}
            {PackageName}
            {PackageName}
        solutionProjects:
            {PackageName}
            /{PackageNamePattern}
            /{PackageNamePattern}
            {PackageName}

    [ProjectFolders]
        {FolderName}: /{PackageNamePattern}
        {FolderName}: /{PackageNamePattern}

* The `{PackageName}` items above would be replaced with names of directories that are below your UnityPackages directory.
* Packages that are listed underneath the `packages` category will be placed directly underneath the Assets/ directory of your project
* Packages that are listed underneath the `packagesPlugins` category will be placed directly underneath the Assets/Plugins directory of your project
* All packages underneath the `solutionProjects` category will have their own .csproj file generated, when running `Project -> Custom Solution -> Update` from within unity or executing the <a href="##commandline-updateCustomSolution">`-ucs` command line option</a>
    * Note that in this case you can also use a regular expression instead of explitly listing every package you want a project for
    * It is common to want a csproj file generated for every package in your project, in which case you can add the line `/.*` which will match everything
* You can also optionally add folders to the generated solution, to organize related projects together.  Each folder has one regex pattern that is used to filter the full list of projects.
* Note that the regex used here following the regex rules defined for python (more details <a href="https://docs.python.org/2/library/re.html">here</a>)

## <a id="packageini"></a>Package.ini reference

In most cases your package.ini will simply list the other packages that this package depends on.  It will look like this:

[Config]
    Dependencies:
        MyPackageA
        MyPackageB
        MyPackageC

Also note that the package.ini file is optional.  If not supplied, Projeny will assume your project has zero dependencies.

However, there is a few other options here for less common cases.  The full format of the package.ini is as follows:

    [Config]
        Dependencies:
            {PackageName}
            {PackageName}
            {PackageName}
        Extras:
            {PackageName}
            {PackageName}
        FolderType: {FolderType}
        Platforms:
            {PlatformName}
            {PlatformName}
            {PlatformName}
        ForcePluginsDirectory: {True/False}
        ForceAssetsDirectory: {True/False}

* The `{PackageName}` items above would be replaced with actual names of directories that are below your UnityPackages directory.
* Any packages that are listed under `Dependencies` or `Extras` will always be added to every project that includes this package
* The only difference between Dependencies and Extras is that Projeny will create a csproj dependency for packages under Dependencies whereas it will not for those packages under Extras.  Most of the time you will want to only add to Dependencies, however in some cases it can be useful to use Extras.  For example, if I have split out a bunch of unit tests for my package into its own separate package, and I want to always include those with my package, I would add them to the Extras list.  I would not add them underDependencies because this would create a circular dependency and Projeny will display an error.
* By default, Projeny will assume that your package is applicable to all platforms.  If this list is set however, Projeny will skip this package for all platforms except those listed, and this directory will not show up at all in the Unity Projects for those platforms.
* When ForcePluginsDirectory is set, this will require that the package always be placed at Assets/Plugins/PackageName.
    * This exists because some packages have hard-coded paths that require that the package be at a specific location
* ForceAssetsDirectory behaves similarly, and will ensure the package will always be placed at Assets/PackageName.
* FolderType can be set to any of the following:
    * streamingassets
        * Package will be placed at Assets/StreamingAssets/YourPackageName
        * This has special meaning to unity - see <a href="http://docs.unity3d.com/Manual/SpecialFolders.html">here</a> for details
    * webgl
        * Package will be placed at Assets/Plugins/WebGL
        * This has special meaning to unity - see <a href="http://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html">here</a> for details
    * androidproject
        * Package will be placed at Assets/Plugins/Android
        * This has special meaning to unity - see <a href="http://wiki.unity3d.com/index.php/Special_Folder_Names_in_your_Assets_Folder">here</a> for details
    * androidlibraries
        * Package will be placed at Assets/Plugins/Android/libs
        * This has special meaning to unity - see <a href="http://docs.unity3d.com/Manual/PluginsForAndroid.html">here</a> for details
    * ios
        * Package will be placed at Assets/Plugins/ios
        * This has special meaning to unity - see <a href="http://docs.unity3d.com/Manual/PluginsForIOS.html">here</a> for details

## <a id="visual-studio-generation-usage"></a>Visual Studio Solution Generation

When Unity normally generates the MonoDevelop/Visual Studio C# solution, it creates the following projects

* `Assembly-CSharp`
* `Assembly-CSharp-firstpass`

Each of these projects corresponds to a compilation phase:

* `Assembly-CSharp-firstpass` will contain any C# files that are below the Plugins/ folder
* `Assembly-CSharp` will contain all other C# files

This makes perfect sense for normal Unity projects as a way to organize the code, however, for Projeny based Unity projects, we have more information that we can use when generating the solution file.

Note that you don't have to use the Projeny generated solution file - you can instead simply use Project for the other features mentioned above.  But the feature is there if you want to use it.

To generate the projeny solution, open your unity project and select the menu item `Projeny -> Custom Solution -> Update`.  Note that you can also <a href="#update-custom-solution-from-command-line">update the projeny solution from the command line</a>.

After this command completes successfully you should see a new file named `AllMovers-Windows.sln` generated underneath the AllMovers directory.  If you open this in Visual Studio you should see the following projects:

* Packages
* Packages-plugins

As you can see, by default Projeny generates a very similar solution file to the one that Unity itself generates.  This is because Projeny requires that you specify in the project.ini how you want the solution to be generated.  For example, if you change AllMovers/project.ini to this:

```
[Config]
    packages:
        AllMovers
    packagesPlugins:
        CubeMover
        SphereMover
    solutionProjects:
        AllMovers
        CubeMover
        SphereMover
```

And then run `upm --project AllMovers --updateCustomSolution` (or the shortened form `upm -p AllMovers -ucs`), then open Visual Studio again, you should see the following:

* AllMovers
* CubeMover
* SphereMover
* Packages-Plugins

As you can see, the `solutionProjects` setting in the project.ini can be used to choose which packages you want to have a corresponding Visual Studio project.  Also, the visual studio project dependencies are set up according to the project dependencies that are defined in the package.ini file for each package.  So in this case the AllMovers C# project will depend on CubeMover and SphereMover.

You might notice in the above example that the Packages project has disappeared while the Packages-Plugins project remains.  If after creating the custom packages, Projeny does not find any code files left over to place inside the catch-all Packages project, then it doesn't bother creating it.  And since we didn't add the CommonShapeMover package to the list of solutionProjects, it was placed in the Packages-Plugins project, which is why that project still exists.

In many cases you might want to just create a project for every package in your project.  You can do that by using the following project.ini:

```
[Config]
    packages:
        AllMovers
    packagesPlugins:
        CubeMover
        SphereMover
    solutionProjects:
        /.*
```

Projeny will interpret a project that starts with a forward slash to be a pattern instead.  And since this pattern will match all package names, all packages will have a corresponding project.  So if we use this project.ini and run `upm -p AllMovers -ucs` again, we should see these projects in our solution:

* AllMovers
* CubeMover
* SphereMover
* CommonShapeMover

A couple of notes:
- The solution file and all csproj files are fully generated, and therefore you should not change any settings on them, such as preprocessor defines, output paths, etc.  Any change that you make here will be over-written next time you run `upm -ucs`.  Also, if you're using source control, the csproj files and solution file should be ignored since the entire unity Assets directory is ignored.
- The DLL's generated by this custom solution are not used at all, just like the DLL's generated by the normal Unity-generated solution are not used at all.  Unity still has to recompile all the code itself, even if we've already compiled it using the custom solution.
- The generated solution file is platform specific, since each platform contains different preprocessor defines

## <a id="appendix"></a>Appendix

### <a id="dllgotchas"></a>A. "Gotchas" with using external assemblies:

* You cannot really use unity preprocessor defines inside your DLL (eg: `UNITY_WEBPLAYER`, `UNITY_5_3`, etc.) unless you create separate DLL's for each platform. This is helped somewhat by Unity 5.3, which allows you to indicate which platforms to use a given DLL in.
* There are some known limitations of using external assemblies for code - in particular, any MonoBehaviour with a custom base class will not be allowed to be added to a game object.
* Unity is not aware of the location of each MonoBehaviour.  You cannot for example, double click on a MonoBehaviour to open up the source file for it.

### <a id="update-custom-solution-from-command-line"></a>Update custom visual studio solution from the command line

To update the projeny solution for your project, run `upm --project YourProject --updateUnitySolution --updateCustomSolution` (or the shortened form `upm -p YourProject -uus -ucs`)

The `updateUnitySolution` option will instruct Projeny to run Unity to general the MonoDevelop solution file.  This is necessary because Projeny uses some project config settings (the preprocessor defines and library references) from this generated solution file when generating its own custom visual studio solution.

The `updateCustomSolution` option will instruct Projeny to create this solution file.  This will always occur after generating the unity solution file.

NOTE: In order for this command to work, Projeny must be able to find the path to Unity.exe, so that it can run unity with the given project and then generate the unity solution file.  By default, Projeny will look at the default install directory of Unity but if you installed it somewhere else you will have to specify it in the ProjenyConfig.xml file in the root directory (search for 'UnityExePath')

## <a id="feature-ideas"></a>Feature Ideas

* OSX Support
* GUI interface to editor the project.ini directly from within Unity. Allow adding/removing packages or selecting from the full list.
* Ability to fetch packages from online repositories, similar to how the node.js NPM utility works.  Could allow entering full URL's and otherwise fallback to using github.com/[PacakgeName]
* Other NPM-like features

Feel free to submit any other ideas as github issues

## <a id="release-notes"></a>Release Notes

0.1 (December, 2015)
- Initial release

## <a id="license"></a>License

    The MIT License (MIT)

    Copyright (c) 2010-2015 Modest Tree Media  http://www.modesttree.com

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
