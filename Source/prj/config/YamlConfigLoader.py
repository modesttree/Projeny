
import os
import yaml

from prj.util.Assert import *

def loadYamlFilesThatExist(*paths):
    configs = []

    for path in paths:
        if os.path.isfile(path):
            config = loadYamlFile(path)

            if config != None:
                configs.append(config)

    return configs

def loadYamlFile(path):
    return yaml.load(readAllTextFromFile(path))

def readAllTextFromFile(filePath):
    with open(filePath, 'r', encoding='utf-8') as f:
        return f.read()

