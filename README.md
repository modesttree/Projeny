
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

This project is open source.  If you're interested in helping, great!  There's still a number of features we'd like to support eventually (in particular we need help with supporting OSX!).

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
    * <a href="#projectini">ProjenyProject.yaml reference</a>
    * <a href="#package-yaml">Package.ini reference</a>
    * <a href="#visual-studio-generation-usage">Visual Studio Solution Generation</a>
* <a href="#appendix">Appendix</a>
* <a href="#release-notes">Release Notes</a>
* <a href="#license">License</a>

## <a id="installation"></a>Installation

You can either run Projeny directly from source (requires python) or simply download the latest binary.  Note that Projeny is currently only supported on Windows.

- From Binary

    - Go to the [releases](https://github.com/modesttree/projeny/releases) section and download the latest ProjenyInstaller.exe
    - Note that after installation completes, you will need to add the [Install Directory]/Bin directory to your windows path.

- From Source

    - Clone this repo to a place on your hard drive
    - Make sure python 3.x is installed
    - Open UnityPlugin/Projeny.sln in Visual Studio
    - Build in Release Mode
    - Add the projeny/Source/bin to your windows path.  NOTE:  This is projeny/Source/bin NOT projeny/bin (the latter is used for exe builds)

## <a id="overview"></a>Overview

Projeny works by composing your Unity3D projects based on 'directory links' (aka windows junctions aka symbolic links).

This is best shown with an example.  After installing Projeny, [download the sample project](https://github.com/modesttree/projeny/releases) from the releases page and extract it to a new folder on your hard drive.  The folder structure should appear like this:

* Projeny.yaml
* UnityPackages
    * AllMovers
    * CommonShapeMover
    * CubeMover
    * SphereMover
* UnityProjects
    * AllMovers
        * ProjectSettings
    * CubeMover
        * ProjectSettings
    * SphereMover
        * ProjectSettings

Each folder in the UnityProjects directory represents an actual Unity3D project.  Note that they each have the familiar ProjectSettings directory but they do not yet have an Assets directory.  This is because these projects have not been initialized yet by Projeny.  You'll also notice a file named Projeny.yaml at the root of the folder structure.  This is a simple text file that is used to specify Projeny configuration settings.

To initialize these unity projects we must run projeny from the command line.  Open up command prompt or powershell and navigate to the root directory (the same directory where you will find Projeny.yaml).  Then execute `prj --init`.  If the prj command is not found, check that it has been added to your windows PATH variable, as mentioned in the <a href="#installation">install instructions</a>.  All this command does is initialize some of the the directory links for these projects.

If we look at our project folders again, we see that a bunch of new folders appear to have been added.  Let's look at the CubeMover project in particular:

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

To actually run our project, open the CubeMover-Windows folder in Unity and then open the scene at CubeMover/CubeMain.  After running it you should see the cube move around while changing colors.

So where did all these files come from?  The answer is 'directory links' (aka windows junctions aka symbolic links).  All of these new folders are not really folders in themselves but instead they are simply links to existing folders somewhere else.

In fact, if you're using source control, the entire CubeMover-Windows directory should be excluded from it (by using a .gitignore file, a .svnignore, .p4ignore, or whatever ignore method applies to the source control you're using).  If you're using subversion or git, this will happen automatically when you initialize your project (otherwise, you will have to make sure to add the ignore files yourself).

We do this because the contents of the CubeMover-Windows folder does not itself contain any real content.  It simply contains temporary files generated by Unity (such as the Library folder) and directory links to other directories that actually do contain content.

The directories containing the actual content can be found in the UnityPackages directory.  This is where you will find the CubeMover and CommonShapeMover folders that we see linked to underneath the assets directory.

But how does Projeny know which packages to use for the CubeMover project?  For this it reads from a configuration text file, which can be found at CubeMover/ProjenyProject.yaml.  If you open this file it should read as follows:

```
AssetsFolder:
    - CubeMover
    - CommonShapeMover
```

Here we see the list of packages to include for this project underneath the setting "AssetsFolder".

Most of the time however, you will not need to edit this file directly.  Instead, you can use projeny's built-in unity plugin to manipulate this file.  You can try this by opening the CubeMover project in Unity, then clicking the menu item `Projeny -> Package Manager...`.  This window will be be explained in more detail in the following sections.

## <a id="advantages"></a>Advantages of Using Projeny

### <a id="shared-files"></a>1 - Shared files between projects

By using directory links, you can have multiple unity projects all using the same package folders, without needing to copy and paste each package per project.  You can change a file such as a prefab or a C# file, and that change will be applied to all other projects that are using it as well

Previously, the best way to share code between different unity projects was to put the code into a DLL and then output that DLL to all the Unity Projects that you want to use it in.  This works ok but has a number of <a href="#dllgotchas">gotchas</a> that make this approach difficult to do correctly.

Want to just test one part of your game without needing to fire up the entire project?  Just create another unity project and reference only the parts of the game you want to test.

To see this in action, do the following:
* Open unity then open the project at UnityProjects/SphereMover/SphereMover-Windows
* Open another copy of unity then open the project at UnityProjects/CubeMover/CubeMover-Windows
* Open the scene named SphereMain in the first unity and the scene CubeMain in the second unity
* If you run them you should see the shapes move and change colors
* The script used here to move both the Sphere here and the Cube in the CubeMover project is the same, and can be found in the shared package CommonShapeMover.
* Then, as a further example, open up the unity project at UnityProjects/AllMovers/AllMovers-Windows
* Run the scene AllMovers
* You should see both the cube and sphere in the same scene
* This works because these three projects all have some shared packages.  Note that this allows sharing code as well prefabs (ie. the cube and the sphere prefabs), scenes, etc.

### <a id="organization"></a>2 - Package Organization and Asset Store integration

Projeny allows you to much more easily manage many different unity packages that you've created yourself, but also those packages that you've installed through the asset store. 

You can build up a big collection of packages that you've purchased through the asset store and added to your UnityPackages directory, and then easily include or exclude those in your purchased assets by simply selecting or not selecting them in each project.  Projeny can also be used to easily upgrade/downgrade installed asset store packages all through a simple GUI interface.  See <a href="#managing-assetstore-assets">this section</a> for more details on managing asset store packages through projeny.

### <a id="platform-switching"></a>3 - Near instant platform switching

You might be wondering why the projects that you've been dealing with are all marked with the suffix '-Windows'.  Or why there are multiple ProjectSettings folders that appear after you run the --init command as described above (One underneath CubeMover and another underneath CubeMover-Windows)

The reason for this is to allow Projeny to create entirely separate unity project directories for each platform.  This allows us to jump instantly from one platform to another without needing to wait for unity to process all the assets.

In the examples above, we have only initialized the windows unity project "CubeMover-Windows" but we can also create "CubeMover-iOS", "CubeMover-Android", etc. by opening these platforms for this project.

When that occurs, Projeny will create a directory link to the main ProjectSettings directory so that we can have all these different platform-specific projects use the same unity project settings.

To see this in action, open up the CubeMover project in Unity then select the menu item `Projeny -> Change Platform -> iOS`.  The first time you do this will take longer since Unity has to process the files but any subsequent times should be nearly instant, since it should be no different from simply opening another project.

### <a id="compilation-time-optimization"></a>4 - Compile time optimization

Unity compiles your project in multiple passes.  The first pass compiles all C# files that are in the Plugins/ directory and the second pass compiles all other C# files.  If Unity does not detect any changes in the Plugins/ directory then Unity will skip this first pass and only execute the second pass.  (Note: This is a bit of a simplification - see [here](http://docs.unity3d.com/Manual/ScriptCompileOrderFolders.html) for full details)

We can take advantage of this by always putting all stable packages (such as those obtained through asset store) into the Plugins folder and putting the packages that we change frequently directly underneath the Assets folder.

And because we are using directory links, and the entire CubeMover-Windows folder is ignored by source control, changing the location of these directory links is trivial.  You can modify this directory structure multiple times per day based on what you're working on, by simply using changing a few things in the Projeny Package Manager window.

To see this in action, open up the AllMovers-Windows project.

By default, the assets folder should appear as follows:

* AllMovers
* CommonShapeMover
* CubeMover
* Plugins
    * Projeny
* SphereMover

If we are only working within the AllMovers project, there is no reason we need to be recompiling CommonShapeMover, CubeMover, and SphereMover projects every time a script changes.  So these projects can be moved to be underneath the Plugins folder.

To do this, open up the package manager by clicking on the menu item `Projeny -> Package Manager...`.  You should see something like this:

<img src="Docs/Screen1.png?raw=true" alt="Package Manager" />

If you click on the Edit button you should see the ProjenyProject.yaml file that we saw previously.  This screen is simply an easier way to edit this through a graphical interface.

Now, drag the CommonShapeMover, CubeMover, and SphereMover projects to the Plugins folder, so that it looks like this:

<img src="Docs/Screen2.png?raw=true" alt="Package Manager" />

Then click the "Apply" button.  This will update the ProjenyProject.yaml file and also update the directory links in our project to match.  After Unity refreshes, if you look at the Assets tab, it should now appear how we want:

* AllMovers
* Plugins
    * CommonShapeMover
    * CubeMover
    * Projeny
    * SphereMover

Now we can continue coding within the AllMovers project and benefit from faster compile times.

### <a id="platform-specific-folders"></a>5 - Platform specific package folders

Unity 5 adds some helpful features, including the ability to enable/disable a DLL based on platform.  If you add a DLL to your project then click on it you get a bunch of checkboxes in the inspector that allow you to choose which platform this DLL is for.   Projeny allows you to do something similar except for all assets and directories.

This is possible because when Projeny generates your unity project, it can choose a different set of package folders to include based on the current platform, since each platform has its own unity project generated for it.

One example use for this is if you had a large Resources folder that contained a lot of data for a specific platform.  Since unity always includes the contents of the Resources folders for builds, this would cause your platform specific files inside Resources to be included on other platforms for no reason.  Using Projeny, you can simply move the Resources folder to a platform specific package to address this problem

This can also make things easier when it comes to code.  You can have entire package folders be platform specific, then you no longer need to add #ifdef's around entire files to avoid the compile errors on other platforms.

For more details on how to use this feature, see <a href="#package-yaml">the section on package configuration</a>.

### <a id="dependency-management"></a>6 - Dependency Management of Packages

When adding a package to the UnityPackages directory, you can also specify the packages that this package depends on.  Then, when Projeny is generating your unity project directory, it can automatically figure out which packages you need.

This is very powerful, because often when working on a new project you want to just be able to ask for package X, and you don't want to have to think about what other packages this package requires.

To see this in action, open up the AllMovers-Windows project again.  Then open the package manager by clicking `Projeny -> Package Manager...`.  In the Project section, try deleting the projects CommonShapeMover, CubeMover, and SphereMover.  You can do this by either clicking them and hitting delete, or right clicking and selecting Remove.  The package manager should then look like this:

<img src="Docs/Screen3.png?raw=true" alt="Package Manager" />

Then hit Apply.  If you then look at the Assets tab, you should see the following directory structure:

* AllMovers
* Plugins
    * CommonShapeMover
    * CubeMover
    * Projeny
    * SphereMover

So the question is, why were the SphereMover, CommonShapeMover, and CubeMover packages added even when not specified in the ProjenyProject.yaml file?

The reason for this is that the AllMovers package has its own set up dependencies.

To see this, click the arrow on the far left side in the Package Manager window.  This will show change the view to show the full list of packages that are available for use in your project, in addition to the previous screen that showed the current ProjenyProject.yaml configuration settings.

Now, right click on the AllMovers package in the Packages list and select "Edit ProjenyPackage.yaml", as shown here:

<img src="Docs/Screen4.png?raw=true" alt="Package Manager" />

This will open up the file at UnityPackages/AllMovers/ProjenyPackage.yaml.  It should appear as follows:

    Dependencies:
        - CubeMover
        - SphereMover

This tells Projeny that whenever the AllMovers package is added to a project, the SphereMover and CubeMover packages should be added as well.  Once projeny processes the ProjenyPackage.yaml file for the AllMovers project, it will then look at the ProjenyPackage.yaml for the CubeMover and the SphereMover projects, and then repeat again for those dependencies, etc.  (The CubeMover and SphereMover packages are what contain the reference to CommonShapeMover).

### <a id="visual-studio-generation"></a>7 - More intelligent Visual Studio Solution generation

Projeny can also take advantage of the dependency information between packages to generate a better Visual Studio project.

To see this in action, open up the AllMovers-Windows project, and then click the menu item `Projeny -> Package Manager...`.  Then click on the arrow button on the right until you read this screen:

<img src="Docs/Screen5.png?raw=true" alt="Package Manager" />

Click the Open Solution button.  You will most likely get an error about the path to Visual Studio not being defined.  To fix this, open up the `Projeny.yaml` file at the root of the folder structure and change it to include a value for `VisualStudioIdePath`.  For example:

    PathVars:
        UnityPackagesDir: '[ConfigDir]/UnityPackages'
        UnityProjectsDir: '[ConfigDir]/UnityProjects'
        LogPath: '[ConfigDir]/PrjLog.txt'
        VisualStudioIdePath: 'C:/Program Files (x86)/Microsoft Visual Studio 12.0/Common7/IDE/devenv.exe'

Now if we click Open Solution again, we should see two C# projects.  One named "AssetsFolder" that contains all C# files under the Assets/ folder and one named "PluginsFolder" that contains all C# files underneath the Plugins/ folder.  So far, this is the same as the solution that Unity produces when it generates its visual studio solution.

Go back to the Package manager and drag the following projects over:

<img src="Docs/Screen6.png?raw=true" alt="Package Manager" />

Now, if you hit Update Solution, and go back to Visual Studio you should see the following:

<img src="Docs/Screen7.png?raw=true" alt="Package Manager" />

As you can no doubt guess by now, every package that you drag to the list on the right in the Package Manager will have a C# project created for it.  You'll also notice that the AssetsFolder has disappeared.  This is because Projeny did not find any files left over to place in it, so it didn't bother to create the project.  But, since we did not drag over the CommonShapeMover project, the PluginsFolder project has remained.

This can be helpful for code organization but more importantly, this allows you to design dependencies on a module level.  In normal unity projects, every code file could potentially make use of any other code file in your entire project.  For small projects this is not an issue, however, as your project scales in size it is helpful to be able to design code at a module level and avoid having your project devolve into a [Big ball of mud](https://en.wikipedia.org/wiki/Big_ball_of_mud).

For example, it is common to build up a library of re-usable utility functions that you can use in multiple unity projects, such as a math library.  For these cases, it would be important to avoid using game-specific code from within your math library, because then your math library is strongly coupled to your game and can't be used in other projects.  If you compile using the Projeny generated solution file, this would not be a problem, since it would guarantee these dependencies remain intact, even though Unity itself would allow them.

Note the following:
- The list of packages that should have corresponding C# projects is also saved to the ProjenyProject.yaml.  If you click the edit button from Package Manager after following the steps above you can see this.
- The C# project dependencies are generated based on the dependencies that are declared for the package in a `ProjenyPackage.yaml` file, as described in the <a href="#dependency-management">previous section</a>.
- The solution file is generated and saved at UnityProjects/AllMovers-Windows.sln.  Not to be confused with UnityProjects/AllMovers-Windows/AllMovers-Windows.sln which is usually the path to the solution generated by Unity
- The solution file and all csproj files are fully generated, and therefore you should not change any settings on them, such as preprocessor defines, output paths, etc. Any change that you make here will be over-written next time you update the solution. Also, note that if you're using source control, the csproj files and solution file will be ignored since the entire unity Assets directory is ignored.
- The DLL's generated by this custom solution are not used at all, just like the DLL's generated by the normal Unity-generated solution are not used at all . Unity still has to recompile all the code itself, even if we've already compiled it using the custom solution.
- The generated solution file is platform specific, since each platform contains different preprocessor defines.  This is good to be aware of because it means you will have to exit visual studio and re-open the solution when changing platforms.

## <a id="managing-assetstore-assets"></a>Managing Asset Store Assets / Releases

If you open the menu item `Projeny -> Package Manager` and click the left arrow all the way to the left, you should see something similar to the following:

<img src="Docs/Screen8.png?raw=true" alt="Package Manager" />

This is the list of "releases".  A "Release" refers to an external collection of assets, often with an associated version number.  In most cases, these refer to items that you've downloaded through the asset store but they can also be retrieved from other sources (such as a local folder or a remote file server).

By default, Projeny will scan your asset store cache to populate this list, so you will likely see some familiar assets listed here.

As an example, choose one of these assets and drag it into the Packages list on the right.  I'm going to choose Asset Store Tools.  After Projeny finishes creating the new package you should see something like the following:

<img src="Docs/Screen9.png?raw=true" alt="Package Manager" />

You'll notice that the name of the package does not necessarily correspond exactly to the name of the release.  This is because by default, Projeny will use the extracted folder name as the package name.  This is necessary in some cases because some assets might require a specific folder name.  Note that you can rename the package to whatever you want after adding it (through right click menu) if the default name is not ideal.

You'll also notice that the actual release name is displayed in green.  Every time you install a release, projeny adds a file named ProjenyInstall.yaml to the new package folder.  This file contains information about where the package came from, what version it is, etc.  This file is how Projeny is able to recognize which packages have a corresponding release.  Note that in many cases there is none, and the package was simply created by itself (eg. AllMovers, CubeMover, etc.)

This file is also what Projeny uses to detect when you are upgrading or downgrading a package.  For example, if I now drag in Asset Store Tools again, but this time I choose version "4.0.5", you will get the following popup:

<img src="Docs/Screen10.png?raw=true" alt="Package Manager" />

This same popup will be displayed when downgrading a package as well.  This can be very useful, because you do not have to be afraid of upgrading and potentially introducing new issues to your project.  You can rest assured that the previous versions will remain in the Releases list in case you ever need to downgrade (which isn't possible using the asset store)

Note also that after you add the release as a package, you will also have to add it to your project, to actually have it appear in unity.

Also note that this list is generated partially from your asset store cache, so in order to have new asset store items listed here you will have to purchase them through the asset store, click download, and then immediately cancel the import popup.   After that, you can hit the refresh button underneath the Releases list to have your newly purchases asset ready for use.

For information on defining your own release source, for use in addition to the asset store source (for example by using a local folder or a remote file server) see <a href="#custom-release-registries">this section</a>

## <a id="gotchas"></a>Gotchas / Miscellaneous Tips and Tricks

* After opening your project for the first time (or when adding new packages) Unity will show the following warning:

```
[Asset] is a symbolic link. Using symlinks in Unity projects may cause your project to become corrupted if you create multiple references to the same asset, use recursive symlinks or use symlinks to share assets between projects used with different versions of Unity. Make sure you know what you are doing.
```

However, we are not doing any of the things that Unity warns about here so this warning can be ignored.

## <a id="faq"></a>Frequently Asked Questions

#### <a id="workflow-create-package"></a>How do I create a new package?

* Method 1 - Using the Package Manager
    * Click the menu item `Projeny -> Package Manager`
    * Go to the Packages section by pressing the arrow button the left
    * Click the new button
    * Enter name for your package
    * Add your package to your project by dragging it to either Assets or Plugins on the right
    * (optional) Add a ProjenyPackage.yaml file to your new package folder.   See <a href="#package-yaml">here</a> for details.

* Method 2 - Manually
    * Go to the UnityPackages directory
    * Create a new folder with the name of your package
    * Done. You can now refer to this package by its folder name in ProjenyPackage.yaml or ProjenyProject.yaml files
    * (optional) Add a ProjenyPackage.yaml file to your new package folder.   See <a href="#package-yaml">here</a> for details.

#### <a id="workflow-create-project"></a>How do I create a new project?

* Method 1 - Within Unity
    * Click the menu item `Projeny -> Change Project -> New...`
    * Enter the name for your new project
    * After the new project loads, open the Package Manager again to add packages, etc.
    * Done
    * (optional) Add a ProjenyProject.yaml file to your new project folder. See <a href="#project-yaml">here</a> for details.

* Method 2 - Command Line
    * Enter command prompt / powershell at the same directory where your `Projeny.yaml` file is
    * Execute `prj --project MyNewProject --createProject` (or the shortened form `prj -p MyNewProject -cpr`)
    * Done.  You can now open your project in unity
    * (optional) Add a ProjenyProject.yaml file to your new project folder. See <a href="#project-yaml">here</a> for details.

## <a id="project-yaml"></a>ProjenyProject.yaml reference

In most cases you can edit the ProjenyProject.yaml file using the Package Manager from within Unity.  However, the Package Manager GUI does not include everything (for example, solution folders cannot be configured from package manager)

The format of ProjenyProject.yaml:

    AssetsFolder:
        {PackageName}
        {PackageName}
    PluginsFolder:
        {PackageName}
        {PackageName}
        {PackageName}
    SolutionProjects:
        {PackageName}
        /{PackageNamePattern}
        /{PackageNamePattern}
        {PackageName}
    ProjectFolders
        {FolderName}: /{PackageNamePattern}
        {FolderName}: /{PackageNamePattern}

* The `{PackageName}` items above would be replaced with names of directories that are below your UnityPackages directory.
* Packages that are listed underneath the `packages` category will be placed directly underneath the Assets/ directory of your project
* Packages that are listed underneath the `PluginsFolder` category will be placed directly underneath the Assets/Plugins directory of your project
* All packages underneath the `solutionProjects` category will have their own .csproj file generated, when running `Project -> Custom Solution -> Update` from within unity or executing the <a href="##commandline-updateCustomSolution">`-ucs` command line option</a>
    * Note that in this case you can also use a regular expression instead of explitly listing every package you want a project for
    * It is common to want a csproj file generated for every package in your project, in which case you can add the line `/.*` which will match everything
* You can also optionally add folders to the generated solution, to organize related projects together.  Each folder has one regex pattern that is used to filter the full list of projects.
* Note that the regex used here following the regex rules defined for python (more details <a href="https://docs.python.org/2/library/re.html">here</a>)

## <a id="projeny-yaml"></a>Projeny.yaml reference

TBD

## <a id="package-yaml"></a>Package.ini reference

In most cases your ProjenyPackage.yaml will simply list the other packages that this package depends on.  It will look like this:

[Config]
    Dependencies:
        MyPackageA
        MyPackageB
        MyPackageC

Also note that the ProjenyPackage.yaml file is optional.  If not supplied, Projeny will assume your project has zero dependencies.

However, there is a few other options here for less common cases.  The full format of the ProjenyPackage.yaml is as follows:

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

## <a id="custom-release-registries"></a>Custom Release Sources

A mentioned in the <a href="#managing-assetstore-assets">above section</a>, the list of releases _usually_ corresponds to your list of asset store purchases, however it supports other sources as well.  

Every source is ultimately just a collection of unity packages.  This is also what unity stores in the asset store cache, so even in that case, it is just a list of unity packages.

For example, to add a new local folder source, open up one of your projeny config files (Projeny.yaml) and include the following:

    ReleaseSources:
        - LocalFolder:
            Path: 'C:/MyLocalFolderSource'

One convenient place that you might want to put this is in the system wide projeny config, which can be found in your user home directory at `C:/Users/[Your User Name]/Projeny.yaml`.  This is convenient because these releases will then be available for any unity project on your computer.

Now, if you copy and paste `.unitypackage` files into this folder, and click the Refresh button in the Package Manager (accessed within unity through the menu at `Projeny -> Package Manager`) then these `.unitypackage` files will be displayed in the Releases list.

Note that you can add multiple local folder sources using different paths, including those on a network share.

Sharing a release source over a network can be very useful when working in an office environment that has its own LAN.   Your organization can build up a big collection of "releases" and anyone in the organization can have access to.

If you don't want to use a network share for this, you can also define a FileServer release source, which is declared using a URL.  You can then run a static web site that can serve out the unity packages to anyone on the network.

First, you have to host a static web site that simply contains a flat list of `.unitypackage` files.  Then you need to run `PrjUpdateReleaseManifest [directory]` with the path to the directory you want to scan (or simply `.` for current directory).  This will result in a file being created in this same directory called `ProjenyReleaseManifest.txt`

Then you can declare your release source in one of your `Projeny.yaml` as follows:

    ReleaseSources:
        - FileServer:
            ManifestUrl: 'http://mysharedserver/ProjenyReleaseManifest.txt'

## <a id="command-line-reference"></a>Command Line Reference

Almost all operations in projeny can be executed within unity using the Projeny menu or the Package Manager.  However, it can also be useful to be able to drive it from the command line, especially if you want to automate any projeny operations yourself.

What follows is the full list of command line parameters that you can pass to the `Prj` command.  Note that you can pass any combination of these and Prj will execute them in a reasonable order.

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
    * Projeny will read the Project.yaml file associated with the given project, then calculate all the packages that it needs to include.  For each package, it will then create a directory link (aka windows junction aka symbolic link) inside either the Assets/ directory or the Assets/Plugins directory
    * Note that in order to run this command you must <a href="#commandline-project">specify a project</a> (or set a default project in ProjenyConfig.xml) and also optionally <a href="#commandline-platform">set a platform</a> (otherwise it will assume windows)

* #### <a id="commandline-listProjects"></a>`--listProjects` / `-lp`
    * Lists the names of all the directories that are underneath the UnityProjects directory, along with the alias for each if one is defined.

* #### <a id="commandline-updateCustomSolution"></a>`--updateCustomSolution` / `-ucs`
    * Generates .csproj files and a .sln file based on the configuration set in the Project.yaml for the given project and given platform
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

## <a id="appendix"></a>Appendix

### <a id="dllgotchas"></a>A. "Gotchas" with using external assemblies:

* You cannot really use unity preprocessor defines inside your DLL (eg: `UNITY_WEBPLAYER`, `UNITY_5_3`, etc.) unless you create separate DLL's for each platform. This is helped somewhat by Unity 5.3, which allows you to indicate which platforms to use a given DLL in.
* There are some known limitations of using external assemblies for code - in particular, any MonoBehaviour with a custom base class will not be allowed to be added to a game object.
* Unity is not aware of the location of each MonoBehaviour.  You cannot for example, double click on a MonoBehaviour to open up the source file for it.

## <a id="release-notes"></a>Release Notes

0.2 (December, 2015)
- Added GUI for package management within Unity, also added a lot more documentation

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
