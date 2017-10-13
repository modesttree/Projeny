
from string import Template

from mtm.util.Assert import *
from mtm.ioc.Inject import Inject
from mtm.log.Logger import Logger

class UnityEditorMenuGenerator:
    _schemaLoader = Inject('ProjectSchemaLoader')
    _sys = Inject('SystemHelper')
    _log = Inject('Logger')

    _ChangeProjectMenuClassTemplate = Template(
"""
using UnityEditor;
using Projeny.Internal;

namespace Projeny
{
    public static class ProjenyChangeProjectMenu
    {
        $methods
    }
}
""")

    _changeProjectMethodTemplate = Template("""
        [MenuItem("Projeny/Change Project/$name-$platform", false, 8)]
        public static void ChangeProject$index()
        {
            PrjHelper.ChangeProject("$name", "$platform");
        }"""
    )
    
    _currentProjectMethodTemplate = Template("""
        [MenuItem("Projeny/Change Project/$name-$platform", true, 8)]
        public static bool ChangeProject$indexValidate()
        {
            return false;
        }"""
    )

    def Generate(self, currentProjName, currentPlatform, outputPath, allProjectNames):
        foundCurrent = False
        methodsText = ""
        projIndex = 1
        for projName in allProjectNames:
            
            projConfig = self._schemaLoader.loadProjectConfig(projName)

            for platform in projConfig.targetPlatforms:
                methodsText += self._changeProjectMethodTemplate.substitute(name = projName, platform = platform, index = projIndex)

                if projName == currentProjName and platform == currentPlatform:
                    assertThat(not foundCurrent)
                    foundCurrent = True
                    methodsText += _currentProjectMethodTemplate.substitute(name = projName, platform = platform, index = projIndex)

                projIndex += 1

        #assertThat(foundCurrent, "Could not find project " + currentProjName)
        fileText = self._ChangeProjectMenuClassTemplate.substitute(methods = methodsText)
        # self._log.info(fileText)
        self._sys.writeFileAsText(outputPath, fileText)