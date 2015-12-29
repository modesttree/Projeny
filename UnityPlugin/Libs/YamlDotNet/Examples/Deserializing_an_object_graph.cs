using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System;

using UnityEngine;

using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

public class Deserializing_an_object_graph : MonoBehaviour {

    void Start () {
        var input = new StringReader(Document);

        var deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention());

        var order = deserializer.Deserialize<Order>(input);

        var output = new StringBuilder();
        output.AppendLine("Order");
        output.AppendLine("-----");
        output.AppendLine();
        foreach(var item in order.Items)
        {
            output.AppendLine(String.Format("{0}\t{1}\t{2}\t{3}", item.PartNo, item.Quantity, item.Price, item.Descrip));
        }
        output.AppendLine();

        output.AppendLine("Shipping");
        output.AppendLine("--------");
        output.AppendLine();
        output.AppendLine(order.ShipTo.Street);
        output.AppendLine(order.ShipTo.City);
        output.AppendLine(order.ShipTo.State);
        output.AppendLine();

        output.AppendLine("Billing");
        output.AppendLine("-------");
        output.AppendLine();
        if(order.BillTo == order.ShipTo) {
            output.AppendLine("*same as shipping address*");
        } else {
            output.AppendLine(order.ShipTo.Street);
            output.AppendLine(order.ShipTo.City);
            output.AppendLine(order.ShipTo.State);
        }
        output.AppendLine();

        output.AppendLine("Delivery instructions");
        output.AppendLine("---------------------");
        output.AppendLine();
        output.AppendLine(order.SpecialDelivery);

        Debug.Log(output);
    }

    public class Order
    {
        public string Receipt { get; set; }
        public DateTime Date { get; set; }
        public Customer Customer { get; set; }
        public List<OrderItem> Items { get; set; }

        [YamlMember(Alias = "bill-to")]
        public Address BillTo { get; set; }

        [YamlMember(Alias = "ship-to")]
        public Address ShipTo { get; set; }

        public string SpecialDelivery { get; set; }
    }

    public class Customer
    {
        public string Given { get; set; }
        public string Family { get; set; }
    }

    public class OrderItem
    {
        [YamlMember(Alias = "part_no")]
        public string PartNo { get; set; }
        public string Descrip { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
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
            street: |-
                    123 Tornado Alley
                    Suite 16
            city:   East Westville
            state:  KS

        ship-to:  *id001

        specialDelivery: >
            Follow the Yellow Brick
            Road to the Emerald City.
            Pay no attention to the
            man behind the curtain.
...";
}
