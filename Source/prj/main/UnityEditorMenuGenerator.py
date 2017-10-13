
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
    

    def Generate(self, currentProjName, currentPlatform, outputPath, allProjectNames):
        foundCurrent = False
        methodsText = ""
        projIndex = 1
        for projName in allProjectNames:
            
            projConfig = self._schemaLoader.loadProjectConfig(projName)

            for platform in projConfig.targetPlatforms:
                methodsText += """
        [MenuItem("Projeny/Change Project/{0}-{1}", false, 8)]
        public static void ChangeProject{2}()""".format(projName, platform, projIndex)
                methodsText += """
        {
            PrjHelper.ChangeProject("{0}", "{1}");
        }
        """

                if projName == currentProjName and platform == currentPlatform:
                    assertThat(not foundCurrent)
                    foundCurrent = True
                    methodsText += """
        [MenuItem("Projeny/Change Project/{0}-{1}", true, 8)]
        public static bool ChangeProject{2}Validate()""".format(currentProjName, currentPlatform, projIndex)
                    methodsText += """
        {
            return false;
        }"""

                projIndex += 1

        #assertThat(foundCurrent, "Could not find project " + currentProjName)
        fileText = self._ChangeProjectMenuClassTemplate.substitute(methods = methodsText)
        # self._log.info(fileText)
        self._sys.writeFileAsText(outputPath, fileText)