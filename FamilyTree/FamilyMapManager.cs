using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public class FamilyMapManager
{
    private string _mapPath;
    private XDocument _xmlDoc;
    private XElement _root;
    private XElement _graphElement;

    public FamilyMapManager(string savePath)
    {
        _mapPath = savePath;
        LoadOrCreateFamilyMap();
    }

    private void LoadOrCreateFamilyMap()
    {
        if (File.Exists(_mapPath))
        {
            _xmlDoc = XDocument.Load(_mapPath);
            _root = _xmlDoc.Root;
        }
        else
        {
            // Create the root element without the xmlns namespace
            _root = new XElement("graphml");

            // Add the <key> elements at the beginning
            var keys = new List<XElement>
            {
                new XElement("key", new XAttribute("attr.name", "TypeName"), new XAttribute("attr.type", "string"), new XAttribute("for", "node"), new XAttribute("id", "TypeName")),
                new XElement("key", new XAttribute("attr.name", "Category"), new XAttribute("attr.type", "string"), new XAttribute("for", "node"), new XAttribute("id", "Category")),
                new XElement("key", new XAttribute("attr.name", "IsShared"), new XAttribute("attr.type", "boolean"), new XAttribute("for", "node"), new XAttribute("id", "IsShared")),
                new XElement("key", new XAttribute("attr.name", "ParentFamily"), new XAttribute("attr.type", "boolean"), new XAttribute("for", "node"), new XAttribute("id", "ParentFamily")),
                new XElement("key", new XAttribute("attr.name", "NestedFamilies"), new XAttribute("attr.type", "string"), new XAttribute("for", "node"), new XAttribute("id", "NestedFamilies")),
                new XElement("key", new XAttribute("attr.name", "SuperComponent"), new XAttribute("attr.type", "string"), new XAttribute("for", "node"), new XAttribute("id", "SuperComponent"))
            };

            _root.Add(keys);

            // Add the <graph> element (not self-closing)
            _graphElement = new XElement("graph", new XAttribute("edgedefault", "undirected"));
            _root.Add(_graphElement);

            // Create the XDocument and save it
            _xmlDoc = new XDocument(_root);
            _xmlDoc.Save(_mapPath);
        }

        // Ensure the <graph> element is available
        _graphElement = _root.Element("graph") ?? new XElement("graph", new XAttribute("edgedefault", "undirected"));
        _root.Add(_graphElement);
    }

    public void ProcessFamilyData(Dictionary<string, object> familyData)
    {
        var nestedFamilies = familyData["NestedFamilies"] as List<string> ?? new List<string>();
        var selfNodeId = familyData["TypeName"] as string ?? "Unknown";
        var category = familyData["Category"] as string ?? "Unknown";
        var isShared = familyData.ContainsKey("IsShared") && (bool)familyData["IsShared"];
        var isParentFamily = familyData.ContainsKey("ParentFamily") && (bool)familyData["ParentFamily"];
        var superComponent = familyData.ContainsKey("SuperComponent") ? familyData["SuperComponent"] as string : null;

        // Add nested families as nodes if they don't exist
        foreach (var nf in nestedFamilies)
        {
            if (!CheckXmlElement("node", "id", nf))
            {
                _graphElement.Add(new XElement("node", new XAttribute("id", nf)));
            }
        }

        // Add the current family as a node if it doesn't exist
        if (!CheckXmlElement("node", "id", selfNodeId))
        {
            var newNode = new XElement("node", new XAttribute("id", selfNodeId));
            _graphElement.Add(newNode);

            var nodeData = new Dictionary<string, string>
            {
                { "TypeName", selfNodeId },
                { "Category", category },
                { "IsShared", isShared.ToString().ToLower() },
                { "ParentFamily", isParentFamily.ToString().ToLower() },
                { "NestedFamilies", FormatNestedFamilies(nestedFamilies) }, // Format as a JSON-like array string
                { "SuperComponent", superComponent ?? string.Empty } // Add SuperComponent property
            };

            foreach (var data in nodeData)
            {
                CreateXmlElementById("node", selfNodeId, "data", new Dictionary<string, string> { { "key", data.Key } }, data.Value);
            }
        }

        // Connect nested families to the parent family (prevent self-connecting)
        foreach (var nf in nestedFamilies)
        {
            if (nf != selfNodeId) // Prevent self-connecting
            {
                string edgeId = nf + "_to_" + selfNodeId;
                if (!CheckXmlElement("edge", "id", edgeId))
                {
                    _graphElement.Add(new XElement("edge", new XAttribute("id", edgeId), new XAttribute("source", nf), new XAttribute("target", selfNodeId)));
                }
            }
        }

        // Connect the family to its super component (if it exists)
        if (!string.IsNullOrEmpty(superComponent))
        {
            // Add the super component node if it doesn't exist
            if (!CheckXmlElement("node", "id", superComponent))
            {
                _graphElement.Add(new XElement("node", new XAttribute("id", superComponent)));
            }

            // Create the edge between the family and its super component
            string edgeId = selfNodeId + "_to_" + superComponent;
            if (!CheckXmlElement("edge", "id", edgeId))
            {
                _graphElement.Add(new XElement("edge", new XAttribute("id", edgeId), new XAttribute("source", selfNodeId), new XAttribute("target", superComponent)));
            }
        }
    }

    private string FormatNestedFamilies(List<string> nestedFamilies)
    {
        // Format the list as a JSON-like array string
        return "[" + string.Join(", ", nestedFamilies.Select(nf => $"'{nf}'")) + "]";
    }

    public bool CheckXmlElement(string elementName, string propertyName, string propertyValue)
    {
        return _root.Descendants(elementName).Any(elem => (string)elem.Attribute(propertyName) == propertyValue);
    }

    public bool CheckNestedXmlElement(string parentElement, string parentProperty, string parentValue, string childElement, string childProperty, string childValue)
    {
        return _root.Descendants(parentElement)
                    .Where(parent => (string)parent.Attribute(parentProperty) == parentValue)
                    .Any(parent => parent.Elements(childElement).Any(child => (string)child.Attribute(childProperty) == childValue));
    }

    public void CreateXmlElementById(string parentElementTag, string parentId, string elementName, Dictionary<string, string> properties, string textValue = null)
    {
        var parentElement = _root.Descendants(parentElementTag)
                                .FirstOrDefault(e => (string)e.Attribute("id") == parentId);

        if (parentElement != null)
        {
            var newElement = new XElement(elementName);
            foreach (var prop in properties)
            {
                newElement.SetAttributeValue(prop.Key, prop.Value);
            }
            if (!string.IsNullOrEmpty(textValue))
            {
                newElement.Value = textValue;
            }
            parentElement.Add(newElement);
        }
    }

    public string GetXmlString()
    {
        return _xmlDoc.ToString();
    }

    public void SaveXml()
    {
        _xmlDoc.Save(_mapPath);
    }
}