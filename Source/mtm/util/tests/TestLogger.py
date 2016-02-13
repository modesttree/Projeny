
import os
import unittest

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject

from mtm.util.VarManager import VarManager
from mtm.log.Logger import Logger
from mtm.log.LogStreamConsole import LogStreamConsole
from mtm.config.Config import Config

from mtm.util.Assert import *

ScriptDir = os.path.dirname(os.path.realpath(__file__))

class TestLogger(unittest.TestCase):
    def setUp(self):
        Container.clear()

    def _installBindings(self):
        Container.bind('Logger').toSingle(Logger)
        Container.bind('LogStream').toSingle(LogStreamConsole, True, True)
        config = {
        }
        Container.bind('Config').toSingle(Config, [config])

    def testOutputToConsole(self):
        self._installBindings()
        log = Container.resolve('Logger')

        with log.heading("heading 1"):
            log.info("test of params: {0}", 5)

            with log.heading("heading 2"):
                log.error("test of params: {0}", 5)
                log.good("test of params: {0}", 5)

            log.info("test of params: {0}", 5)
            log.info("info 1")
            log.error("error 1")
            log.good("good 1")
            log.info("info 2")

            with log.heading("heading 2"):
                log.info("info 3")
                log.good("Done")

    #def testOutputToFile(self):
        ##Container.bind('Config').toSingle(ConfigXml)
        #Container.bind('VarManager').toSingle(VarManager, {
            #'LogPath': ScriptDir + '/logtest.txt',
            #'LogPathPrevious': ScriptDir + '/logtest.prev.txt',
        #})

        #log = Logger(False, True)

        #log.heading("heading 1")
        #log.info("info 1")
        #log.error("error 1")
        #log.good("good 1")
        #log.info("info 2")
        #log.heading("heading 2")
        #log.info("info 3")
        #log.good("Done")

        #log.dispose()

if __name__ == '__main__':
    unittest.main()
