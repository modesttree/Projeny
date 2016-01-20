
import os
import unittest

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
import mtm.ioc.IocAssertions as Assertions

from mtm.util.VarManager import VarManager
from mtm.log.Logger import Logger

ScriptDir = os.path.dirname(os.path.realpath(__file__))

class TestLogger(unittest.TestCase):
    def setUp(self):
        Container.clear()

    def testOutputToConsole(self):
        assertThat(False)
        #Container.bind('Config').toSingle(ConfigXml)

        log = Logger(True)

        log.heading("heading 1")
        log.info("test of params: {0}", 5)
        log.error("test of params: {0}", 5)
        log.good("test of params: {0}", 5)
        log.heading("test of params: {0}", 5)
        log.info("test of params: {0}", 5)
        log.info("info 1")
        log.error("error 1")
        log.good("good 1")
        log.info("info 2")
        log.heading("heading 2")
        log.info("info 3")
        log.finished("Done")

    def testOutputToFile(self):
        assertThat(False)
        #Container.bind('Config').toSingle(ConfigXml)
        Container.bind('VarManager').toSingle(VarManager, {
            'LogPath': ScriptDir + '/logtest.txt',
            'LogPathPrevious': ScriptDir + '/logtest.prev.txt',
        })

        log = Logger(False, True)

        log.heading("heading 1")
        log.info("info 1")
        log.error("error 1")
        log.good("good 1")
        log.info("info 2")
        log.heading("heading 2")
        log.info("info 3")
        log.finished("Done")

        log.dispose()

if __name__ == '__main__':
    unittest.main()
