//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Xml.Linq;

//public class FamilyMapManager2
//{
//    private string _mapPath;
//    private XDocument _xmlDoc;
//    private XElement _root;
//    private XElement _graphElement;

//    public FamilyMapManager(string scriptPath, string mapFilename = "FamilyMap.graphml")
//    {
//        _mapPath = Path.Combine(scriptPath, mapFilename);
//        LoadOrCreateFamilyMap();
//    }

//    private void LoadOrCreateFamilyMap()
//    {
//        if (File.Exists(_mapPath))
//        {
//            _xmlDoc = XDocument.Load(_mapPath);
//            _root = _xmlDoc.Root;
//        }
//        else
//        {
//            _root = new XElement("graphml");
//            _graphElement = new XElement("graph", new XAttribute("edgedefault", "undirected"));
//            _root.Add(_graphElement);
//            _xmlDoc = new XDocument(_root);

//            var keys = new List<XElement>
//            {
//                new XElement("key", new XAttribute("id", "Type Name"), new XAttribute("for", "node"), new XAttribute("attr.name", "Type Name"), new XAttribute("attr.type", "string")),
//                new XElement("key", new XAttribute("id", "FamilyName"), new XAttribute("for", "node"), new XAttribute("attr.name", "FamilyName"), new XAttribute("attr.type", "string")),
//                new XElement("key", new XAttribute("id", "IsShared"), new XAttribute("for", "node"), new XAttribute("attr.name", "IsShared"), new XAttribute("attr.type", "boolean")),
//                new XElement("key", new XAttribute("id", "Category"), new XAttribute("for", "node"), new XAttribute("attr.name", "Category"), new XAttribute("attr.type", "string")),
//                new XElement("key", new XAttribute("id", "ParentFamily"), new XAttribute("for", "node"), new XAttribute("attr.name", "ParentFamily"), new XAttribute("attr.type", "boolean"))
//            };
            
//            _root.Add(keys);
//        }
//        _graphElement = _root.Element("graph") ?? new XElement("graph", new XAttribute("edgedefault", "undirected"));
//        _root.Add(_graphElement);
//    }

//    public bool CheckXmlElement(string xpath, string propertyName, string propertyValue)
//    {
//        return _root.Descendants(xpath).Any(elem => (string)elem.Attribute(propertyName) == propertyValue);
//    }

//    public bool CheckNestedXmlElement(string parentXPath, string parentProperty, string parentValue, string childXPath, string childProperty, string childValue)
//    {
//        return _root.Descendants(parentXPath)
//                    .Where(parent => (string)parent.Attribute(parentProperty) == parentValue)
//                    .Any(parent => parent.Descendants(childXPath).Any(child => (string)child.Attribute(childProperty) == childValue));
//    }

//    public void CreateXmlElement(string xpath, string elementName, string propertyName, string propertyValue)
//    {
//        var parent = _root.Descendants(xpath).FirstOrDefault();
//        if (parent == null)
//        {
//            parent = new XElement(xpath);
//            _root.Add(parent);
//        }
//        var newElement = new XElement(elementName, new XAttribute(propertyName, propertyValue));
//        parent.Add(newElement);
//    }

//    public void CreateXmlElementById(string parentElementTag, string parentId, string elementName, Dictionary<string, string> properties, string textValue = null)
//    {
//        var parentElement = _root.Descendants(parentElementTag)
//                                .FirstOrDefault(e => (string)e.Attribute("id") == parentId);

//        if (parentElement != null)
//        {
//            var newElement = new XElement(elementName);
//            foreach (var prop in properties)
//            {
//                newElement.SetAttributeValue(prop.Key, prop.Value);
//            }
//            if (!string.IsNullOrEmpty(textValue))
//            {
//                newElement.Value = textValue;
//            }
//            parentElement.Add(newElement);
//        }
//    }

//    public void AddPropertiesToElementById(string elementId, string idAttribute, Dictionary<string, string> properties)
//    {
//        var element = _root.Descendants().FirstOrDefault(e => (string)e.Attribute(idAttribute) == elementId);
//        if (element != null)
//        {
//            foreach (var prop in properties)
//            {
//                element.SetAttributeValue(prop.Key, prop.Value);
//            }
//        }
//    }

//    public void ProcessFamilyData(Dictionary<string, object> familyData)
//    {
//        List<string> nestedFamilies = (List<string>)familyData["NestedFamilies"];
//        string selfNodeId = (string)familyData["FamilyName"];

//        // Create nested family nodes if they don't exist
//        foreach (var nf in nestedFamilies)
//        {
//            if (!CheckXmlElement("node", "id", nf))
//            {
//                _graphElement.Add(new XElement("node", new XAttribute("id", nf)));
//            }
//        }

//        // Create or update current family node
//        if (!CheckXmlElement("node", "id", selfNodeId))
//        {
//            var newNode = new XElement("node", new XAttribute("id", selfNodeId));
//            _graphElement.Add(newNode);
//            foreach (var k in familyData.Keys)
//            {
//                CreateXmlElementById("node", selfNodeId, "data", new Dictionary<string, string> { { "key", k } }, familyData[k].ToString());
//            }
//        }
//        else
//        {
//            foreach (var k in familyData.Keys)
//            {
//                bool nodeDataExists = CheckNestedXmlElement("graphml/graph/node", "id", selfNodeId, "data", "key", k);
//                if (!nodeDataExists)
//                {
//                    CreateXmlElementById("node", selfNodeId, "data", new Dictionary<string, string> { { "key", k } }, familyData[k].ToString());
//                }
//            }
//        }

//        // Create edges between nodes
//        foreach (var nf in nestedFamilies)
//        {
//            string edgeId = nf + "_to_" + selfNodeId;
//            _graphElement.Add(new XElement("edge", new XAttribute("id", edgeId), new XAttribute("source", nf), new XAttribute("target", selfNodeId)));
//        }
//    }

//    public string GetXmlString()
//    {
//        return _xmlDoc.ToString();
//    }

//    public void SaveXml()
//    {
//        _xmlDoc.Save(_mapPath);
//    }
//}
