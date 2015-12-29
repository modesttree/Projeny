using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System;

using UnityEngine;

using YamlDotNet.Core.Events;
using YamlDotNet.Core;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

public class Deserializing_multiple_documents : MonoBehaviour {

    // Use this for initialization
    void Start () {
        var input = new StringReader(Document);
        
        var deserializer = new Deserializer();
        
        var reader = new EventReader(new Parser(input));
        
        // Consume the stream start event "manually"
        reader.Expect<StreamStart>();
        
        var output = new StringBuilder();
        while(reader.Accept<DocumentStart>())
        {
            // Deserialize the document
            var doc = deserializer.Deserialize<List<string>>(reader);
        
            output.AppendLine("## Document");
            foreach(var item in doc)
            {
                output.AppendLine(item);
            }
        }    
        Debug.Log(output);
        
    }

    private const string Document = @"---
- Prisoner
- Goblet
- Phoenix
---
- Memoirs
- Snow 
- Ghost        
...";
}
