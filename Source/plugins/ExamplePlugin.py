
# This is an example of adding a custom plugin to Projeny
# If you uncomment this then initialize a new project (for eg. "prj -p MyProject -bf")
# Then after that completes there should be a new file at UnityProjects/MyProject/MyP-win/MyExampleFile.txt

#import prj.ioc.Container as Container
#from prj.ioc.Inject import Inject

#class CustomProjectInitHandler:
    #_varMgr = Inject('VarManager')

    #def onProjectInit(self, projectName, platform):
        #outputPath = self._varMgr.expand('[ProjectPlatformRoot]/MyExampleFile.txt')

        #with open(outputPath, 'w') as f:
            #f.write("This is a sample of configuring the generated project directory")

#Container.bind('ProjectInitHandlers').toSingle(CustomProjectInitHandler)

