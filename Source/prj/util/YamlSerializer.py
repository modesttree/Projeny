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
    return _deserializeObj(yaml.load(yamlStr))

def _deserializeObj(data):
    dataType = type(data)

    if dataType is dict:
        newDict = {}
        for pair in data.items():
            key = pair[0]
            value = _deserializeObj(pair[1])
            newDict[key[0].lower() + key[1:]] = value
        return YamlData(newDict)

    if dataType is list:
        return [_deserializeObj(x) for x in data]

    return data

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

        if value != None and inspect.isclass(valueType) and valueType not in (int, float, bool, str, datetime, list):
            value = _convertToDict(value)

        if valueType is list:
            value = [_convertToDict(x) for x in value]

        newObj[key[0].upper() + key[1:]] = value

    return newObj

