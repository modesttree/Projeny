
import re
import sys
import os

import upm.util.MiscUtil as MiscUtil

import upm.ioc.Container as Container
from upm.ioc.Inject import Inject, InjectOptional
import upm.ioc.IocAssertions as Assertions

from upm.util.Assert import *

class VarManager:
    _config = Inject('Config')

    '''
    Stores a dictionary of keys to values to replace path variables with
    '''
    def __init__(self, initialParams = None):
        self._params = initialParams if initialParams else {}
        self._params['StartCurrentDir'] = os.getcwd()
        self._params['ExecDir'] = MiscUtil.getExecDirectory().replace('\\', '/')

        configPaths = self._config.tryGetDictionary({}, 'PathVars')

        for key, value in configPaths.items():
            assertThat(not key in self._params.keys())
            self._params[key] = value

    def hasKey(self, key):
        return key in self._params

    def get(self, key):
        assertThat(key in self._params, 'Missing variable "{0}"'.format(key))
        return self._params[key]

    def add(self, key, value):
        assertThat(not key in self._params)
        self._params[key] = value

    def set(self, key, value):
        self._params[key] = value

    def tryGet(self, key):
        return self._params.get(key)

    def expandPath(self, text, extraVars = None):
        ''' Same as expand() except it cleans up the path to remove ../ '''
        return os.path.realpath(self.expand(text, extraVars)).replace("\\", "/")

    def expand(self, text, extraVars = None):

        if not extraVars:
            extraVars = {}

        allArgs = self._params.copy()
        allArgs.update(extraVars)

        lastText = None

        while '[' in text and lastText != text:
            lastText = text
            for arg in allArgs.items():
                text = text.replace('[' + arg[0] + ']', arg[1])

        if '[' in text:
            raise Exception("Unable to find all keys in path '{0}'".format(text))

        return text

