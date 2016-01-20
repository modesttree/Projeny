import yaml
import inspect
from datetime import datetime

class YamlData:
    def __init__(self, data):
        self.__dict__.update(data)

def serialize(obj):
    # width is necessary otherwise it can insert newlines into string values
    return yaml.dump(_serializeObj(obj), width=9999999, default_flow_style=False)

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

    if data == None:
        return YamlData({})

    return data

# This is necessary because otherwise yaml inserts python specific type information
def _serializeObj(obj):

    if obj == None:
        return None

    objType = type(obj)

    if objType is list:
        obj = [_serializeObj(x) for x in obj]

    elif objType in (int, float, bool, str, datetime):
        # Do nothing
        pass

    else:
        if objType is not dict:
            obj = obj.__dict__

        oldItems = obj.items()
        obj = {}
        for pair in oldItems:
            key = pair[0]
            value = pair[1]

            # Our convention with YAML is PascalCase
            obj[key[0].upper() + key[1:]] = _serializeObj(value)

    return obj

