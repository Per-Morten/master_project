using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using Int64 = System.Int64;

namespace IfcEngine
{
    class x64
    {
        public const Int64 flagbit0 = 1;           // 2^^0    0000.0000..0000.0001
        public const Int64 flagbit1 = 2;           // 2^^1    0000.0000..0000.0010
        public const Int64 flagbit2 = 4;           // 2^^2    0000.0000..0000.0100
        public const Int64 flagbit3 = 8;           // 2^^3    0000.0000..0000.1000
        public const Int64 flagbit4 = 16;          // 2^^4    0000.0000..0001.0000
        public const Int64 flagbit5 = 32;          // 2^^5    0000.0000..0010.0000
        public const Int64 flagbit6 = 64;          // 2^^6    0000.0000..0100.0000
        public const Int64 flagbit7 = 128;         // 2^^7    0000.0000..1000.0000
        public const Int64 flagbit8 = 256;         // 2^^8    0000.0001..0000.0000
        public const Int64 flagbit9 = 512;         // 2^^9    0000.0010..0000.0000
        public const Int64 flagbit10 = 1024;       // 2^^10   0000.0100..0000.0000
        public const Int64 flagbit11 = 2048;       // 2^^11   0000.1000..0000.0000
        public const Int64 flagbit12 = 4096;       // 2^^12   0001.0000..0000.0000
        public const Int64 flagbit13 = 8192;       // 2^^13   0010.0000..0000.0000
        public const Int64 flagbit14 = 16384;      // 2^^14   0100.0000..0000.0000
        public const Int64 flagbit15 = 32768;      // 2^^15   1000.0000..0000.0000
        public const Int64 flagbit24 = 16777216;
        public const Int64 flagbit25 = 33554432;
        public const Int64 flagbit26 = 67108864;
        public const Int64 flagbit27 = 134217728;

        public const Int64 sdaiADB = 1;
        public const Int64 sdaiAGGR = sdaiADB + 1;
        public const Int64 sdaiBINARY = sdaiAGGR + 1;
        public const Int64 sdaiBOOLEAN = sdaiBINARY + 1;
        public const Int64 sdaiENUM = sdaiBOOLEAN + 1;
        public const Int64 sdaiINSTANCE = sdaiENUM + 1;
        public const Int64 sdaiINTEGER = sdaiINSTANCE + 1;
        public const Int64 sdaiLOGICAL = sdaiINTEGER + 1;
        public const Int64 sdaiREAL = sdaiLOGICAL + 1;
        public const Int64 sdaiSTRING = sdaiREAL + 1;
        public const Int64 sdaiUNICODE = sdaiSTRING + 1;
        public const Int64 sdaiEXPRESSSTRING = sdaiUNICODE + 1;
        public const Int64 engiGLOBALID = sdaiEXPRESSSTRING + 1;

        public const string IFCEngineDLL = @"ifcengine.dll";


        //
        //  Calls for File IO 
        //


        [DllImport(IFCEngineDLL, EntryPoint = "sdaiCloseModel")]
        public static extern void sdaiCloseModel(Int64 model);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiCreateModelBN")]
        public static extern Int64 sdaiCreateModelBN(Int64 repository, string fileName, string schemaName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiCreateModelBN")]
        public static extern Int64 sdaiCreateModelBN(Int64 repository, byte[] fileName, byte[] schemaName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiCreateModelBNUnicode")]
        public static extern Int64 sdaiCreateModelBNUnicode(Int64 repository, byte[] fileName, byte[] schemaName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiOpenModelBN")]
        public static extern Int64 sdaiOpenModelBN(Int64 repository, string fileName, string schemaName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiOpenModelBN")]
        public static extern Int64 sdaiOpenModelBN(Int64 repository, byte[] fileName, byte[] schemaName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiOpenModelBNUnicode")]
        public static extern Int64 sdaiOpenModelBNUnicode(Int64 repository, byte[] fileName, byte[] schemaName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiSaveModelBN")]
        public static extern void sdaiSaveModelBN(Int64 model, string fileName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiSaveModelBN")]
        public static extern void sdaiSaveModelBN(Int64 model, byte[] fileName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiSaveModelBNUnicode")]
        public static extern void sdaiSaveModelBNUnicode(Int64 model, byte[] fileName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiSaveModelAsXmlBN")]
        public static extern void sdaiSaveModelAsXmlBN(Int64 model, string fileName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiSaveModelAsXmlBN")]
        public static extern void sdaiSaveModelAsXmlBN(Int64 model, byte[] fileName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiSaveModelAsXmlBNUnicode")]
        public static extern void sdaiSaveModelAsXmlBNUnicode(Int64 model, byte[] fileName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiSaveModelAsSimpleXmlBN")]
        public static extern void sdaiSaveModelAsSimpleXmlBN(Int64 model, string fileName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiSaveModelAsSimpleXmlBN")]
        public static extern void sdaiSaveModelAsSimpleXmlBN(Int64 model, byte[] fileName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiSaveModelAsSimpleXmlBNUnicode")]
        public static extern void sdaiSaveModelAsSimpleXmlBNUnicode(Int64 model, byte[] fileName);


        //
        //  Schema Reading
        //


        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetEntity")]
        public static extern Int64 sdaiGetEntity(Int64 model, string entityName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetEntity")]
        public static extern Int64 sdaiGetEntity(Int64 model, byte[] entityName);

        [DllImport(IFCEngineDLL, EntryPoint = "engiGetEntityArgumentName")]
        public static extern void engiGetEntityArgumentName(Int64 entity, Int64 index, Int64 valueType, out IntPtr argumentName);

        [DllImport(IFCEngineDLL, EntryPoint = "engiGetEntityArgumentType")]
        public static extern void engiGetEntityArgumentType(Int64 entity, Int64 index, ref Int64 argumentType);

        [DllImport(IFCEngineDLL, EntryPoint = "engiGetEntityCount")]
        public static extern Int64 engiGetEntityCount(Int64 model);

        [DllImport(IFCEngineDLL, EntryPoint = "engiGetEntityElement")]
        public static extern Int64 engiGetEntityElement(Int64 model, Int64 index);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetEntityExtent")]
        public static extern Int64 sdaiGetEntityExtent(Int64 model, Int64 entity);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetEntityExtentBN")]
        public static extern Int64 sdaiGetEntityExtentBN(Int64 model, string entityName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetEntityExtentBN")]
        public static extern Int64 sdaiGetEntityExtentBN(Int64 model, byte[] entityName);

        [DllImport(IFCEngineDLL, EntryPoint = "engiGetEntityName")]
        public static extern void engiGetEntityName(Int64 entity, Int64 valueType, out IntPtr entityName);

        [DllImport(IFCEngineDLL, EntryPoint = "engiGetEntityNoArguments")]
        public static extern Int64 engiGetEntityNoArguments(Int64 entity);

        [DllImport(IFCEngineDLL, EntryPoint = "engiGetEntityParent")]
        public static extern Int64 engiGetEntityParent(Int64 entity);


        //
        //  Instance Header
        //


        [DllImport(IFCEngineDLL, EntryPoint = "GetSPFFHeaderItem")]
        public static extern Int64 GetSPFFHeaderItem(Int64 model, Int64 itemIndex, Int64 itemSubIndex, Int64 valueType, out IntPtr value);

        [DllImport(IFCEngineDLL, EntryPoint = "SetSPFFHeader")]
        public static extern void SetSPFFHeader(Int64 model, string description, string implementationLevel, string name, string timeStamp, string author, string organization, string preprocessorVersion, string originatingSystem, string authorization, string fileSchema);

        [DllImport(IFCEngineDLL, EntryPoint = "SetSPFFHeader")]
        public static extern void SetSPFFHeader(Int64 model, byte[] description, byte[] implementationLevel, byte[] name, byte[] timeStamp, byte[] author, byte[] organization, byte[] preprocessorVersion, byte[] originatingSystem, byte[] authorization, byte[] fileSchema);

        [DllImport(IFCEngineDLL, EntryPoint = "SetSPFFHeaderItem")]
        public static extern Int64 SetSPFFHeaderItem(Int64 model, Int64 itemIndex, Int64 itemSubIndex, Int64 valueType, string value);

        [DllImport(IFCEngineDLL, EntryPoint = "SetSPFFHeaderItem")]
        public static extern Int64 SetSPFFHeaderItem(Int64 model, Int64 itemIndex, Int64 itemSubIndex, Int64 valueType, byte[] value);


        //
        //  Instance Reading
        //


        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetADBType")]
        public static extern Int64 sdaiGetADBType(Int64 ADB);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetADBTypePathx")]
        public static extern void sdaiGetADBTypePath(Int64 ADB, Int64 typeNameNumber, out IntPtr path);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetADBValue")]
        public static extern void sdaiGetADBValue(Int64 ADB, Int64 valueType, out Int64 value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetADBValue")]
        public static extern void sdaiGetADBValue(Int64 ADB, Int64 valueType, out double value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetADBValue")]
        public static extern void sdaiGetADBValue(Int64 ADB, Int64 valueType, out IntPtr value);

        [DllImport(IFCEngineDLL, EntryPoint = "engiGetAggrElement")]
        public static extern Int64 engiGetAggrElement(Int64 aggregate, Int64 elementIndex, Int64 valueType, out Int64 value);

        [DllImport(IFCEngineDLL, EntryPoint = "engiGetAggrElement")]
        public static extern Int64 engiGetAggrElement(Int64 aggregate, Int64 elementIndex, Int64 valueType, out double value);

        [DllImport(IFCEngineDLL, EntryPoint = "engiGetAggrElement")]
        public static extern Int64 engiGetAggrElement(Int64 aggregate, Int64 elementIndex, Int64 valueType, out IntPtr value);

        [DllImport(IFCEngineDLL, EntryPoint = "engiGetAggrType")]
        public static extern void engiGetAggrType(Int64 aggregate, ref Int64 aggragateType);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetAttr")]
        public static extern Int64 sdaiGetAttr(Int64 instance, Int64 attribute, Int64 valueType, out Int64 value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetAttr")]
        public static extern Int64 sdaiGetAttr(Int64 instance, Int64 attribute, Int64 valueType, out double value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetAttr")]
        public static extern Int64 sdaiGetAttr(Int64 instance, Int64 attribute, Int64 valueType, out IntPtr value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetAttrBN")]
        public static extern Int64 sdaiGetAttrBN(Int64 instance, string attributeName, Int64 valueType, out Int64 value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetAttrBN")]
        public static extern Int64 sdaiGetAttrBN(Int64 instance, string attributeName, Int64 valueType, out double value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetAttrBN")]
        public static extern Int64 sdaiGetAttrBN(Int64 instance, string attributeName, Int64 valueType, out IntPtr value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetAttrBN")]
        public static extern Int64 sdaiGetAttrBN(Int64 instance, byte[] attributeName, Int64 valueType, out Int64 value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetAttrBN")]
        public static extern Int64 sdaiGetAttrBN(Int64 instance, byte[] attributeName, Int64 valueType, out double value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetAttrBN")]
        public static extern Int64 sdaiGetAttrBN(Int64 instance, byte[] attributeName, Int64 valueType, out IntPtr value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetAttrDefinition")]
        public static extern Int64 sdaiGetAttrDefinition(Int64 entity, string attributeName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetAttrDefinition")]
        public static extern Int64 sdaiGetAttrDefinition(Int64 entity, byte[] attributeName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetInstanceType")]
        public static extern Int64 sdaiGetInstanceType(Int64 instance);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiGetMemberCount")]
        public static extern Int64 sdaiGetMemberCount(Int64 aggregate);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiIsKindOf")]
        public static extern Int64 sdaiIsKindOf(Int64 instance, Int64 entity);


        //
        //  Instance Writing
        //


        [DllImport(IFCEngineDLL, EntryPoint = "sdaiAppend")]
        public static extern void sdaiAppend(Int64 list, Int64 valueType, ref Int64 value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiAppend")]
        public static extern void sdaiAppend(Int64 list, Int64 valueType, ref double value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiAppend")]
        public static extern void sdaiAppend(Int64 list, Int64 valueType, string value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiAppend")]
        public static extern void sdaiAppend(Int64 list, Int64 valueType, byte[] value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiCreateADB")]
        public static extern Int64 sdaiCreateADB(Int64 valueType, ref Int64 value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiCreateADB")]
        public static extern Int64 sdaiCreateADB(Int64 valueType, ref double value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiCreateADB")]
        public static extern Int64 sdaiCreateADB(Int64 valueType, string value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiCreateADB")]
        public static extern Int64 sdaiCreateADB(Int64 valueType, byte[] value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiCreateAggr")]
        public static extern Int64 sdaiCreateAggr(Int64 instance, Int64 attribute);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiCreateAggrBN")]
        public static extern Int64 sdaiCreateAggrBN(Int64 instance, string attributeName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiCreateAggrBN")]
        public static extern Int64 sdaiCreateAggrBN(Int64 instance, byte[] attributeName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiCreateInstance")]
        public static extern Int64 sdaiCreateInstance(Int64 model, Int64 entity);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiCreateInstanceBN")]
        public static extern Int64 sdaiCreateInstanceBN(Int64 model, string entityName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiCreateInstanceBN")]
        public static extern Int64 sdaiCreateInstanceBN(Int64 model, byte[] entityName);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiDeleteInstance")]
        public static extern void sdaiDeleteInstance(Int64 instance);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiPutADBTypePath")]
        public static extern void sdaiPutADBTypePath(Int64 ADB, Int64 pathCount, string path);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiPutADBTypePath")]
        public static extern void sdaiPutADBTypePath(Int64 ADB, Int64 pathCount, byte[] path);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiPutAttr")]
        public static extern void sdaiPutAttr(Int64 instance, Int64 attribute, Int64 valueType, ref Int64 value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiPutAttr")]
        public static extern void sdaiPutAttr(Int64 instance, Int64 attribute, Int64 valueType, ref double value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiPutAttr")]
        public static extern void sdaiPutAttr(Int64 instance, Int64 attribute, Int64 valueType, string value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiPutAttr")]
        public static extern void sdaiPutAttr(Int64 instance, Int64 attribute, Int64 valueType, byte[] value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiPutAttrBN")]
        public static extern void sdaiPutAttrBN(Int64 instance, string attributeName, Int64 valueType, ref Int64 value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiPutAttrBN")]
        public static extern void sdaiPutAttrBN(Int64 instance, string attributeName, Int64 valueType, ref double value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiPutAttrBN")]
        public static extern void sdaiPutAttrBN(Int64 instance, string attributeName, Int64 valueType, string value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiPutAttrBN")]
        public static extern void sdaiPutAttrBN(Int64 instance, string attributeName, Int64 valueType, byte[] value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiPutAttrBN")]
        public static extern void sdaiPutAttrBN(Int64 instance, byte[] attributeName, Int64 valueType, ref Int64 value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiPutAttrBN")]
        public static extern void sdaiPutAttrBN(Int64 instance, byte[] attributeName, Int64 valueType, ref double value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiPutAttrBN")]
        public static extern void sdaiPutAttrBN(Int64 instance, byte[] attributeName, Int64 valueType, string value);

        [DllImport(IFCEngineDLL, EntryPoint = "sdaiPutAttrBN")]
        public static extern void sdaiPutAttrBN(Int64 instance, byte[] attributeName, Int64 valueType, byte[] value);

        [DllImport(IFCEngineDLL, EntryPoint = "engiSetComment")]
        public static extern void engiSetComment(Int64 instance, string comment);

        [DllImport(IFCEngineDLL, EntryPoint = "engiSetComment")]
        public static extern void engiSetComment(Int64 instance, byte[] comment);


        //
        //  Controling Calls
        //


        [DllImport(IFCEngineDLL, EntryPoint = "circleSegments")]
        public static extern void circleSegments(Int64 circles, Int64 smallCircles);

        [DllImport(IFCEngineDLL, EntryPoint = "cleanMemory")]
        public static extern void cleanMemory(Int64 model, Int64 mode);

        [DllImport(IFCEngineDLL, EntryPoint = "internalGetP21Line")]
        public static extern Int64 internalGetP21Line(Int64 instance);

        [DllImport(IFCEngineDLL, EntryPoint = "internalGetInstanceFromP21Line")]
        public static extern Int64 internalGetInstanceFromP21Line(Int64 model, Int64 P21Line);

        [DllImport(IFCEngineDLL, EntryPoint = "setStringUnicode")]
        public static extern Int64 setStringUnicode(Int64 unicode);


        //
        //  Geometry Interaction
        //


        [DllImport(IFCEngineDLL, EntryPoint = "initializeModellingInstance")]
        public static extern Int64 initializeModellingInstance(Int64 model, ref Int64 noVertices, ref Int64 noIndices, double scale, Int64 instance);

        [DllImport(IFCEngineDLL, EntryPoint = "finalizeModelling")]
        public static extern Int64 finalizeModelling(Int64 model, float[] vertices, int[] indices, Int64 FVF);

        [DllImport(IFCEngineDLL, EntryPoint = "finalizeModelling")]
        public static extern Int64 finalizeModelling(Int64 model, double[] vertices, Int64[] indices, Int64 FVF);

        [DllImport(IFCEngineDLL, EntryPoint = "getInstanceInModelling")]
        public static extern Int64 getInstanceInModelling(Int64 model, Int64 instance, Int64 mode, ref Int64 startVertex, ref Int64 startIndex, ref Int64 primitiveCount);

        [DllImport(IFCEngineDLL, EntryPoint = "setVertexOffset")]
        public static extern void setVertexOffset(Int64 model, double x, double y, double z);

        [DllImport(IFCEngineDLL, EntryPoint = "setFilter")]
        public static extern void setFilter(Int64 model, Int64 setting, Int64 mask);

        [DllImport(IFCEngineDLL, EntryPoint = "setFormat")]
        public static extern void setFormat(Int64 model, Int64 setting, Int64 mask);

        [DllImport(IFCEngineDLL, EntryPoint = "SetFormat")]
        public static extern Int64 SetFormat(Int64 model, Int64 setting, Int64 mask);
        
        [DllImport(IFCEngineDLL, EntryPoint = "getConceptualFaceCnt")]
        public static extern Int64 getConceptualFaceCnt(Int64 instance);

        [DllImport(IFCEngineDLL, EntryPoint = "getConceptualFaceEx")]
        public static extern Int64 getConceptualFaceEx(Int64 instance, Int64 index, ref Int64 startIndexTriangles, ref Int64 noIndicesTriangles, ref Int64 startIndexLines, ref Int64 noIndicesLines, ref Int64 startIndexPoints, ref Int64 noIndicesPoints, ref Int64 startIndexFacesPolygons, ref Int64 noIndicesFacesPolygons, ref Int64 startIndexConceptualFacePolygons, ref Int64 noIndicesConceptualFacePolygons);

        [DllImport(IFCEngineDLL, EntryPoint = "CalculateInstance")]
        public static extern Int64 CalculateInstance(Int64 owlInstance, ref Int64 vertexBufferSize, ref Int64 indexBufferSize, ref Int64 transformationBufferSize);

        [DllImport(IFCEngineDLL, EntryPoint = "UpdateInstanceVertexBuffer")]
        public static extern Int64 UpdateInstanceVertexBuffer(Int64 owlInstance, float[] vertexBuffer);

        [DllImport(IFCEngineDLL, EntryPoint = "UpdateInstanceIndexBuffer")]
        public static extern Int64 UpdateInstanceIndexBuffer(Int64 owlInstance, int[] indexBuffer);

        [DllImport(IFCEngineDLL, EntryPoint = "GetInstanceClass")]
        public static extern Int64 GetInstanceClass(Int64 owlInstance);

        [DllImport(IFCEngineDLL, EntryPoint = "GetClassByName")]
        public static extern Int64 GetClassByName(Int64 model, string owlClassName);

        [DllImport(IFCEngineDLL, EntryPoint = "GetPropertyByName")]
        public static extern Int64 GetPropertyByName(Int64 model, string rdfPropertyName);

        [DllImport(IFCEngineDLL, EntryPoint = "GetObjectTypeProperty")]
        public static extern Int64 GetObjectTypeProperty(Int64 owlInstance, Int64 rdfProperty, out IntPtr value, ref Int64 card);

        [DllImport(IFCEngineDLL, EntryPoint = "GetDataTypeProperty")]
        public static extern Int64 GetDataTypeProperty(Int64 owlInstance, Int64 rdfProperty, out IntPtr value, ref Int64 card);

        [DllImport(IFCEngineDLL, EntryPoint = "owlGetModel")]
        public static extern void owlGetModel(Int64 model, ref Int64 owlModel);

        [DllImport(IFCEngineDLL, EntryPoint = "owlGetInstance")]
        public static extern void owlGetInstance(Int64 model, Int64 instance, ref Int64 owlInstance);

        [DllImport(IFCEngineDLL, EntryPoint = "SetDefaultColor")]
        public static extern void SetDefaultColor(Int64 model, UInt32 ambient, UInt32 diffuse, UInt32 emissive, UInt32 specular);
    }
}
