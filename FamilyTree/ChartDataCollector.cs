using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace FamilyTree
    {
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ChartDataCollector : IExternalCommand
        {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
            System.Diagnostics.Debugger.Launch();
            var uiApp = commandData.Application;
            var uiDoc = uiApp.ActiveUIDocument;
            var doc = uiDoc.Document;
            
            var categories = new List<BuiltInCategory>(){
            BuiltInCategory.OST_Furniture,
            BuiltInCategory.OST_FurnitureSystems,
            BuiltInCategory.OST_GenericModel,
            BuiltInCategory.OST_LightingFixtures,
            BuiltInCategory.OST_Casework};

            var multiCategoryFilter = new ElementMulticategoryFilter(categories);
            var allFamilySymbols = new FilteredElementCollector(doc)
            .WherePasses(multiCategoryFilter) // Apply your multi-category filter
            .OfClass(typeof(FamilySymbol)) // Collect only FamilySymbols
            .Cast<FamilySymbol>()
            .OrderBy(fs => fs.Family.Id.IntegerValue) // Order by Family Id
            .ToList();

            //var names = string.Join("\n", allFamilySymbols.Select(a=>a.Name).ToList());
            //TaskDialog.Show("Selection Made", names);

            
            var familyDataCollection = new List<Dictionary<string, Object> >();
            

            foreach (var familySymbol in allFamilySymbols)
            { 
                var familySymbolData = new Dictionary<string, object>(); 

                var FamInst = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(fi => fi.Symbol.Id == familySymbol.Id)
                .ToList().FirstOrDefault();
                if (FamInst == null)
                    continue;              
               


                var dependentElementIds = familySymbol.GetDependentElements(new ElementClassFilter(typeof(FamilyInstance)));
                // Convert IDs to names (if elements exist)
                var dependentElementNames = dependentElementIds
                .Select(id => doc.GetElement(id) as FamilyInstance)
                .Where(instance => instance != null && instance.Name != familySymbol.Name) // Exclude the parent element
                .Select(instance => instance.Name) // Cast to object
                .ToList();


                Family family = familySymbol.Family;
                int value = family.get_Parameter(BuiltInParameter.FAMILY_SHARED).AsInteger();
                bool isShared = false;
                if (value > 0)
                    isShared = true;
                else
                    isShared = false;


                bool parentFamilyValues = false; // Use List<object>
    
                Parameter parentFamilyParam = familySymbol.LookupParameter("Parent Family");

                if (parentFamilyParam != null && parentFamilyParam.HasValue)
                {
                    var paramValue = parentFamilyParam.AsInteger(); // Get the parameter value as a string
                    if (paramValue == 1)
                        parentFamilyValues = true;
                }              

                familySymbolData.Add("TypeName", familySymbol.Name.ToString());
                familySymbolData.Add("Category", familySymbol.Category.Name.ToString());
                familySymbolData.Add("NestedFamilies", dependentElementNames); // Already cast to List<object>
                familySymbolData.Add("ParentFamily", parentFamilyValues); 
                familySymbolData.Add("IsShared", isShared); 
                
                if (FamInst != null && FamInst.SuperComponent != null)
                {
                    var superComponent = FamInst.SuperComponent;
                    familySymbolData.Add("SuperComponent", superComponent.Name);
                }

                familyDataCollection.Add(familySymbolData);
            }


            string mapPath = @"C:\Users\User\Documents\ARAVIND";
            var filePath = System.IO.Path.Combine(mapPath, "NewXmlDocument.xml");
            
            FamilyMapManager familyMap = new FamilyMapManager(filePath);
           
            foreach (var familyData in familyDataCollection) 
                {
                familyMap.ProcessFamilyData(familyData);
                }
            string xmlString = familyMap.GetXmlString();
            familyMap.SaveXml();
            var count = familyDataCollection.Count;
            TaskDialog.Show("Success", count.ToString());

            return Result.Succeeded;
            }    
        }
    }