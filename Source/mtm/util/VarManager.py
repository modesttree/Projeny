
import os
import re

import mtm.util.MiscUtil as MiscUtil

import mtm.ioc.Container as Container
from mtm.ioc.Inject import Inject
from mtm.ioc.Inject import InjectOptional
import mtm.ioc.IocAssertions as Assertions

from mtm.util.Assert import *

class VarManager:
    _config = Inject('Config')

    '''
    Stores a dictionary of keys to values to replace path variables with
    '''
    def __init__(self, initialParams = None):
        self._params = initialParams if initialParams else {}
        self._params['StartCurrentDir'] = os.getcwd()
        self._params['ExecDir'] = MiscUtil.getExecDirectory().replace('\\', '/')

        # We could just call self._config.getDictionary('PathVars') here but
        # then we wouldn't be able to use fallback (?) and override (!) characters in
        # our config

        self._regex = re.compile('^([^\[]*)(\[[^\]]*\])(.*)$')

    def hasKey(self, key):
        return key in self._params or self._config.tryGet('PathVars', key) != None

    def get(self, key):
        if key in self._params:
            return self._params[key]

        return self._config.getString('PathVars', key)

    def tryGet(self, key):
        if key in self._params:
            return self._params[key]

        return self._config.tryGetString(None, 'PathVars', key)

    def add(self, key, value):
        self._params[key] = value

    def set(self, key, value):
        self._params[key] = value

    def expandPath(self, text, extraVars = None):
        ''' Same as expand() except it cleans up the path to remove ../ '''
        return os.path.realpath(self.expand(text, extraVars))

    def expand(self, text, extraVars = None):

        if not extraVars:
            extraVars = {}

        allArgs = self._params.copy()
        allArgs.update(extraVars)

        while True:
            match = self._regex.match(text)

            if not match:
                break

            prefix = match.group(1)
            var = match.group(2)
            suffix = match.group(3)

            var = var[1:-1]

            if var in allArgs:
                replacement = allArgs[var]
            else:
                replacement = self.get(var)

            text = prefix + replacement + suffix

        if '[' in text:
            raise Exception("Unable to find all keys in path '{0}'".format(text))

        return text

