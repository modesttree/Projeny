using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

using UnityEngine;

using YamlDotNet.RepresentationModel;

public class Loading_a_YAML_stream : MonoBehaviour {

    void Start () {
        var input = new StringReader(Document);

        var yaml = new YamlStream();
        yaml.Load(input);

        // Examine the stream
        var mapping =
            (YamlMappingNode)yaml.Documents[0].RootNode;

        var output = new StringBuilder();
        foreach (var entry in mapping.Children)
        {
            output.AppendLine(((YamlScalarNode)entry.Key).Value);
        }

        var items = (YamlSequenceNode)mapping.Children[new YamlScalarNode("items")];
        foreach (YamlMappingNode item in items)
        {
            output.AppendLine(
                String.Format("{0}\t{1}",
                    item.Children[new YamlScalarNode("part_no")],
                    item.Children[new YamlScalarNode("descrip")]
                )
            );
        }
        Debug.Log(output);
    }

    private const string Document = @"---
        receipt:    Oz-Ware Purchase Invoice
        date:        2007-08-06
        customer:
            given:   Dorothy
            family:  Gale

        items:
            - part_no:   A4786
              descrip:   Water Bucket (Filled)
              price:     1.47
              quantity:  4

            - part_no:   E1628
              descrip:   High Heeled ""Ruby"" Slippers
              price:     100.27
              quantity:  1

        bill-to:  &id001
            street: |
                    123 Tornado Alley
                    Suite 16
            city:   East Westville
            state:  KS

        ship-to:  *id001

        specialDelivery:  >
            Follow the Yellow Brick
            Road to the Emerald City.
            Pay no attention to the
            man behind the curtain.
...";
}
