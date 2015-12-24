
import sys
import os
import unittest

import upm.ioc.Container as Container
from upm.ioc.Inject import Inject
import upm.ioc.IocAssertions as Assertions

from upm.util.VarManager import VarManager
from upm.util.SystemHelper import SystemHelper
from upm.log.Logger import Logger

class TestSystemHelper(unittest.TestCase):
    def setUp(self):
        Container.clear()

    def test1(self):
        assertThat(False, "TODO")
        #Container.bind('Config').toSingle(ConfigXml)
        Container.bind('VarManager').toSingle(VarManager)
        Container.bind('SystemHelper').toSingle(SystemHelper)
        Container.bind('Logger').toSingle(Logger)

        self._test1(Container.resolve('SystemHelper'))

    def _test1(self, sysHelper):
        '''
        @type sysHelper: SystemHelper
        '''
        output = sysHelper.executeAndReturnOutput('echo 5')
        self.assertEqual(output, '5')

        self.assertTrue(sysHelper.fileExists(os.path.realpath(__file__)))
        self.assertTrue(not sysHelper.fileExists('sadfcvzxvasdf'))

        #sysHelper.executeAndWait('cmd /c mklink')
        sysHelper.executeAndWaitWithParams(None, 'cmd', '/c', 'mklink')

if __name__ == '__main__':
    unittest.main()


