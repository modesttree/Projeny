using System.IO;
using System.Linq;
using System;

using UnityEngine;

using YamlDotNet.Core;
using YamlDotNet.Serialization.NodeDeserializers;
using YamlDotNet.Serialization;

public class Validating_during_deserialization : MonoBehaviour {

    void Start () {
        // Wrap the existing ObjectNodeDeserializer
        // with our ValidatingNodeDeserializer:
        
        var deserializer = new Deserializer();

        var objectDeserializer = deserializer.NodeDeserializers
            .Select((d, i) => new {
                Deserializer = d as ObjectNodeDeserializer,
                Index = i
            })
            .First(d => d.Deserializer != null);
        
        deserializer.NodeDeserializers[objectDeserializer.Index] =
            new ValidatingNodeDeserializer(objectDeserializer.Deserializer);
        
        // This will cause a message to be logged in the console
        deserializer.Deserialize<Data>(new StringReader(@"Name: ~"));
    }
}

// By manipulating the list of node deserializers,
// it is easy to add behavior to the deserializer.
// This example shows how to validate the objects as they are deserialized.

// First, we'll implement a new INodeDeserializer
// that will decorate another INodeDeserializer with validation:
public class ValidatingNodeDeserializer : INodeDeserializer
{
    private readonly INodeDeserializer _nodeDeserializer;

    public ValidatingNodeDeserializer(INodeDeserializer nodeDeserializer)
    {
        _nodeDeserializer = nodeDeserializer;
    }

    public bool Deserialize(EventReader reader, Type expectedType,
        Func<EventReader, Type, object> nestedObjectDeserializer,
        out object value)
    {
        if (_nodeDeserializer.Deserialize(reader, expectedType,
            nestedObjectDeserializer, out value))
        {
            if (((Data)value).Name == null) 
            {
                Debug.Log("ValidatingNodeDeserializer found that 'Name' was missing or null");
            }
            return true;
        }
        return false;
    }
}

public class Data
{
    public string Name { get; set; }
}
