import yaml
import inspect
from datetime import datetime

class YamlData:
    def __init__(self, data):
        self.__dict__.update(data)

def serialize(obj):
    # width is necessary otherwise it can insert newlines into string values
    return yaml.dump(_convertToDict(obj), width=9999999, default_flow_style=False)

def deserialize(yamlStr):
    return _convertFromDict(yaml.load(yamlStr))

def _convertFromDict(data):
    newDict = {}
    for pair in data.items():
        key = pair[0]
        value = pair[1]

        if type(value) is dict:
            value = _convertFromDict(value)

        newDict[key[0].lower() + key[1:]] = value

    return YamlData(newDict)

# This is necessary because otherwise yaml inserts python specific type information
def _convertToDict(obj):
    if type(obj) is not dict:
        obj = obj.__dict__

    # Our convention with YAML is PascalCase
    newObj = {}
    for pair in obj.items():
        key = pair[0]
        value = pair[1]
        valueType = type(value)

        if value != None and inspect.isclass(valueType) and valueType not in (int, float, bool, str, datetime):
            value = _convertToDict(value)

        newObj[key[0].upper() + key[1:]] = value

    return newObj

