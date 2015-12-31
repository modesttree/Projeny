import yaml
import inspect
from datetime import datetime

def serialize(obj):
    # width is necessary otherwise it can insert newlines into string values
    return yaml.dump(_convertToDict(obj), width=9999999, default_flow_style=False)

def deserialize(yamlStr):
    return yaml.load(yamlStr)

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

