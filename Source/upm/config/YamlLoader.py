
import os
import yaml

from upm.util.Assert import *

def loadYamlFilesThatExist(*paths):
    configs = []

    for path in paths:
        if os.path.isfile(path):
            config = yaml.load(readAllTextFromFile(path))
            configs.append(config)

    return configs

def readAllTextFromFile(filePath):
    with open(filePath, 'r', encoding='utf-8') as f:
        return f.read()

