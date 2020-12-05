using FBRepacker.PAC;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;

using OpenTK;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using SFGraphics.Utils;
using System.Security.Policy;
using SharpGLTF.Schema2;

namespace FBRepacker.NUD
{
    // very barebone
    class ModelConverter : Internals
    {
        public ModelConverter() : base()
        {

        }

        XNamespace ns;

        private static readonly Dictionary<Type, string> Aliases = new Dictionary<Type, string>()
        {
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(object), "object" },
            { typeof(bool), "bool" },
            { typeof(char), "char" },
            { typeof(string), "string" },
            { typeof(void), "void" },
            // Controller specific Aliases
            { typeof(Matrix4), "float4x4" }
        };

        public enum vertexColorTypes
        {
            NoVertexColor = 0x00,
            Byte = 0x02,
            HalfFloat = 0x04
        }

        public enum BoneTypes
        {
            NoBones = 0x00,
            Float = 0x01,
            HalfFloat = 0x02,
            Byte = 0x04
        }

        public enum vertexNormalTypes
        {
            NoNormals = 0x0,
            NormalsFloat = 0x1,
            OnlyNormalsFloat = 0x2, // ?? 
            NormalsTanBiTanFloat = 0x3,
            NormalsHalfFloat = 0x6,
            NormalsTanBiTanHalfFloat = 0x7
        }

        #region NUDtoDAE
        public void fromNUDtoDAE()
        {
            // parseNUD and write DAE.
            FileStream NUD = File.OpenRead(Properties.Settings.Default.NUDPathNUDtoDAE);
            FileStream VBN = File.OpenRead(Properties.Settings.Default.VBNPathNUDtoDAE);

            string NUDFileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.NUDPathNUDtoDAE);
            string DAEPath = Path.Combine(Properties.Settings.Default.OutputPathNUDtoDAE, NUDFileName + " Converted.dae");
            //TODO: Create a new DAE instead of modifying existing ones.
            string DAEInputPath = Path.Combine(Path.GetDirectoryName(Properties.Settings.Default.NUDPathNUDtoDAE), NUDFileName + ".dae");
            XElement DAE = XElement.Load(DAEInputPath);
            ns = DAE.GetDefaultNamespace();

            XElement[] geometry = DAE.Element(ns + "library_geometries").Elements(ns + "geometry").ToArray();
            XElement[] controller = DAE.Element(ns + "library_controllers").Elements(ns + "controller").ToArray();

            parseNUDMetadata(NUD, out int datasetCount, out int metaDataChunkSize, out int triangleDataChunkSize, out int vertexColorandUVDataChunkSize, out int vertexDataChunkSize);

            int trianglesDataEntryOffset = metaDataChunkSize + 0x30;
            int vertexColorandUVDataEntryOffset = trianglesDataEntryOffset + triangleDataChunkSize;
            int vertexDataEntryOffset = vertexColorandUVDataEntryOffset + vertexColorandUVDataChunkSize;
            int geometryShapeName = vertexDataEntryOffset + vertexDataChunkSize;

            // TODO: add iteration for more than 1 polySetCount.
            parsePolysetMetadata(NUD, out int boneFlag, out int nonRiggedBoneIndex, out ushort polySetCount, out int polyMetaDataOffset);

            if (geometry.Length != (int)polySetCount)
                throw new Exception("polyset Count != DAE Geometry Count");

            if (controller.Length != (int)polySetCount)
                throw new Exception("polyset Count != DAE Controller Count");

            for (int i = 0; i < polySetCount; i++)
            {
                List<float[]> UVList = new List<float[]>();
                List<float[]> vertexColorList = new List<float[]>();
                List<float[]> verticePositionList = new List<float[]>();
                List<float[]> verticeNormalList = new List<float[]>();
                List<Dictionary<int, float>> boneIndicesandWeightsList = new List<Dictionary<int, float>>();

                changeStreamFile(NUD);

                NUD.Seek(polyMetaDataOffset + (0x30 * i), SeekOrigin.Begin); // Seek to the start of poly metadata
                parsePolyMetadata(NUD, out int tiranglesDataOffset, out int vertexColorandUVDataOffset, out int vertexDataOffset, out ushort vertexCount, 
                    out byte boneTypeandVertexType, out byte UVSizeandVertexColorType, out int[] materialDataOffset, out ushort polyCount);

                NUD.Seek(trianglesDataEntryOffset, SeekOrigin.Begin);
                readTrianglesData(NUD, polyCount, tiranglesDataOffset, out List<ushort> primitiveIndiceValueList);

                readVertexData(NUD, boneTypeandVertexType, UVSizeandVertexColorType, vertexCount,
                    vertexColorandUVDataEntryOffset, vertexDataEntryOffset,
                    vertexColorandUVDataOffset, vertexDataOffset,
                    nonRiggedBoneIndex,
                    out vertexColorList, out UVList,
                    out verticePositionList, out verticeNormalList, 
                    out boneIndicesandWeightsList);

                parseVBN(VBN, out Dictionary<string, int> boneNamesandParentIndexDic, out List<Matrix4> boneTransformMatList);

                writeDAE(DAE, geometry[i], controller[i], DAEPath, primitiveIndiceValueList, vertexColorList, UVList, 
                    verticePositionList, verticeNormalList, boneIndicesandWeightsList, boneNamesandParentIndexDic, boneTransformMatList);
            }

            NUD.Close();
            VBN.Close();
        }

        private void readVertexData(FileStream NUD, byte boneTypeandVertexType, byte UVSizeandvertexColorType, int vertexCount,
            int vertexColorandUVDataEntryOffset, int vertexDataEntryOffset,
            int vertexColorandUVDataOffset, int vertexDataOffset,
            int nonRiggedBoneIndex,
            out List<float[]> vertexColorList, out List<float[]> UVList,
            out List<float[]> verticePositionList, out List<float[]> verticeNormalList,
            out List<Dictionary<int, float>> boneIndicesandWeightsList)
        {
            int boneType = (boneTypeandVertexType >> 4); // Take the first 4 bits
            int vertexNormalType = (boneTypeandVertexType & 0x0F); // Take the last 4 bits

            // e.g. 0x12, 1 = UVCount, 2 = RGBAByte type.
            int UVCount = (UVSizeandvertexColorType >> 4); // Get the first 4 bit (half byte).
            int vertexColorType = (UVSizeandvertexColorType & 0x0F); // Get the last 4 bit (half byte).

            vertexColorList = new List<float[]>();
            UVList = new List<float[]>();
            verticePositionList = new List<float[]>();
            verticeNormalList = new List<float[]>();
            boneIndicesandWeightsList = new List<Dictionary<int, float>>();

            if (boneType != (int)BoneTypes.NoBones)
            {
                NUD.Seek(vertexColorandUVDataEntryOffset, SeekOrigin.Begin);
                NUD.Seek(vertexColorandUVDataOffset, SeekOrigin.Current);
                for (int i = 0; i < vertexCount; i++)
                {
                    readVertexColorandUVData(NUD, vertexColorType, UVCount, out float[] vertexColor, out float[] UV);
                    vertexColorList.Add(vertexColor);
                    UVList.Add(UV);
                }

                NUD.Seek(vertexDataEntryOffset, SeekOrigin.Begin);
                NUD.Seek(vertexDataOffset, SeekOrigin.Current);
                for (int i = 0; i < vertexCount; i++)
                {
                    readVertexPosNrmTanBitan(NUD, vertexNormalType, out float[] verticePosition, out float[] verticeNormal);
                    readVertexBoneData(NUD, boneType, nonRiggedBoneIndex, out Dictionary<int, float> boneIndicesandWeights);

                    verticePositionList.Add(verticePosition);
                    verticeNormalList.Add(verticeNormal);
                    boneIndicesandWeightsList.Add(boneIndicesandWeights);
                }
            }
            else
            {
                // For no Bone type, we read vertex Data and normals first before reading vertex color and UV. 
                // No second skipping is required, since all data are stored consecutively (only 3 sections exist)
                NUD.Seek(vertexColorandUVDataEntryOffset, SeekOrigin.Begin);
                NUD.Seek(vertexColorandUVDataOffset, SeekOrigin.Current);
                for (int i = 0; i < vertexCount; i++)
                {
                    readVertexPosNrmTanBitan(NUD, vertexNormalType, out float[] verticePosition, out float[] verticeNormal);
                    readVertexColorandUVData(NUD, vertexColorType, UVCount, out float[] vertexColor, out float[] UV);
                    readVertexBoneData(NUD, boneType, nonRiggedBoneIndex, out Dictionary<int, float> boneIndicesandWeights);

                    verticePositionList.Add(verticePosition);
                    verticeNormalList.Add(verticeNormal);
                    vertexColorList.Add(vertexColor);
                    UVList.Add(UV);
                    boneIndicesandWeightsList.Add(boneIndicesandWeights);
                }
            }
                
        }

        private void parseNUDMetadata(FileStream NUD, out int dataSetCount, out int metaDataChunkSize, out int triangleDataChunkSize, out int vertexColorandUVDataChunkSize, out int vertexDataChunkSize)
        {
            changeStreamFile(NUD);
            int header = readIntBigEndian(NUD.Position);
            if (header != 0x4E445033)
                throw new Exception("Selected file is not NDP3 type!");

            int fileSize = readIntBigEndian(NUD.Position); // Not used?
            int fileType = readShort(NUD.Position, false); // Type is always small endian.

            if (fileType != 0x02)
                throw new NotImplementedException("NUD type: " + fileType + " is not supported yet");

            dataSetCount = readShort(NUD.Position, true);

            if (dataSetCount > 1)
                throw new NotImplementedException("NUD file with more than 1 polyset count not supported yet");

            int NUDType = readShort(NUD.Position, true); // Not used?
            int boneCount = readShort(NUD.Position, true); // I have no idea why the bone count is different from VBN bone count. Not used too.

            metaDataChunkSize = readIntBigEndian(NUD.Position);
            triangleDataChunkSize = readIntBigEndian(NUD.Position);
            vertexColorandUVDataChunkSize = readIntBigEndian(NUD.Position);
            vertexDataChunkSize = readIntBigEndian(NUD.Position);

            // TODO: add boundingSphere read
            NUD.Seek(0x30, SeekOrigin.Current);
        }

        private void parsePolysetMetadata(FileStream NUD, out int boneFlag, out int boneIndex, out ushort polySetCount, out int polyDataOffset)
        {
            if (readIntBigEndian(NUD.Position) != 0)
                throw new Exception("Unexpected polysetmetadata: First 4 byte is not 0.");

            if(readShort(NUD.Position, true) != 0)
                throw new Exception("Unexpected polysetmetadata: byte 5 & 6 is not 0.");

            boneFlag = readShort(NUD.Position, true);
            boneIndex = readShort(NUD.Position, true);
            polySetCount = readUShort(NUD.Position, true);
            polyDataOffset = readIntBigEndian(NUD.Position);
        }

        private void parsePolyMetadata(FileStream NUD, out int tiranglesDataOffset, out int vertexColorandUVDataOffset, out int vertexDataOffset, 
            out ushort vertexCount, out byte boneTypeandVertexType, out byte UVSizeandVertexColorType, out int[] materialDataOffset, out ushort polyCount)
        {
            tiranglesDataOffset = readIntBigEndian(NUD.Position);
            vertexColorandUVDataOffset = readIntBigEndian(NUD.Position);
            vertexDataOffset = readIntBigEndian(NUD.Position);

            vertexCount = readUShort(NUD.Position, true);
            boneTypeandVertexType = (byte)NUD.ReadByte(); // Determines which type of data type is stored. 
            UVSizeandVertexColorType = (byte)NUD.ReadByte(); 

            List<int> materialDataOffsetList = new List<int>();

            for(int i = 0; i < 4; i++)
            {
                materialDataOffsetList.Add(readIntBigEndian(NUD.Position));
            }

            materialDataOffset = materialDataOffsetList.ToArray();

            polyCount = readUShort(NUD.Position, true);
            int polySize = NUD.ReadByte();
            int polyFlag = NUD.ReadByte();

            if (polySize != 0x00)
                throw new NotImplementedException("polySize type not supported yet");
        }

        private void readTrianglesData(FileStream NUD, int polygonCount, int trianglesDataOffset, out List<ushort> primitiveIndiceValueList)
        {
            primitiveIndiceValueList = new List<ushort>();
            NUD.Seek(trianglesDataOffset, SeekOrigin.Current);
            for (int i = 0; i < polygonCount; i++)
            {
                primitiveIndiceValueList.Add((ushort)readShort(NUD.Position, true));
            }

            primitiveIndiceValueList = parsePrimitiveVertexIndices(primitiveIndiceValueList);
        }

        private void readVertexColorandUVData(FileStream NUD, int vertexColorType, int UVCount, out float[] vertexColor, out float[] UV)
        {
            // changed vertex color from uint[] to float[] as IDK why Maya won't accept color param with int / short;
            UV = new float[] { };
            float VCr, VCg, VCb, VCa;
            switch (vertexColorType)
            {
                case (int)vertexColorTypes.NoVertexColor:
                    vertexColor = new float[] { }; // No vertex Color type
                    break;

                case (int)vertexColorTypes.Byte:
                    VCr = (float)Decimal.Divide((uint)NUD.ReadByte(), 255);
                    VCg = (float)Decimal.Divide((uint)NUD.ReadByte(), 255);
                    VCb = (float)Decimal.Divide((uint)NUD.ReadByte(), 255);
                    VCa = (float)Decimal.Divide((uint)NUD.ReadByte(), 255);
                    vertexColor = new float[] { (float)VCr, (float)VCg, (float)VCb, (float)VCa }; // Vertex color in RGBA Byte
                    break;

                case (int)vertexColorTypes.HalfFloat:
                    VCr = ToFloat(readShort(NUD.Position, true));
                    VCg = ToFloat(readShort(NUD.Position, true));
                    VCb = ToFloat(readShort(NUD.Position, true));
                    VCa = ToFloat(readShort(NUD.Position, true));
                    vertexColor = new float[] { (float)VCr, (float)VCg, (float)VCb, (float)VCa }; // Vertex color in RGBA HalfFloat
                    break;

                default:
                    throw new Exception("vertex color Type not found!");
            }

            for(int j = 0; j < UVCount; j++)
            {
                // Freakin Half Floats, I have no idea how to convert, I will just use Smash Forge's converter
                float UVx = ToFloat(readShort(NUD.Position, true));
                float UVy = ToFloat(readShort(NUD.Position, true));
                UV = new float[] { UVx, UVy };
            }
        }

        private void readVertexPosNrmTanBitan(FileStream NUD, int vertexNormalType, out float[] verticePosition, out float[] verticeNormal)
        {
            float VPx = readFloat(NUD.Position, true);
            float VPy = readFloat(NUD.Position, true);
            float VPz = readFloat(NUD.Position, true);
            verticePosition = new float[] { VPx, VPy, VPz };

            if((vertexNormalTypes)vertexNormalType != vertexNormalTypes.NoNormals)
            {
                float Nx, Ny, Nz;
                // Since we don't need tan bitans, we can just skip the parts, but the size of the section is different.
                // All of the types have Nw, but difference in sizes. (float / short)
                switch ((vertexNormalTypes)vertexNormalType)
                {
                    case vertexNormalTypes.NormalsFloat:
                        Nx = readFloat(NUD.Position, true);
                        Ny = readFloat(NUD.Position, true);
                        Nz = readFloat(NUD.Position, true);
                        NUD.Seek(0x08, SeekOrigin.Current);
                        break;

                    case vertexNormalTypes.OnlyNormalsFloat: // IDK what tis is
                        Nx = readFloat(NUD.Position, true);
                        Ny = readFloat(NUD.Position, true);
                        Nz = readFloat(NUD.Position, true);
                        NUD.Seek(0x28, SeekOrigin.Current);
                        break;

                    case vertexNormalTypes.NormalsTanBiTanFloat:
                        NUD.Seek(0x04, SeekOrigin.Current); // I thought it was VPw, but it only exists in NormalsTanBitanwithFloat
                        Nx = readFloat(NUD.Position, true);
                        Ny = readFloat(NUD.Position, true);
                        Nz = readFloat(NUD.Position, true);
                        NUD.Seek(0x24, SeekOrigin.Current);
                        break;

                    case vertexNormalTypes.NormalsHalfFloat:
                        Nx = ToFloat(readShort(NUD.Position, true));
                        Ny = ToFloat(readShort(NUD.Position, true));
                        Nz = ToFloat(readShort(NUD.Position, true));
                        NUD.Seek(0x02, SeekOrigin.Current);
                        break;

                    case vertexNormalTypes.NormalsTanBiTanHalfFloat:
                        Nx = ToFloat(readShort(NUD.Position, true));
                        Ny = ToFloat(readShort(NUD.Position, true));
                        Nz = ToFloat(readShort(NUD.Position, true));
                        NUD.Seek(0x12, SeekOrigin.Current);
                        break;

                    default:
                        throw new Exception("vertex Normal Type not found!");
                }

                verticeNormal = new float[] { Nx, Ny, Nz };
            }
            else
            {
                NUD.Seek(0x04, SeekOrigin.Current);
                verticeNormal = new float[] {  };
            }
        }

        private void readVertexBoneData(FileStream NUD, int boneType, int nonRiggedBoneIndex, out Dictionary<int, float> boneIndicesandWeights)
        {
            boneIndicesandWeights = new Dictionary<int, float>();

            switch ((BoneTypes)boneType)
            {
                case BoneTypes.NoBones:
                    boneIndicesandWeights[nonRiggedBoneIndex] = 1;
                    break;

                case BoneTypes.Float:
                    for (int j = 0; j < 4; j++)
                    {
                        int boneIndex = readIntBigEndian(NUD.Position);
                        NUD.Seek(0x0C, SeekOrigin.Current);
                        float boneWeight = readFloat(NUD.Position, true);

                        if (boneWeight != 0)
                            boneIndicesandWeights[boneIndex] = boneWeight;

                        NUD.Seek(-0x10, SeekOrigin.Current);
                    }

                    NUD.Seek(0x10, SeekOrigin.Current);
                    break;

                case BoneTypes.HalfFloat:
                    for (int j = 0; j < 4; j++)
                    {
                        int boneIndex = readUShort(NUD.Position, true);
                        NUD.Seek(0x06, SeekOrigin.Current);
                        float boneWeight = ToFloat(readShort(NUD.Position, true));

                        if (boneWeight != 0)
                            boneIndicesandWeights[boneIndex] = boneWeight;

                        NUD.Seek(-0x08, SeekOrigin.Current);
                    }
                    break;

                case BoneTypes.Byte:
                    for (int j = 0; j < 4; j++)
                    {
                        int boneIndex = NUD.ReadByte();
                        NUD.Seek(0x03, SeekOrigin.Current);
                        float boneWeight = ((float)NUD.ReadByte() / 255);

                        if (boneWeight != 0)
                            boneIndicesandWeights[boneIndex] = boneWeight;

                        NUD.Seek(-0x04, SeekOrigin.Current);
                    }
                    break;

                default:
                    throw new Exception("bone Type not found!");
            }
        }

        private void parseVBN(FileStream VBN, out Dictionary<string, int> boneNamesandParentIndexDic, out List<Matrix4> boneTransformMatList)
        {
            boneNamesandParentIndexDic = new Dictionary<string, int>();
            boneTransformMatList = new List<Matrix4>();

            // TODO: add header checks.
            changeStreamFile(VBN);
            VBN.Seek(0x0C, SeekOrigin.Begin);
            int numberofBones = readIntBigEndian(VBN.Position);
            // read short bone type count, but skip for now.
            VBN.Seek(0x10, SeekOrigin.Current);

            // get the names of each bones, string with max 0x10 encoding length.
            for(int boneCount = 0; boneCount < numberofBones; boneCount++)
            {
                long initialPos = VBN.Position;
                string boneName = readString(VBN.Position, 0x10);

                VBN.Seek(initialPos + 0x10, SeekOrigin.Begin);

                int boneType = readIntBigEndian(VBN.Position);
                if (boneType > 1 || boneType < 0)
                    throw new NotImplementedException("Non type 0 and 1 bone not supported. Bone: " + boneName);

                uint parentBoneIndex = readUIntBigEndian(VBN.Position);
                if (parentBoneIndex == 0xFFFFFFF)
                    boneNamesandParentIndexDic[boneName] = -1; // For GBL_RT root bone. Since it is the root bone, it dosen't have a parent.

                boneNamesandParentIndexDic[boneName] = (int)parentBoneIndex;
            }

            int transformMatrixwithPadding = addPaddingSizeCalculation((int)VBN.Position + (numberofBones * 0x24));
            int inversedMatPos = transformMatrixwithPadding + (numberofBones * 0x40);
            // Normally we need to read the bone's pos rot scale, but since transformations are calculated already we will just use it directly. (Skip 0x24 per bone)
            VBN.Seek(inversedMatPos, SeekOrigin.Begin);

            // We only read the non-transformed matrix, since we can extract pos rot scale data. We can get inversed by using inversed function.
            //long transformMatrixOffset = VBN.Position + (numberofBones * 0x40);
            //VBN.Seek(transformMatrixOffset, SeekOrigin.Begin);
            for (int boneCount = 0; boneCount < numberofBones; boneCount++)
            {
                Matrix4 transformMatrix = new Matrix4();
                for (int i = 0; i < 4; i++)
                {
                    Vector4 row = new Vector4();

                    for (int j = 0; j < 4; j++)
                    {
                        float value = readFloat(VBN.Position, true);
                        row[j] = value;
                    }

                    switch (i)
                    {
                        case 0:
                            transformMatrix.Row0 = row;
                            break;
                        case 1:
                            transformMatrix.Row1 = row;
                            break;
                        case 2:
                            transformMatrix.Row2 = row;
                            break;
                        case 3:
                            transformMatrix.Row3 = row;
                            break;
                    }
                }
                boneTransformMatList.Add(transformMatrix);
            }
        }

        private List<ushort> parsePrimitiveVertexIndices(List<ushort> primitiveIndiceValueList)
        {
            // FB's nud seems to have FF blocks to indicate adding a new primitive. I have no idea why this is structured this way. 
            // Code modified from SmashForge's Polygon.cs GetRenderingVertexIndices() method. 

            List<ushort> sortedPrimitiveIndiceValueList = new List<ushort>();

            int initialFaceDirection = 1, faceDirection = initialFaceDirection;
            ushort face1 = (primitiveIndiceValueList.ElementAt(0));
            ushort face2 = (primitiveIndiceValueList.ElementAt(1));

            for(int i = 2; i < primitiveIndiceValueList.Count; i++)
            {
                ushort face3 = (primitiveIndiceValueList[i]);
                if(face3 == 0)
                {

                }

                if(primitiveIndiceValueList[i] == 0xFFFF)
                {
                    face1 = (primitiveIndiceValueList[i + 1]);
                    face2 = (primitiveIndiceValueList[i + 2]);
                    if(face2 == 0 || face1 == 0)
                    {

                    }

                    faceDirection = initialFaceDirection;
                }
                else
                {
                    faceDirection *= -1;
                    if(!face1.Equals(face2) && !face2.Equals(face3) && !face3.Equals(face1))
                    {
                        if(faceDirection > 0)
                        {
                            sortedPrimitiveIndiceValueList.Add(face3);
                            sortedPrimitiveIndiceValueList.Add(face2);
                            sortedPrimitiveIndiceValueList.Add(face1);
                        }
                        else
                        {
                            sortedPrimitiveIndiceValueList.Add(face2);
                            sortedPrimitiveIndiceValueList.Add(face3);
                            sortedPrimitiveIndiceValueList.Add(face1);
                        }
                    }
                    face1 = face2;
                    face2 = face3;
                }
            }

            return sortedPrimitiveIndiceValueList;
        }

        private void writeDAE(XElement DAE, XElement geometry, XElement controller, string DAEPath, List<ushort> primitiveIndiceValueList, List<float[]> vertexColorList,
            List<float[]> UVList, List<float[]> verticePositionList, List<float[]> verticeNormalList, List<Dictionary<int, float>> boneIndicesandWeightsList,
            Dictionary<string, int> boneNamesandParentIndexDic, List<Matrix4> boneTransformMatList)
        {
            // TODO: add validation to written DAE.
            //XmlDocument DAEXml = new XmlDocument();
            //DAEXml.Load(@"F:\Model Research\Gundam\Gun Model Research\Beam Gun.dae");
            //DAEXml.Schemas.Add(null, Properties.Resources.ColladaDAESchema);
            //validateDAE(DAEXml);

            string geometryBaseName = geometry.Attribute("name").Value;
            string geometryName = geometry.Attribute("id").Value;
            XElement mesh = geometry.Elements().First();

            string controllerName = controller.Attribute("id").Value;
            controllerName = geometryBaseName + controllerName;

            writeDAEGeometryLibrary(mesh, geometryName, primitiveIndiceValueList, vertexColorList, UVList, verticePositionList, verticeNormalList);
            writeDAEControllerLibrary(controller, controllerName, boneIndicesandWeightsList, boneNamesandParentIndexDic, boneTransformMatList);

            //validateDAE(DAEXml);

            DAE.Save(DAEPath);
        }

        private void writeDAEGeometryLibrary(XElement mesh, string geometryName, List<ushort> primitiveIndiceValueList, List<float[]> vertexColorList, 
            List<float[]> UVList, List<float[]> verticePositionList, List<float[]> verticeNormalList)
        {
            mesh.Elements().Remove();

            XElement posGeo = writeGeometrySource(geometryName, "pos", verticePositionList, new string[] { "X", "Y", "Z" }, 3);
            XElement nrmGeo = writeGeometrySource(geometryName, "nrm", verticeNormalList, new string[] { "X", "Y", "Z" }, 3);
            XElement UVGeo = writeGeometrySource(geometryName, "tx0", UVList, new string[] { "S", "T" }, 2);
            XElement verClrGeo = writeGeometrySource(geometryName, "clr", vertexColorList, new string[] { "R", "G", "B", "A" }, 4);

            Dictionary<string, string> verticesInput = new Dictionary<string, string>();
            verticesInput["POSITION"] = "#" + posGeo.Attribute("id").Value;
            verticesInput["NORMAL"] = "#" + nrmGeo.Attribute("id").Value;
            verticesInput["TEXCOORD"] = "#" + UVGeo.Attribute("id").Value;
            verticesInput["COLOR"] = "#" + verClrGeo.Attribute("id").Value;

            XElement verticesGeo = writeVertices(geometryName, verticesInput);

            Dictionary<string, string> trianglesInput = new Dictionary<string, string>();
            trianglesInput["VERTEX"] = "#" + verticesGeo.Attribute("id").Value;

            XElement trianglesGeo = writeTriangles(geometryName, trianglesInput, primitiveIndiceValueList);

            mesh.Add(posGeo, nrmGeo, UVGeo, verClrGeo, verticesGeo, trianglesGeo);
        }

        private XElement writeGeometrySource<T> (string geometryName, string geometryType, List<T[]> geoArray, string[] paramName, int stride)
        {
            if (geoArray.Any(element => !element.Length.Equals(stride)))
                throw new Exception("Array length not same with stride!");

            if (paramName.Length != stride)
                throw new Exception("paramName length not same with stride!");

            string arrType = Aliases[geoArray.First().First().GetType()], sourceAttrName = geometryName + "_" + geometryType, arrAttrName = geometryName + "_" + geometryType + "-array";
            int totalArrayCount = geoArray.Count() * stride;

            StringBuilder expandedArray = new StringBuilder();
            
            foreach(T element in geoArray.SelectMany(e => e))
            {
                expandedArray.Append(element);
                expandedArray.Append(" ");
            }

            XElement source = new XElement(ns + "source");
            XAttribute sourceID = new XAttribute("id", sourceAttrName);
            source.Add(sourceID);

            XElement array = new XElement(ns + arrType + "_array");
            XAttribute aID = new XAttribute("id", arrAttrName);
            XAttribute aCount = new XAttribute("count", totalArrayCount);
            array.Value = expandedArray.ToString();
            array.Add(aID, aCount);

            XElement techniqueComm = new XElement(ns + "technique_common");

            XElement accessor = new XElement(ns + "accessor");
            XAttribute accSource = new XAttribute("source", "#" + arrAttrName);
            XAttribute accCount = new XAttribute("count", geoArray.Count);
            XAttribute accStride = new XAttribute("stride", stride);
            accessor.Add(accSource, accCount, accStride);

            for(int i = 0; i < stride; i++)
            {
                XElement param = new XElement(ns + "param");
                XAttribute name = new XAttribute("name", paramName[i]);
                XAttribute type = new XAttribute("type", arrType);
                param.Add(name, type);
                accessor.Add(param);
            }

            techniqueComm.Add(accessor);

            source.Add(array, techniqueComm);

            return source;
        }

        private XElement writeVertices(string geometryName, Dictionary<string, string> semanticList)
        {
            XElement vertices = new XElement(ns + "vertices");
            XAttribute verticesID = new XAttribute("id", geometryName + "_verts");
            vertices.Add(verticesID);

            foreach(KeyValuePair<string, string> e in semanticList)
            {
                XElement input = new XElement(ns + "input");
                XAttribute inputSemantic = new XAttribute("semantic", e.Key);
                XAttribute inputSource = new XAttribute("source", e.Value);
                //XAttribute inputOffset = new XAttribute("offset", 0); // Vertices offset is always 0
                input.Add(inputSemantic, inputSource);
                vertices.Add(input);
            }

            return vertices;
        }

        private XElement writeTriangles(string geometryName, Dictionary<string, string> semanticList, List<ushort> primitiveIndices)
        {
            // https://stackoverflow.com/questions/19326366/how-do-dae-files-store-mesh-data
            // FB's nud input semantics are always fixed

            StringBuilder primitiveDataExpanded = new StringBuilder();

            XElement triangles = new XElement(ns + "triangles");
            XAttribute trianglesMat = new XAttribute("material", "");
            XAttribute trianglesCount = new XAttribute("count", primitiveIndices.Count);
            triangles.Add(trianglesMat, trianglesCount);

            int offset = 0;

            foreach (KeyValuePair<string, string> e in semanticList)
            {
                XElement input = new XElement(ns + "input");
                XAttribute inputSemantic = new XAttribute("semantic", e.Key);
                XAttribute inputSource = new XAttribute("source", e.Value);
                XAttribute inputOffset = new XAttribute("offset", offset);
                input.Add(inputSemantic, inputSource, inputOffset);
                triangles.Add(input);
                offset++;
            }

            foreach(ushort triangle in primitiveIndices)
            {
                primitiveDataExpanded.Append(triangle.ToString());
                primitiveDataExpanded.Append(" ");
            }

            XElement p = new XElement(ns + "p");
            p.Value = primitiveDataExpanded.ToString();

            triangles.Add(p);
            return triangles;
        }

        private void writeDAEControllerLibrary(XElement controller, string controllerName, List<Dictionary<int, float>> boneIndicesandWeightsList, 
            Dictionary<string, int> boneNamesandParentIndexDic, List<Matrix4> boneTransformMatList)
        {
            // TODO: add / change source for skin
            XElement skin = controller.Elements().First();
            skin.Elements().Remove();

            XElement matrix = new XElement("matrix");
            matrix.Value = "1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1";

            // Turn the bone name dic to list with array to comply with the method param.
            List<string[]> boneNameList = new List<string[]>();
            foreach(KeyValuePair<string, int> bone in boneNamesandParentIndexDic)
            {
                boneNameList.Add(new string[] { bone.Key });
            }

            // Expand the inverse matrix and convert them into str array.
            List<float[]> inverseBoneTransMatExp = new List<float[]>();

            for (int i = 0; i < boneTransformMatList.Count; i++)
            {
                float[] boneMat = new float[16];
                int index = 0;
                for (int column = 0; column < 4; column++)
                {
                    for(int row = 0; row < 4; row++)
                    {
                        boneMat[index] = boneTransformMatList[i][row, column];
                        index++;
                    }
                }
                inverseBoneTransMatExp.Add(boneMat);
            }

            List<float[]> boneWeightList = new List<float[]>();
            // Find common weight and make a list of weights.
            foreach(Dictionary<int, float> boneIndicesandWeights in boneIndicesandWeightsList)
            {
                // Since writeControllerSource required arrayed types, we need to force the weights to be arrayed, hence only one item in the array. 
                float[] newWeight = boneIndicesandWeights.Where(s =>
                    {
                        bool ifContains = boneWeightList.Any(p => p.SequenceEqual(new float[] { s.Value }));
                        return !ifContains;
                    }
                ).Select(e => e.Value).ToArray();

                foreach(float w in newWeight)
                {
                    boneWeightList.Add(new float[] { w });
                }
            }

            List<Dictionary<int, int>> boneIndicesandBoneWeightIndicesList = new List<Dictionary<int, int>>();
            foreach (Dictionary<int, float> boneIndicesandWeights in boneIndicesandWeightsList)
            {
                Dictionary<int, int> boneIndicesandBoneWeightIndices = new Dictionary<int, int>();
                foreach(var boneIndexandWeight in boneIndicesandWeights)
                {
                    int boneID = boneIndexandWeight.Key;
                    float boneWeight = boneIndexandWeight.Value;
                    int boneWeightIndex = boneWeightList.FindIndex(s => s.SequenceEqual(new float[] { boneWeight }));
                    boneIndicesandBoneWeightIndices[boneID] = boneWeightIndex;
                }
                boneIndicesandBoneWeightIndicesList.Add(boneIndicesandBoneWeightIndices);
            }

            XElement jointName = writeControllerSource(controllerName, "Joints", "Name", "name", boneNameList, new string[] { "JOINT" }, 1);
            XElement inverseTransformMatrix = writeControllerSource(controllerName, "Matrices", "float", "float4x4", inverseBoneTransMatExp, new string[] { "TRANSFORM" }, 16);
            XElement weight = writeControllerSource(controllerName, "Weights", "float", "float", boneWeightList, new string[] { "WEIGHT" }, 1);

            Dictionary<string, string> jointInputs = new Dictionary<string, string>();
            jointInputs["JOINT"] = "#" + jointName.Attribute("id").Value;
            jointInputs["INV_BIND_MATRIX"] = "#" + inverseTransformMatrix.Attribute("id").Value;

            XElement jointsCont = writeJoints(controllerName, jointInputs);

            Dictionary<string, string> vertexWeightInputs = new Dictionary<string, string>();
            vertexWeightInputs["JOINT"] = "#" + jointName.Attribute("id").Value;
            vertexWeightInputs["WEIGHT"] = "#" + weight.Attribute("id").Value;

            XElement vertexWeights = writeVertexWeights(controllerName, vertexWeightInputs, boneIndicesandBoneWeightIndicesList);

            skin.Add(matrix, jointName, inverseTransformMatrix, weight, jointsCont, vertexWeights);
        }

        private XElement writeControllerSource<T>(string controllerName, string controllerType, string paramTypeName, string accessorParamType, List<T[]> controllerArray, string[] paramName, int stride)
        { 
            if (controllerArray.Any(element => !element.Length.Equals(stride)))
                throw new Exception("Array length not same with stride!");

            string sourceAttrName = controllerName + "_" + controllerType, arrAttrName = controllerName + "_" + controllerType + "-array";
            int totalArrayCount = controllerArray.Count() * stride;

            StringBuilder expandedArray = new StringBuilder();
            //expandedArray.Append(Environment.NewLine);

            foreach (T[] ele in controllerArray)
            {
                foreach (T element in ele)
                {
                    expandedArray.Append(element);
                    expandedArray.Append(" ");
                }
                //expandedArray.Append(Environment.NewLine);
            }

            XElement source = new XElement(ns + "source");
            XAttribute sourceID = new XAttribute("id", sourceAttrName);
            source.Add(sourceID);

            XElement array = new XElement(ns + paramTypeName + "_array");
            XAttribute aID = new XAttribute("id", arrAttrName);
            XAttribute aCount = new XAttribute("count", totalArrayCount);
            array.Value = expandedArray.ToString();
            array.Add(aID, aCount);

            XElement techniqueComm = new XElement(ns + "technique_common");

            XElement accessor = new XElement(ns + "accessor");
            XAttribute accSource = new XAttribute("source", "#" + arrAttrName);
            XAttribute accCount = new XAttribute("count", controllerArray.Count);
            XAttribute accStride = new XAttribute("stride", stride);
            accessor.Add(accSource, accCount, accStride);

            for (int i = 0; i < paramName.Length; i++)
            {
                XElement param = new XElement(ns + "param");
                XAttribute name = new XAttribute("name", paramName[i]);
                XAttribute type = new XAttribute("type", accessorParamType);
                param.Add(name, type);
                accessor.Add(param);
            }

            techniqueComm.Add(accessor);

            source.Add(array, techniqueComm);

            return source;
        }

        private XElement writeJoints(string controllerName, Dictionary<string, string> semanticList)
        {
            XElement joints = new XElement(ns + "joints");
            XAttribute jointsID = new XAttribute("id", controllerName + "_verts");
            joints.Add(jointsID);

            foreach (KeyValuePair<string, string> e in semanticList)
            {
                XElement input = new XElement(ns + "input");
                XAttribute inputSemantic = new XAttribute("semantic", e.Key);
                XAttribute inputSource = new XAttribute("source", e.Value);
                //XAttribute inputOffset = new XAttribute("offset", 0); // Joints offset is always 0
                input.Add(inputSemantic, inputSource);
                joints.Add(input);
            }

            return joints;
        }

        private XElement writeVertexWeights(string geometryName, Dictionary<string, string> semanticList, List<Dictionary<int, int>> boneIndicesandBoneWeightIndicesList)
        {
            int count = 0; // IDK why sometimes it crash

            /*
            foreach(var s in boneIndicesandBoneWeightIndicesList.SelectMany(e => e))
            {
                count++;
            }
            */

            count = boneIndicesandBoneWeightIndicesList.Count;

            StringBuilder primitiveDataExpanded = new StringBuilder();

            XElement vertexWeights = new XElement(ns + "vertex_weights");
            XAttribute boneWeightCount = new XAttribute("count", count);
            vertexWeights.Add(boneWeightCount);

            int offset = 0;

            foreach (KeyValuePair<string, string> e in semanticList)
            {
                XElement input = new XElement(ns + "input");
                XAttribute inputSemantic = new XAttribute("semantic", e.Key);
                XAttribute inputSource = new XAttribute("source", e.Value);
                XAttribute inputOffset = new XAttribute("offset", offset);
                input.Add(inputSemantic, inputSource, inputOffset);
                vertexWeights.Add(input);
                offset++;
            }

            
            StringBuilder vcountVal = new StringBuilder();

            foreach(var boneIndicesandBoneWeightIndices in boneIndicesandBoneWeightIndicesList)
            {
                int numberofBones = boneIndicesandBoneWeightIndices.Count;
                vcountVal.Append(numberofBones);
                vcountVal.Append(" ");

                foreach(var boneIndexandBoneWeightIndex in boneIndicesandBoneWeightIndices)
                {
                    primitiveDataExpanded.Append(boneIndexandBoneWeightIndex.Key);
                    primitiveDataExpanded.Append(" ");
                    primitiveDataExpanded.Append(boneIndexandBoneWeightIndex.Value);
                    primitiveDataExpanded.Append(" ");
                }
            }

            XElement vcount = new XElement(ns + "vcount");
            vcount.Value = vcountVal.ToString();

            XElement p = new XElement(ns + "v");
            p.Value = primitiveDataExpanded.ToString();

            vertexWeights.Add(vcount, p);
            return vertexWeights;
        }

        private void validateDAE(XmlDocument DAE)
        {
            try
            {
                DAE.Validate(null);
            }
            catch (XmlSchemaValidationException)
            {
                throw new Exception("Schema Validation Failed.");
            }
        }

        private static float ToFloat(int hbits)
        {
            int mant = hbits & 0x03ff;            // 10 bits mantissa
            int exp = hbits & 0x7c00;            // 5 bits exponent
            if (exp == 0x7c00)                   // NaN/Inf
                exp = 0x3fc00;                    // -> NaN/Inf
            else if (exp != 0)                   // normalized value
            {
                exp += 0x1c000;                   // exp - 15 + 127
                if (mant == 0 && exp > 0x1c400)  // smooth transition
                    return BitConverter.ToSingle(BitConverter.GetBytes((hbits & 0x8000) << 16
                        | exp << 13 | 0x3ff), 0);
            }
            else if (mant != 0)                  // && exp==0 -> subnormal
            {
                exp = 0x1c400;                    // make it normal
                do
                {
                    mant <<= 1;                   // mantissa * 2
                    exp -= 0x400;                 // decrease exp by 1
                } while ((mant & 0x400) == 0); // while not normal
                mant &= 0x3ff;                    // discard subnormal bit
            }                                     // else +/-0 -> +/-0
            return BitConverter.ToSingle(BitConverter.GetBytes(          // combine all parts
                (hbits & 0x8000) << 16          // sign  << ( 31 - 15 )
                | (exp | mant) << 13), 0);         // value << ( 23 - 10 )
        }

        #endregion NUDtoDAE

        #region DAEtoNUD
        enum FixedSemantics
        {
            GEO_POSITION,
            GEO_NORMAL,
            GEO_TEXCOORD,
            GEO_VERTEX,
            GEO_COLOR,
            CONT_JOINT,
            CONT_INV_BIND_MATRIX,
            CONT_WEIGHT
        }

        public void fromDAEtoNUD()
        {
            string DAEPath = Properties.Settings.Default.DAEPathDAEtoNUD;
            XElement DAE = XElement.Load(DAEPath);
            ns = DAE.GetDefaultNamespace();

            List<Dictionary<FixedSemantics, List<dynamic[]>>> geoSourceDatasetList = new List<Dictionary<FixedSemantics, List<dynamic[]>>>();
            List<Dictionary<FixedSemantics, List<uint>>> geoPrimitiveDataSetList = new List<Dictionary<FixedSemantics, List<uint>>>();
            List<Dictionary<FixedSemantics, List<dynamic[]>>> contSourceDatasetList = new List<Dictionary<FixedSemantics, List<dynamic[]>>>();
            List<Dictionary<FixedSemantics, List<int[]>>> contPrimitiveDataSetList = new List<Dictionary<FixedSemantics, List<int[]>>>();
            List<XElement> imageList = new List<XElement>();

            // Parse visual scenes, which connects bone with controllers.
            // TODO: add condition for more than 1 visual scene node.
            Dictionary<string, string> DAEjointHierarchy = new Dictionary<string, string>();
            Dictionary<string, Matrix4> DAEjointPositionDic = new Dictionary<string, Matrix4>();
            List<string> instanceControllerList = new List<string>();
            List<string> skinSourceList = new List<string>();
            XElement visualScene = DAE.Element(ns + "library_visual_scenes").Element(ns + "visual_scene");
            XElement[] allVSNodes = visualScene.Elements(ns + "node").ToArray();

            parseVisualSceneNodes(allVSNodes, instanceControllerList, DAEjointHierarchy, DAEjointPositionDic);

            XElement[] libraryControllers = DAE.Element(ns + "library_controllers").Elements(ns + "controller").ToArray();
            foreach (string instanceController in instanceControllerList)
            {
                XElement controller = libraryControllers.FirstOrDefault(s => s.Attribute("id").Value.Equals(instanceController));
                if(controller != null)
                {
                    string controllerName = controller.Attributes().FirstOrDefault(attr => attr.Name.LocalName.Equals("id")).Value;
                    XElement skin = controller.Elements().First();
                    string skinSource = skin.Attribute("source").Value.Remove(0, 1);
                    skinSourceList.Add(skinSource);

                    Dictionary<FixedSemantics, List<dynamic[]>> contSourceDataset = parseDAEControllerSourceData(skin, out Dictionary<int, FixedSemantics> vertexWeightSourceDic);
                    Dictionary<FixedSemantics, List<int[]>> contPrimitiveDataSet = parseVertexWeightData(skin, vertexWeightSourceDic);

                    contSourceDatasetList.Add(contSourceDataset);
                    contPrimitiveDataSetList.Add(contPrimitiveDataSet);
                }
                else
                {
                    throw new Exception("Cannot find " + instanceController + " in libraryControllers!");
                }
            }

            XElement[] libraryGeometries = DAE.Element(ns + "library_geometries").Elements(ns + "geometry").ToArray();
            foreach (string skinSource in skinSourceList)
            {
                XElement geometry = libraryGeometries.FirstOrDefault(s => s.Attribute("id").Value.Equals(skinSource));
                if (geometry != null)
                {
                    string geometryName = geometry.Attributes().FirstOrDefault(attr => attr.Name.LocalName.Equals("name")).Value;
                    XElement mesh = geometry.Elements().First();

                    // TODO: add material read from triangles.
                    Dictionary<FixedSemantics, List<dynamic[]>> geoSourceDataset = parseDAEGeometrySourceData(mesh, out Dictionary<int, FixedSemantics> trianglesSourceDic);
                    Dictionary<FixedSemantics, List<uint>> geoPrimitiveDataSet = parseTrianglesData(mesh, trianglesSourceDic);

                    geoSourceDatasetList.Add(geoSourceDataset);
                    geoPrimitiveDataSetList.Add(geoPrimitiveDataSet);
                }
                else
                {
                    throw new Exception("Cannot find " + skinSource + " in libraryGeometries!");
                }
            }

            /*
            XElement[] geometries = DAE.Element(ns + "library_geometries").Elements(ns + "geometry").ToArray();
            
            foreach(XElement geometry in geometries)
            {
                string geometryName = geometry.Attributes().FirstOrDefault(attr => attr.Name.LocalName.Equals("name")).Value;
                XElement mesh = geometry.Elements().First();

                Dictionary<FixedSemantics, List<dynamic[]>> geoSourceDataset = parseDAEGeometrySourceData(mesh, out Dictionary<int, FixedSemantics> trianglesSourceDic);
                Dictionary<FixedSemantics, List<uint>> geoPrimitiveDataSet = parseTrianglesData(mesh, trianglesSourceDic);

                geoSourceDatasetList.Add(geoSourceDataset);
                geoPrimitiveDataSetList.Add(geoPrimitiveDataSet);
            }

            XElement[] controllers = DAE.Element(ns + "library_controllers").Elements(ns + "controller").ToArray();

            if (geometries.Length != controllers.Length)
                throw new Exception("Number of Geometry Library does not match Controllers Library!");

            foreach (XElement controller in controllers)
            {
                string controllerName = controller.Attributes().FirstOrDefault(attr => attr.Name.LocalName.Equals("id")).Value;
                XElement skin = controller.Elements().First();

                Dictionary<FixedSemantics, List<dynamic[]>> contSourceDataset = parseDAEControllerSourceData(skin, out Dictionary<int, FixedSemantics> vertexWeightSourceDic);
                Dictionary<FixedSemantics, List<int[]>> contPrimitiveDataSet = parseVertexWeightData(skin, vertexWeightSourceDic);

                contSourceDatasetList.Add(contSourceDataset);
                contPrimitiveDataSetList.Add(contPrimitiveDataSet);
            }
            */

            // TODO: materials / texture stuff
            XElement[] images = DAE.Element(ns + "library_images").Elements(ns + "image").ToArray();

            if (libraryGeometries.Length != images.Length)
            {
                //throw new Exception("Number of Geometry Library does not match Images Library!");
                foreach(var number in libraryGeometries)
                {
                    imageList.Add(new XElement("a")); // temporary placeholder, this list is only used for count.
                }
            }
            else
            {
                foreach (XElement image in images)
                {
                    string imageName = image.Attributes().FirstOrDefault(attr => attr.Name.LocalName.Equals("id")).Value;
                    XElement init_from = image.Elements().First();

                    imageList.Add(init_from);
                }
            }

            Dictionary<string, int> VBNjointHierarchy = new Dictionary<string, int>();
            if (!Properties.Settings.Default.exportVBN)
            {
                // If we don't need to export VBN, we follow the hierarchy of the original VBN file (no OMO needs to be changed to fit the new VBN file)
                // The only way is to get bone hierarchy is by reading VBN.
                // Since OMO is tied to VBN, this is assuming we don't change VBN during reimport.
                FileStream VBN = File.OpenRead(Properties.Settings.Default.VBNPathDAEtoNUD);
                parseVBN(VBN, out VBNjointHierarchy, out List<Matrix4> boneTransformMatList);
                VBN.Close();
            }
            else
            {
                // We fill VBNjointHierarchy's key with DAE's jointName since the code to write vertexInfo in NUD converts the DAE bone's hierarchy to VBN's hierarchy.
                foreach(string DAEjoint in DAEjointHierarchy.Keys)
                {
                    VBNjointHierarchy[DAEjoint] = -1;
                }
                writeVBN(DAEjointHierarchy, DAEjointPositionDic, contSourceDatasetList);
            }

            int numberOfShapeKeys = geoSourceDatasetList.Count;
            if (geoPrimitiveDataSetList.Count != numberOfShapeKeys || contSourceDatasetList.Count != numberOfShapeKeys || contPrimitiveDataSetList.Count != numberOfShapeKeys)
                throw new Exception("number of shape keys mismatch between data sources");

            writeNUD(geoSourceDatasetList, geoPrimitiveDataSetList, contSourceDatasetList, contPrimitiveDataSetList, imageList, VBNjointHierarchy, DAEjointHierarchy);
        }

        private void parseVisualSceneNodes(XElement[] allNodes, List<string> instanceControllerList, Dictionary<string, string> DAEjointHierarchy, Dictionary<string, Matrix4> DAEjointPositionDic)
        {
            // We only care about if the nodes have instance_controller (mesh) child node or JOINT type attributes.
            XElement[] allNodesNodes = allNodes.Where(s => s.Attribute("type").Value == "NODE").ToArray();

            foreach(XElement nodes in allNodesNodes)
            {
                XElement instanceController = nodes.Element(ns + "instance_controller");
                if(instanceController != null)
                {
                    string instanceControllerUrl = instanceController.Attribute("url").Value.Remove(0, 1);
                    instanceControllerList.Add(instanceControllerUrl);
                    // DAEjointHierarchy and DAEjointPositionDic is passed inside instead of making a List of the dic. 
                    // This way if there ae multiple parseInstanceController referencing to the same skeleton node we will assign it based on the key instead of adding it.
                    parseInstanceControllers(nodes.Parent, instanceController, DAEjointHierarchy, DAEjointPositionDic);
                }
                else
                {
                    parseVisualSceneNodes(allNodesNodes, instanceControllerList, DAEjointHierarchy, DAEjointPositionDic);
                }
            }
        }

        private void parseInstanceControllers(XElement parentNodes, XElement instanceController, Dictionary<string, string> DAEjointHierarchy, Dictionary<string, Matrix4> DAEjointPositionDic)
        {
            XElement[] skeletons = instanceController.Elements(ns + "skeleton").ToArray();
            foreach(XElement skeleton in skeletons)
            {
                // Find the node with the same url id attr.
                string urlwithoutHashtag = skeleton.Value.Remove(0, 1);
                XElement correspondingJointNode = parentNodes.Elements().FirstOrDefault(s => s.Attribute("id").Value.Equals(urlwithoutHashtag));

                if (correspondingJointNode == null)
                    throw new Exception("cannot find correspondingJointNode!");

                if (correspondingJointNode.Attribute("type").Value != "JOINT")
                    throw new Exception("node defined in Url not a Joint node!");

                // Get all bones in the visual scenes.
                string rootJointName = correspondingJointNode.Attribute("sid").Value;

                DAEjointHierarchy[rootJointName] = "null"; // Makes the first bone null. (root joint has no parent)
                // Parse joint hierarchy and get the joint position matrix.
                // Reason we pass an array of XElement, is becuase parseVisualScenetypeJoint is a recursive method that will get the list of child nodes, 
                // and when recalling the method it will require an array of XElement.
                // Hence on the first instance we only have 1 XElement node.
                parseVisualScenetypeJoint(new XElement[] { correspondingJointNode }, DAEjointHierarchy, DAEjointPositionDic, true); // the result is stored in the Dictionary passed inside, since it is a recursive method we can't use out.
            }
        }

        private void writeVBN(Dictionary<string, string> DAEjointHierarchy, Dictionary<string, Matrix4> DAEjointPositionDic, List<Dictionary<FixedSemantics, List<dynamic[]>>> contSourceDatasetList)
        {
            Dictionary<string, Matrix4> DAEjointBindMatrix = new Dictionary<string, Matrix4>();
            // Parse each bind pose matrix and get a dictionary of bind pose matrix 4.
            for (int i = 0; i < contSourceDatasetList.Count; i++)
            {
                List<dynamic[]> bindMatrixList = contSourceDatasetList[i][FixedSemantics.CONT_INV_BIND_MATRIX];
                List<dynamic[]> jointsNameList = contSourceDatasetList[i][FixedSemantics.CONT_JOINT];

                if (bindMatrixList.Count != jointsNameList.Count)
                    throw new Exception("bindMatrix count not the same as ");

                var bindMatrixandJointsNameList = bindMatrixList.Zip(jointsNameList, (s, p) => new { bindMatrix = s, jointsName = p });

                foreach(var bindMatandJointsName in bindMatrixandJointsNameList)
                {
                    float[] bindMatrixArr = bindMatandJointsName.bindMatrix.Cast<float>().ToArray();
                    string jointsName = bindMatandJointsName.jointsName.First();

                    if (bindMatrixArr.Length != 16)
                        throw new Exception("bindMatrixArr count is not 16!");

                    Matrix4 matrix = convertArraytoMatrix4(bindMatrixArr);

                    if (DAEjointBindMatrix.ContainsKey(jointsName))
                    {
                        // A check to make sure bind pose matrix across all controllers are the same.
                        if (DAEjointBindMatrix[jointsName] != matrix)
                            throw new Exception("bind matrix not the same value across controllers!");
                    }
                    else
                    {
                        DAEjointBindMatrix[jointsName] = matrix;
                    }
                }

            }

            if (DAEjointHierarchy.Count != DAEjointPositionDic.Count)
                throw new Exception("DAEjointHierarchy does not has the same count as DAEjointPositionDic");

            MemoryStream VBNStream = new MemoryStream();
            MemoryStream VBNHeaderStream = new MemoryStream();
            MemoryStream jointNameandPositionStream = new MemoryStream();
            MemoryStream jointTransformandInverseBindMatrixStream = new MemoryStream();

            appendIntMemoryStream(VBNHeaderStream, 0x56424E20, true); // VBN Header
            appendIntMemoryStream(VBNHeaderStream, 0x00020000, true); // VBN version?
            // TODO: add Flags selection
            appendIntMemoryStream(VBNHeaderStream, 0x00000001, true); // VBN Flags, no idea what are these. Either 0x190 or 0x191 or 0x1. 0x190 for T-Pose (non animation?) for guns and other parts.
            appendIntMemoryStream(VBNHeaderStream, DAEjointHierarchy.Count, true);
            // TODO: differentiate between bone type count. bone type 1 = animated (bones in OMO), bone type 2 = remaining. 
            appendIntMemoryStream(VBNHeaderStream, DAEjointHierarchy.Count, true); // We need to change this manually.
            appendZeroMemoryStream(VBNHeaderStream, 0x0C); // bone count for type 2 to 4.

            string[] DAEjoints = DAEjointHierarchy.Keys.ToArray();

            foreach (string DAEjoint in DAEjoints)
            {
                // Joint Names
                byte[] encodedString = Encoding.Default.GetBytes(DAEjoint);
                encodedString = encodedString.Skip(encodedString.Length - 0x0F).Take(0x0F).ToArray();
                byte[] encodedStringFixedSize = new byte[0x10];

                for(int i = 0; i < 0x0F; i++)
                {
                    // To fill the array if the size is < 0x10;
                    if(i < encodedString.Length)
                    {
                        encodedStringFixedSize[i] = encodedString[i];
                    }
                }

                jointNameandPositionStream.Write(encodedStringFixedSize, 0, encodedStringFixedSize.Length);
                // TODO: research proper bone types. We use 0 for now.
                appendIntMemoryStream(jointNameandPositionStream, 0x00, true);

                string parentBoneName = DAEjointHierarchy[DAEjoint];
                int parentIndex = parentBoneName == "null" ? 0x0FFFFFFF : DAEjointHierarchy.Keys.ToList().IndexOf(parentBoneName);
                appendIntMemoryStream(jointNameandPositionStream, parentIndex, true);
            }

            foreach (string DAEjoint in DAEjoints)
            {
                Vector3 trans = new Vector3(2.649f, 0f, 3.602f);
                Vector3 scale = new Vector3(1, 1, 1);

                Matrix4 rotationMat = Matrix4.CreateFromAxisAngle(Vector3.UnitX, 0) * Matrix4.CreateFromAxisAngle(Vector3.UnitY, 0.0872665f) * Matrix4.CreateFromAxisAngle(Vector3.UnitZ, 0);
                Matrix4 transMat = Matrix4.CreateTranslation(trans);
                Matrix4 scaleMat = Matrix4.CreateScale(scale);

                Matrix4 transformationMatTRS = transMat * rotationMat * scaleMat;
                Matrix4 transformationMatSRT = scaleMat * rotationMat * transMat;

                // Joint Position
                Matrix4 posMatrix = DAEjointPositionDic[DAEjoint];
                /*
                Matrix4.Invert(posMatrix);

                Vector3 sca = posMatrix.ExtractScale();
                Matrix4 scaMat = Matrix4.CreateScale(sca);
                Matrix4.Invert(scaMat);

                Vector3 tra = posMatrix.ExtractTranslation();
                Matrix4 traMat = Matrix4.CreateTranslation(tra);
                Matrix4.Invert(traMat);

                Matrix4 scaRotMat = Matrix4.Mult(posMatrix, traMat);
                Matrix4 rotMat = Matrix4.Mult(scaRotMat, scaMat);

                //Vector3 rot = posMatrix.ExtractRotation().Xyz;

                float rotX = (float)Math.Atan2((-rotMat.Row1.Z), rotMat.Row2.Z);
                float cosY = (float)Math.Sqrt(Math.Pow(rotMat.Row0.X, 2) + Math.Pow(rotMat.Row0.Y, 2));
                float rotY = (float)Math.Atan2(rotMat.Row0.Z, cosY);
                float sinX = (float)Math.Sin(rotX);
                float cosX = (float)Math.Cos(rotX);
                float rotZ = (float)(Math.Atan2(cosX * rotMat.Row1.X + sinX * rotMat.Row2.X, cosX * rotMat.Row1.Y + sinX * rotMat.Row2.Y));
                */
                DecomposeSRTMatrix(posMatrix, out Vector3 sca, out Vector3 rot, out Vector3 tra);

                float[] traArr = new float[] { tra.X, tra.Y, tra.Z };
                float[] rotArr = new float[] { rot.X, rot.Y, rot.Z };
                float[] scaArr = new float[] { sca.X, sca.Y, sca.Z };

                for (int i = 0; i < 3; i++)
                {
                    appendFloatMemoryStream(jointNameandPositionStream, traArr[i], true);
                }
                for (int i = 0; i < 3; i++)
                {
                    appendFloatMemoryStream(jointNameandPositionStream, rotArr[i], true);
                }
                for (int i = 0; i < 3; i++)
                {
                    appendFloatMemoryStream(jointNameandPositionStream, scaArr[i], true);
                }
            }

            addPaddingStream(jointNameandPositionStream);

            foreach (string DAEjoint in DAEjoints)
            {
                // TransformMatrix
                Matrix4 transformMatrix = DAEjointBindMatrix[DAEjoint];
                transformMatrix = Matrix4.Invert(transformMatrix);
                
                for (int i = 0; i < 4; i++)
                {
                    appendFloatMemoryStream(jointTransformandInverseBindMatrixStream, transformMatrix.Row0[i], true);
                }
                for (int i = 0; i < 4; i++)
                {
                    appendFloatMemoryStream(jointTransformandInverseBindMatrixStream, transformMatrix.Row1[i], true);
                }
                for (int i = 0; i < 4; i++)
                {
                    appendFloatMemoryStream(jointTransformandInverseBindMatrixStream, transformMatrix.Row2[i], true);
                }
                for (int i = 0; i < 4; i++)
                {
                    appendFloatMemoryStream(jointTransformandInverseBindMatrixStream, transformMatrix.Row3[i], true);
                }
            }

            foreach (string DAEjoint in DAEjoints)
            {
                // InverseBindMatrix
                Matrix4 inverseBindMatrix = DAEjointBindMatrix[DAEjoint];

                for (int i = 0; i < 4; i++)
                {
                    appendFloatMemoryStream(jointTransformandInverseBindMatrixStream, inverseBindMatrix.Row0[i], true);
                }
                for (int i = 0; i < 4; i++)
                {
                    appendFloatMemoryStream(jointTransformandInverseBindMatrixStream, inverseBindMatrix.Row1[i], true);
                }
                for (int i = 0; i < 4; i++)
                {
                    appendFloatMemoryStream(jointTransformandInverseBindMatrixStream, inverseBindMatrix.Row2[i], true);
                }
                for (int i = 0; i < 4; i++)
                {
                    appendFloatMemoryStream(jointTransformandInverseBindMatrixStream, inverseBindMatrix.Row3[i], true);
                }
            }

            VBNStream.Write(VBNHeaderStream.GetBuffer(), 0, (int)VBNHeaderStream.Length);
            VBNStream.Write(jointNameandPositionStream.GetBuffer(), 0, (int)jointNameandPositionStream.Length);
            VBNStream.Write(jointTransformandInverseBindMatrixStream.GetBuffer(), 0, (int)jointTransformandInverseBindMatrixStream.Length);

            VBNHeaderStream.Close();
            jointNameandPositionStream.Close();
            jointTransformandInverseBindMatrixStream.Close();

            string DAEfileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.DAEPathDAEtoNUD);

            string VBNPath = Path.Combine(Properties.Settings.Default.OutputPathDAEtoNUD, DAEfileName + " Converted.vbn");
            Stream writeStream = File.Open(VBNPath, FileMode.Create);
            VBNStream.WriteTo(writeStream);
            VBNStream.Close();
            writeStream.Close();
        }

        private void writeNUD(List<Dictionary<FixedSemantics, List<dynamic[]>>> sourceDatasetList, List<Dictionary<FixedSemantics, List<uint>>> primitiveDataSetList,
            List<Dictionary<FixedSemantics, List<dynamic[]>>> contSourceDatasetList, List<Dictionary<FixedSemantics, List<int[]>>> contPrimitiveDataSetList, List<XElement> imageList,
            Dictionary<string, int> VBNjointHierarchy, Dictionary<string, string> DAEjointHierarchy)
        {
            MemoryStream NUDStream = new MemoryStream();

            // Wrong concept? (Maybe check if primitive indice count > vertex count
            //Dictionary<FixedSemantics, List<dynamic[]>> dataset = convertPolySourcestoVertex(sourceDataset, primitiveDataSet);

            int numberofShapeKeys = sourceDatasetList.Count;

            List<int> stripNoList = new List<int>();
            List<int> vertexIndicesOffsets = new List<int>();
            List<int> vertexColorandUVOffsets = new List<int>();
            List<int> vertexDataOffsets = new List<int>();
            List<int> materialDataOffsets = new List<int>();
            List<int> polysetMetadataMaterialOffsets = new List<int>();

            MemoryStream headerStream = new MemoryStream();
            MemoryStream polySetMetadataStream = new MemoryStream();
            MemoryStream materialStream = new MemoryStream();
            MemoryStream vertexIndicesStream = new MemoryStream();
            MemoryStream vertexColorandUVStream = new MemoryStream();
            MemoryStream vertexDataStream = new MemoryStream();

            vertexIndicesOffsets.Add(0);
            vertexColorandUVOffsets.Add(0);
            vertexDataOffsets.Add(0);

            foreach (var primitiveDataSet in primitiveDataSetList)
            {
                writeVertexIndices(primitiveDataSet, vertexIndicesStream, out int sizeWithoutPadding, out int stripNo);
                stripNoList.Add(stripNo);
                vertexIndicesOffsets.Add(sizeWithoutPadding);
            }

            foreach(var sourceDataset in sourceDatasetList)
            {
                writeVertexColorandUV(sourceDataset, vertexColorandUVStream, out int vertexColorandUVSizewithoutPadding);
                vertexColorandUVOffsets.Add(vertexColorandUVSizewithoutPadding);
            }

            foreach (var images in imageList)
            {
                materialDataOffsets.Add((int)materialStream.Position);
                writeMaterials(materialStream);
            }

            for (int i = 0; i < numberofShapeKeys; i++)
            {
                writeVertex(sourceDatasetList[i], primitiveDataSetList[i], contSourceDatasetList[i], contPrimitiveDataSetList[i], vertexDataStream, VBNjointHierarchy, DAEjointHierarchy, 
                    out int vertexSizewithoutPadding);
                vertexDataOffsets.Add(vertexSizewithoutPadding);

                writePolySetMetadata(polySetMetadataStream, sourceDatasetList[i][FixedSemantics.GEO_POSITION].Count, stripNoList[i],
                    materialDataOffsets[i], vertexIndicesOffsets[i], vertexColorandUVOffsets[i], vertexDataOffsets[i],
                    out int materialStreamOffset);
                polysetMetadataMaterialOffsets.Add(materialStreamOffset);
            }
            
            addPaddingStream(vertexIndicesStream);
            addPaddingStream(vertexColorandUVStream);
            addPaddingStream(vertexDataStream);
            addPaddingStream(materialStream);
            addPaddingStream(polySetMetadataStream);

            int vertexIndicesStreamSize = (int)vertexIndicesStream.Length;
            int vertexColorandUVStreamSize = (int)vertexColorandUVStream.Length;
            int vertexDataStreamSize = (int)vertexDataStream.Length;
            int materialStreamSize = (int)materialStream.Length;
            int polySetMetadataStreamSize = (int)polySetMetadataStream.Length;

            // TODO: add loop for more than 1 dataSet. 
            writeNUDHeader(headerStream, 1, numberofShapeKeys, contSourceDatasetList, 
                polySetMetadataStreamSize, materialStreamSize, vertexIndicesStreamSize, vertexColorandUVStreamSize, vertexDataStreamSize,
                out int metadataSizeOffet);

            int materialStartOffset = (int)headerStream.Length + (int)polySetMetadataStream.Length;
            for (int i = 0; i < polysetMetadataMaterialOffsets.Count; i++)
            {
                polySetMetadataStream.Seek(polysetMetadataMaterialOffsets[i], SeekOrigin.Begin);
                int materialGlobalOffset = materialDataOffsets[i] + materialStartOffset;
                polySetMetadataStream.Write(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(materialGlobalOffset)), 0, 4);
            }

            int totalMetadataSize = (int)headerStream.Length + polySetMetadataStreamSize + materialStreamSize;
            headerStream.Seek(metadataSizeOffet, SeekOrigin.Begin);
            headerStream.Write(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(totalMetadataSize - 0x30)), 0, 4); // The size will have -0x30, I have no idea why;

            string shapeName = "SHAPE_ROOT\0";
            byte[] shapeNameBuffer = Encoding.Default.GetBytes(shapeName);

            int totalFileSize = totalMetadataSize + vertexIndicesStreamSize + vertexColorandUVStreamSize + vertexDataStreamSize + shapeNameBuffer.Length;

            headerStream.Seek(0x04, SeekOrigin.Begin);
            headerStream.Write(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(totalFileSize)), 0, 4);

            NUDStream.Write(headerStream.GetBuffer(), 0, (int)headerStream.Length);
            NUDStream.Write(polySetMetadataStream.GetBuffer(), 0, polySetMetadataStreamSize);
            NUDStream.Write(materialStream.GetBuffer(), 0, materialStreamSize);
            NUDStream.Write(vertexIndicesStream.GetBuffer(), 0, vertexIndicesStreamSize);
            NUDStream.Write(vertexColorandUVStream.GetBuffer(), 0, vertexColorandUVStreamSize);
            NUDStream.Write(vertexDataStream.GetBuffer(), 0, vertexDataStreamSize);
            NUDStream.Write(shapeNameBuffer, 0, shapeNameBuffer.Length);

            string DAEfileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.DAEPathDAEtoNUD);
           
            string NUDPath = Path.Combine(Properties.Settings.Default.OutputPathDAEtoNUD, DAEfileName + " Converted.nud");
            Stream writeStream = File.Open(NUDPath, FileMode.Create);
            NUDStream.WriteTo(writeStream);
            NUDStream.Close();
            writeStream.Close();
        }

        private void writeNUDHeader(MemoryStream headerStream, int datasetCount, int polySetCount,
            List<Dictionary<FixedSemantics, List<dynamic[]>>> contSourceDatasetList,
            int polySetMetadataSize, int materialMetadataSize, int vertexIndicesSize, int vertexColorandUVStreamSize, int vertexDataStreamSize,
            out int metadataSizeOffset)
        {
            ushort boneNumber = 0;
            List<dynamic[]> allBoneJoints = contSourceDatasetList.First()[FixedSemantics.CONT_JOINT];
            foreach (var bone in allBoneJoints)
            {
                boneNumber += (ushort)bone.Length;
            }
            boneNumber -= 1;
            
            // TODO: make this header part another method if we have more than 1 datasets.
            appendUIntMemoryStream(headerStream, 0x4E445033, true); // NDP3 header.
            appendUIntMemoryStream(headerStream, 0x00000000, true); // the size of the whole file. This is just a placeholder, we will write the actual file size.
            appendUShortMemoryStream(headerStream, 0x02, false); // version. Always in little endian.
            appendUShortMemoryStream(headerStream, 1, true); // the number of dataSet. it should always be one? Never met two or more.
            appendUShortMemoryStream(headerStream, 0x0001, true); // NUD type, still up to experimentations, use 01 for now. (no bone = 0)
            appendUShortMemoryStream(headerStream, boneNumber, true); // Number of bones. still unclear why it is -1, for now following SmashForge's algorithm.

            // Start of repeating if more than 1 datasets.
            metadataSizeOffset = (int)headerStream.Position;
            appendUIntMemoryStream(headerStream, 0x00000000, true); // Placeholder offset for whole metadata section, we need to write it after knowing the true size of whole header.
            appendUIntMemoryStream(headerStream, (uint)vertexIndicesSize, true);
            appendUIntMemoryStream(headerStream, (uint)vertexColorandUVStreamSize, true);
            appendUIntMemoryStream(headerStream, (uint)vertexDataStreamSize, true);

            // TODO: calculate bounding spheres. 
            appendFloatMemoryStream(headerStream, 0, true);
            appendFloatMemoryStream(headerStream, 9.177307f, true);
            appendFloatMemoryStream(headerStream, 0.431778073f, true);
            appendFloatMemoryStream(headerStream, 10.8765144f, true);
            appendFloatMemoryStream(headerStream, 0, true);
            appendFloatMemoryStream(headerStream, 9.177307f, true);
            appendFloatMemoryStream(headerStream, 0.431778073f, true);
            appendFloatMemoryStream(headerStream, 10.8765144f, true);
            appendFloatMemoryStream(headerStream, 0, true);
            appendFloatMemoryStream(headerStream, 9.177307f, true);
            appendFloatMemoryStream(headerStream, 0.431778073f, true);
            appendFloatMemoryStream(headerStream, 0, true);

            appendZeroMemoryStream(headerStream, 6);
            appendUShortMemoryStream(headerStream, 0x04, true);
            appendUShortMemoryStream(headerStream, 0xFFFF, true);
            appendUShortMemoryStream(headerStream, (ushort)polySetCount, true);
            appendUIntMemoryStream(headerStream, (uint)headerStream.Position + 0x04, true);
        }

        private void writePolySetMetadata(MemoryStream polySetMetadata,
            int vertexCount, int vertexIndicesCount, 
            int materialOffset, int vertexIndicesOffset, int vertexColorandUVOffset, int vertexDataOffset,
            out int materialStreamOffset)
        {
            appendUIntMemoryStream(polySetMetadata, (uint)vertexIndicesOffset, true);
            appendUIntMemoryStream(polySetMetadata, (uint)vertexColorandUVOffset, true);
            appendUIntMemoryStream(polySetMetadata, (uint)vertexDataOffset, true);
            appendUShortMemoryStream(polySetMetadata, (ushort)vertexCount, true);
            polySetMetadata.WriteByte(0x13); // vertexFlag. Check the NUDtoDAE for different types. For now we only export type 13
            polySetMetadata.WriteByte(0x12); // UVFlag

            materialStreamOffset = (int)polySetMetadata.Position;
            appendUIntMemoryStream(polySetMetadata, (uint)materialOffset, true); // Placeholder for offset, we need to get the real offset after writing the header since the offset is global.
            appendZeroMemoryStream(polySetMetadata, 0x0C);
            appendUShortMemoryStream(polySetMetadata, (ushort)vertexIndicesCount, true);
            appendUShortMemoryStream(polySetMetadata, 0x0004, true);
            appendZeroMemoryStream(polySetMetadata, 0x0C);
        }

        // Not needed?
        private Dictionary<FixedSemantics, List<dynamic[]>> convertPolySourcestoVertex(Dictionary<FixedSemantics, List<dynamic[]>> sourceDataset, Dictionary<FixedSemantics, List<uint>> primitiveDataSet)
        {
            if (sourceDataset.Count != primitiveDataSet.Count)
                throw new Exception("Debug: check parseVertexandPolySources. This should not be seen in production.");

            // To iterate through both dic at once. 
            // https://stackoverflow.com/questions/1955766/iterate-two-lists-or-arrays-with-one-foreach-statement-in-c-sharp/1955780
            var sourceandprimitive = sourceDataset.Zip(primitiveDataSet, (s, p) => new { sourceDataset = s, primitiveDataSet = p });

            Dictionary<FixedSemantics, List<dynamic[]>> convertedDataset = sourceDataset;

            foreach (var dataset in sourceandprimitive)
            {
                FixedSemantics source = dataset.primitiveDataSet.Key;
                List<dynamic[]> data = sourceDataset[source];
                List<dynamic[]> convertedSourceData = new List<dynamic[]>();
                List<uint> primitiveData = dataset.primitiveDataSet.Value;

                if (source != FixedSemantics.GEO_VERTEX)
                {
                    foreach(uint vertexID in primitiveData)
                    {
                        if (vertexID > data.Count)
                            throw new Exception("vertexID is larger than sourceDataset!");

                        convertedSourceData.Add(data[(int)vertexID]);
                    }
                }
                else if(source == FixedSemantics.GEO_VERTEX)
                {
                    uint[] primitiveArr = primitiveData.ToArray();
                    List<uint[]> primitiveList = new List<uint[]>();
                    primitiveList.Add(primitiveArr);
                    
                    convertedSourceData = primitiveList as dynamic;
                }
                else
                {
                    throw new NotImplementedException("??!??");
                }

                convertedDataset[source] = (convertedSourceData);
            }

            return convertedDataset;
        }

        private Dictionary<FixedSemantics, List<dynamic[]>> parseDAEGeometrySourceData(XElement mesh, out Dictionary<int, FixedSemantics> trianglesSourceDic)
        {
            Dictionary<FixedSemantics, XElement> sourcebyIDDict = new Dictionary<FixedSemantics, XElement>();
            List<XElement> allSourceList = mesh.Elements(ns + "source").ToList();

            XElement vertices = mesh.Element(ns + "vertices");
            getInputSources(vertices, allSourceList, sourcebyIDDict, "GEO_"); // output not required, we update the dictionary passed
            allSourceList.Add(vertices); //vertices is also one of the source - for triangles

            XElement triangles = mesh.Element(ns + "triangles");
            trianglesSourceDic = getInputSources(triangles, allSourceList, sourcebyIDDict, "GEO_");

            Dictionary<FixedSemantics, List<dynamic[]>> sourceData = new Dictionary<FixedSemantics, List<dynamic[]>>();
            foreach (FixedSemantics sourceType in Enum.GetValues(typeof(FixedSemantics)).OfType<FixedSemantics>().Where(s => s.ToString().StartsWith("GEO")).Select(s => s).ToArray())
            {
                if(sourceType != FixedSemantics.GEO_VERTEX)
                {
                    if (!sourcebyIDDict.ContainsKey(sourceType) && sourceType != FixedSemantics.GEO_COLOR)
                        throw new Exception(Enum.GetName(typeof(FixedSemantics), sourceType) + " source not found!");

                    if (sourcebyIDDict.ContainsKey(sourceType))
                    {
                        XElement source = sourcebyIDDict[sourceType];
                        List<dynamic[]> dataSet = parseSource(source);
                        sourceData[sourceType] = dataSet;
                    }
                }
            }

            return sourceData;
        }

        private Dictionary<int, FixedSemantics> getInputSources(XElement node, IEnumerable<XElement> allSourceList, Dictionary<FixedSemantics, XElement> sourcebyIDDict, string fixedSemanticAffix)
        {
            Dictionary<int, FixedSemantics> sourcewithOffset = new Dictionary<int, FixedSemantics>();

            foreach (XElement input in node.Elements(ns + "input").ToList())
            {
                string semantic = fixedSemanticAffix + input.Attribute("semantic").Value;
                if (!FixedSemantics.TryParse(semantic, out FixedSemantics res))
                    throw new Exception("Semantic parse failed!");
                
                if (!res.Equals(null))
                {
                    string sourceName = input.Attribute("source").Value != null ? input.Attribute("source").Value : throw new Exception("No attribute found with source attribute!");
                    sourceName = sourceName.Remove(0, 1); // removing the #

                    XElement source = allSourceList.FirstOrDefault(s => s.Attribute("id").Value.Equals(sourceName));

                    if (source == null)
                        throw new Exception("No source found with same id attribute!");

                    if(res != FixedSemantics.GEO_VERTEX)
                    {
                        sourcebyIDDict[res] = source;
                    }
                    
                    // For primitives 
                    XAttribute offset = input.Attribute("offset");
                    if (offset != null)
                    {
                        int.TryParse(offset.Value, out int pOffset);
                        sourcewithOffset[pOffset] = res;
                    }
                }
            }

            // Only used by primitive nodes
            return sourcewithOffset;
        }

        private List<dynamic[]> parseSource(XElement source)
        {
            List<dynamic[]> valueList = new List<dynamic[]>();
            List<dynamic> strideValueList = new List<dynamic>();
            List<Type> strideTypeList = new List<Type>();

            XElement technique = source.Element(ns + "technique_common");
            XElement accessor = technique.Element(ns + "accessor");

            int.TryParse(accessor.Attribute("count").Value, out int dataSetsCount);

            int stride = 1;
            if (accessor.Attribute("stride") != null)
            {
                int.TryParse(accessor.Attribute("stride").Value, out stride);
            }

            foreach(XElement param in accessor.Elements(ns + "param"))
            {
                string type = param.Attribute("type").Value.ToLower();
                Type valType = type != "name" ? Aliases.FirstOrDefault(t => t.Value.Equals(type)).Key : typeof(string);

                if (valType == typeof(Matrix4))
                {
                    for(int u = 0; u < 16; u++)
                    {
                        strideTypeList.Add(typeof(float));
                    }
                }
                else
                {
                    strideTypeList.Add(valType);
                }
            }

            XElement array = source.Elements().FirstOrDefault(s => s.Name.LocalName.Contains("_array"));
            string values = array.Value;
            IEnumerable<string> allValues = spiltStringData(values);

            int i = 0;
            foreach (string value in allValues)
            {
                object convertedValue = Convert.ChangeType(value, strideTypeList[i]);
                strideValueList.Add(convertedValue);

                i++;
                if (i >= stride)
                {
                    i = 0;
                    valueList.Add(strideValueList.ToArray());
                    strideValueList = new List<dynamic>();
                }
            }

            if (valueList.Count != dataSetsCount)
                throw new Exception("Incorrect stride / array count combination, check DAE file.");

            return valueList;
        }

        private Dictionary<FixedSemantics, List<uint>> parseTrianglesData(XElement mesh, Dictionary<int, FixedSemantics> trianglesSourceDic)
        {
            XElement triangles = mesh.Element(ns + "triangles");

            XElement primitive = triangles.Element(ns + "p");
            string primitiveData = primitive.Value;
            IEnumerable<string> allValues = spiltStringData(primitiveData);

            Dictionary<FixedSemantics, List<uint>> polygonPrimitiveDataSets = new Dictionary<FixedSemantics, List<uint>>();

            foreach(KeyValuePair<int, FixedSemantics> trianglesSources in trianglesSourceDic)
            {
                polygonPrimitiveDataSets[trianglesSources.Value] = new List<uint>();
            }

            int i = 0;
            foreach(string valueStr in allValues)
            {
                if (i >= trianglesSourceDic.Count)
                    i = 0;

                uint.TryParse(valueStr, out uint value);
                FixedSemantics source = trianglesSourceDic[i];

                polygonPrimitiveDataSets[source].Add(value);
                i++;
            }

            return polygonPrimitiveDataSets;
        }

        private Dictionary<FixedSemantics, List<dynamic[]>> parseDAEControllerSourceData(XElement skin, out Dictionary<int, FixedSemantics> vertexWeightSourceDic)
        {
            Dictionary<FixedSemantics, XElement> sourcebyIDDict = new Dictionary<FixedSemantics, XElement>();
            List<XElement> allSourceList = skin.Elements(ns + "source").ToList();

            XElement joints = skin.Element(ns + "joints");
            getInputSources(joints, allSourceList, sourcebyIDDict, "CONT_"); // output not required, we update the dictionary passed

            XElement vertexWeights = skin.Element(ns + "vertex_weights");
            vertexWeightSourceDic = getInputSources(vertexWeights, allSourceList, sourcebyIDDict, "CONT_");

            Dictionary<FixedSemantics, List<dynamic[]>> sourceData = new Dictionary<FixedSemantics, List<dynamic[]>>();
            foreach (FixedSemantics sourceType in Enum.GetValues(typeof(FixedSemantics)).OfType<FixedSemantics>().Where(s => s.ToString().StartsWith("CONT")).Select(s => s).ToArray())
            {
                if (!sourcebyIDDict.ContainsKey(sourceType))
                    throw new Exception(Enum.GetName(typeof(FixedSemantics), sourceType) + " source not found!");

                XElement source = sourcebyIDDict[sourceType];
                List<dynamic[]> dataSet = parseSource(source);
                sourceData[sourceType] = dataSet;
            }

            return sourceData;
        }

        private Dictionary<FixedSemantics, List<int[]>> parseVertexWeightData(XElement skin, Dictionary<int, FixedSemantics> vertexWeightSourceDic)
        {
            XElement vertexWeights = skin.Element(ns + "vertex_weights");

            XElement primitiveCount = vertexWeights.Element(ns + "vcount");
            string primitiveCountData = primitiveCount.Value;
            List<string> allCountStr = spiltStringData(primitiveCountData).ToList();
            List<int> allCount = new List<int>();
            allCountStr.ForEach(s =>
            {
                int.TryParse(s, out int value);
                allCount.Add(value);
            });

            XElement primitive = vertexWeights.Element(ns + "v");
            string primitiveIndicesData = primitive.Value;
            List<string> allValuesStr = spiltStringData(primitiveIndicesData).ToList();
            List<int> allValues = new List<int>();
            allValuesStr.ForEach(s =>
            {
                int.TryParse(s, out int value);
                allValues.Add(value);
            });

            Dictionary<FixedSemantics, List<int[]>> vertexWeightsPrimitiveDataSets = new Dictionary<FixedSemantics, List<int[]>>();

            Dictionary<FixedSemantics, List<int>> fixedSemanticsSerializedValues = new Dictionary<FixedSemantics, List<int>>();
            Dictionary<FixedSemantics, List<int>> serializedIndices = new Dictionary<FixedSemantics, List<int>>();
            foreach (KeyValuePair<int, FixedSemantics> vertexWeightsSource in vertexWeightSourceDic)
            {
                vertexWeightsPrimitiveDataSets[vertexWeightsSource.Value] = new List<int[]>();
                serializedIndices[vertexWeightsSource.Value] = new List<int>();
            }

            int semanticCount = 0;
            foreach (int value in allValues)
            {
                // Serialize the indices to each vertexWeightSource.
                if (semanticCount >= vertexWeightSourceDic.Count)
                    semanticCount = 0;

                serializedIndices[vertexWeightSourceDic[semanticCount]].Add(value);
                semanticCount++;
            }

            int currentPos = 0;
            foreach (int count in allCount)
            {
                if(count > 1)
                {

                }

                foreach (var vertexWeightsSource in vertexWeightSourceDic)
                {
                    int[] indices = new int[] { -1, -1, -1, -1 };
                    int[] tempIndices = serializedIndices[vertexWeightsSource.Value].Skip(currentPos).Take(count).ToArray();

                    if (tempIndices.Length >= 5)
                        throw new Exception("Cannot have more than 4 bone Indices with weights!");

                    for(int i = 0; i < tempIndices.Length; i++)
                    {
                        indices[i] = tempIndices[i];
                    }
                    vertexWeightsPrimitiveDataSets[vertexWeightsSource.Value].Add(indices);
                }
                currentPos += count;
            }

            /*
            int i = 0;
            foreach (string valueStr in allValues)
            {
                if (i >= vertexWeightSourceDic.Count)
                    i = 0;

                uint.TryParse(valueStr, out uint value);
                FixedSemantics source = vertexWeightSourceDic[i];

                vertexWeightsPrimitiveDataSets[source].Add(value);
                i++;
            }
            */

            return vertexWeightsPrimitiveDataSets;
        }

        private void writeVertexIndices(Dictionary<FixedSemantics, List<uint>> dataset, MemoryStream vertexIndicesStream, out int sizeWithoutPadding, out int triStripNo)
        {
            List<uint> trianglularsPrimitiveIndex = dataset[FixedSemantics.GEO_VERTEX];
            List<ushort> triStripPrimitiveIndex = parsePrimitiveVertexIndicesReversed(trianglularsPrimitiveIndex); // turn into tristrips

            for(int i = 0; i < triStripPrimitiveIndex.Count; i++)
            {
                ushort triStrip = triStripPrimitiveIndex[i];
                if(!((i == triStripPrimitiveIndex.Count - 1) && triStrip == 0xFFFF))
                {
                    appendUShortMemoryStream(vertexIndicesStream, triStrip, true);
                }
            }

            triStripNo = triStripPrimitiveIndex.Count;
            sizeWithoutPadding = (int)vertexIndicesStream.Length;
        }

        private List<ushort> parsePrimitiveVertexIndicesReversed(List<uint> triangularsPrimitiveIndex)
        {
            // TODO: clean up this messs
            string stripsexeSource = Path.Combine(Directory.GetCurrentDirectory(), @"3rd Party\Strips\");

            string inputPath = Path.Combine(Environment.CurrentDirectory, "Helpers", "inputPrimitiveTriangles.txt");
            string outputPath = Path.Combine(Environment.CurrentDirectory, "Helpers", "outputPrimitiveTriangleStrips.txt");

            // writing into input txt
            StringBuilder lineStr = new StringBuilder();
            StringBuilder baseStr = new StringBuilder();
            int count = 1;
            foreach (ushort value in triangularsPrimitiveIndex)
            {
                if (count <= 3)
                {
                    lineStr.Append(value);
                    lineStr.Append(" ");
                }
                else
                {
                    count = 1;
                    baseStr.AppendLine(lineStr.ToString());
                    lineStr = new StringBuilder();
                    lineStr.Append(value);
                    lineStr.Append(" ");
                }
                count++;
            }

            baseStr.AppendLine(lineStr.ToString());
            TextWriter writer = new StreamWriter(inputPath);
            writer.Write(baseStr.ToString());
            writer.Close();

            // using converter
            string arg = inputPath + " -i " + outputPath + " -o";
            using (Process strips = new Process())
            {
                strips.StartInfo.WorkingDirectory = stripsexeSource;
                strips.StartInfo.FileName = stripsexeSource + "Strips.exe";
                strips.StartInfo.UseShellExecute = false;
                strips.StartInfo.RedirectStandardInput = true;
                strips.StartInfo.RedirectStandardOutput = true;
                strips.StartInfo.CreateNoWindow = false;
                strips.Start();
                strips.StandardInput.WriteLine(arg);
                string output = strips.StandardOutput.ReadToEnd();
                strips.WaitForExit();
            }

            List<ushort> triStripPrimitives = new List<ushort>();
            StreamReader sr = new StreamReader(outputPath);

            string fline = sr.ReadLine();
            if(fline != null)
            {
                int.TryParse(fline.Split(':')[1], out int stripNo);

                string line = sr.ReadLine();
                while (line != null)
                {
                    string[] spilt = line.Split(' ').Skip(2).ToArray();
                    foreach (string primitive in spilt)
                    {
                        ushort.TryParse(primitive, out ushort res);
                        triStripPrimitives.Add(res);
                    }
                    triStripPrimitives.Add(0xFFFF);
                    line = sr.ReadLine();
                }
            }
            else
            {
                throw new Exception("tri_list to tri_strip vertIndices conversion failed! Possibly non-manifold vertex detected, please try reexporting the dae file inside a 3D model program.");
            }

            sr.Close();

            return triStripPrimitives;
        }

        private void writeVertexColorandUV(Dictionary<FixedSemantics, List<dynamic[]>> sourceDataset, MemoryStream vertexColorandUVStream, out int vertexColorandUVSizewithoutPadding)
        {
            List<dynamic[]> colorDataset = new List<dynamic[]>();
            List<dynamic[]> UVDataset = sourceDataset[FixedSemantics.GEO_TEXCOORD];
            if (sourceDataset.ContainsKey(FixedSemantics.GEO_COLOR))
            {
                //colorDataset = sourceDataset[FixedSemantics.GEO_COLOR];
                // If no vertex color info, create own.
                foreach (var s in UVDataset)
                {
                    dynamic[] createdColorSet = new dynamic[] { 127, 127, 127, -128 };
                    colorDataset.Add(createdColorSet);
                }
            }
            else
            {
                // If no vertex color info, create own.
                foreach(var s in UVDataset)
                {
                    dynamic[] createdColorSet = new dynamic[] { 127, 127, 127, -128 };
                    colorDataset.Add(createdColorSet);
                }
            }
            
            if (colorDataset.Count != UVDataset.Count)
                throw new NotImplementedException("Does not support different color & UV set count");

            var colorandUVDataset = colorDataset.Zip(UVDataset, (n, w) => new { colorDataset = n, UVDataset = w });

            foreach(var colorandUV in colorandUVDataset)
            {
                dynamic[] colorArr = colorandUV.colorDataset;
                var type = colorArr.First().GetType();
                if (type == typeof(float) || type == typeof(double))
                {
                    colorArr = convertColorFloatArraytoUShort(colorArr);
                    foreach (dynamic RGBA in colorArr)
                    {
                        vertexColorandUVStream.WriteByte((byte)RGBA);
                    }
                }
                else
                {
                    foreach (dynamic RGBA in colorArr)
                    {
                        vertexColorandUVStream.WriteByte((byte)RGBA);
                    }
                }

                dynamic[] UVArr = colorandUV.UVDataset;
                foreach(dynamic UV in UVArr)
                {
                    float UVValue = (float)UV;
                    if (UVValue < 0) // For flipped UVs
                        UVValue = 1f + UVValue;
                    ///if (UVValue > 1) // For repeating UVs
                    //    UVValue = UVValue - 1f;
                    //if (UVValue < -1) 
                    //    UVValue = 1f + UVValue;
                    short halfFloat = (short)FromFloat(UVValue);
                    byte[] convert = new byte[2];
                    BinaryPrimitives.WriteInt16BigEndian(convert, halfFloat);
                    vertexColorandUVStream.Write(convert, 0, convert.Length);
                }
            }

            vertexColorandUVSizewithoutPadding = (int)vertexColorandUVStream.Position;

            /*
            int requiredPadding = addPaddingSizeCalculation((int)vertexColorandUVStream.Position) - (int)vertexColorandUVStream.Position;
            for(int i = 0; i < requiredPadding; i++)
            {
                vertexColorandUVStream.WriteByte(0);
            }

            UVSizewithPadding = (int)vertexColorandUVStream.Position;
            */
        }

        private dynamic[] convertColorFloatArraytoUShort(dynamic[] color)
        {
            List<dynamic> converted = new List<dynamic>();
            foreach(float cl in color)
            {
                if(cl < 1)
                {
                    uint mul = (uint)Math.Round(cl * 255, 0);
                    if (mul > 255)
                    {
                        converted.Add(255);
                    }
                    else
                    {
                        converted.Add((byte)mul);
                    }
                }
                else
                {
                    converted = color.ToList();
                }
            }

            return converted.ToArray();
        }

        private void writeVertex(Dictionary<FixedSemantics, List<dynamic[]>> sourceDataset, Dictionary<FixedSemantics, List<uint>> primitiveDataSet, 
            Dictionary<FixedSemantics, List<dynamic[]>> contSourceDataset, Dictionary<FixedSemantics, List<int[]>> contPrimitiveDataSet, MemoryStream vertexDataStream, 
            Dictionary<string, int> VBNjointHierarchy, Dictionary<string, string> DAEjointHierarchy, out int vertexSize)
        {
            List<dynamic[]> posDataset = sourceDataset[FixedSemantics.GEO_POSITION];
            List<dynamic[]> nrmDataset = sourceDataset[FixedSemantics.GEO_NORMAL];
            List<dynamic[]> UVDataset = sourceDataset[FixedSemantics.GEO_TEXCOORD];
            List<uint> trianglesDataset = primitiveDataSet[FixedSemantics.GEO_VERTEX];
            List<dynamic[]> controllerJointsName = contSourceDataset[FixedSemantics.CONT_JOINT];
            List<dynamic[]> boneInvBindMat = contSourceDataset[FixedSemantics.CONT_INV_BIND_MATRIX];
            List<dynamic[]> boneWeightList = contSourceDataset[FixedSemantics.CONT_WEIGHT];
            List<int[]> boneVertexIndiceList = contPrimitiveDataSet[FixedSemantics.CONT_JOINT];
            List<int[]> boneWeightIndiceList = contPrimitiveDataSet[FixedSemantics.CONT_WEIGHT];

            // Convert to vector lists.
            List<Vector3> posDatasetVec = new List<Vector3>();
            List<Vector3> nrmDatasetVec = new List<Vector3>();
            List<Vector2> UVDatasetVec = new List<Vector2>();
            List<int> trianglesDatasetInt = new List<int>();

            byte[] oneBuffer = BitConverter.GetBytes(1);
            byte[] zeroFloatBuffer = BitConverter.GetBytes((float)0);
            byte[] oneFloatBuffer = BitConverter.GetBytes((float)1);
            Array.Reverse(oneBuffer);
            Array.Reverse(oneFloatBuffer);

            if (posDataset.Count != nrmDataset.Count)
                throw new Exception("Pos and Nrm dataset count not the same!");

            // We need to align the hierarchy order of bone Index between VBN and DAE since they have different order.
            List<string> VBNBoneNameList = VBNjointHierarchy.Keys.ToList();
            List<string> nodeHierarcyJointsName = DAEjointHierarchy.Keys.ToList();

            // Before we align the hierarchy between VBN and DAE, we need to align the index found in controller primitive with the Hierarchy found in DAE's joint nodes.
            alignDAEControllerIndexandJointHierarchy(controllerJointsName, nodeHierarcyJointsName, out Dictionary<int, int> DAEjointIndexConvertDic);

            // This is not used for exporting vbn cases, but the code is still run through as the code to write vertex indice is already coded with this in mind.
            alignDAEandVBNBoneIndices(VBNBoneNameList, nodeHierarcyJointsName, out Dictionary<int, int> VBNjointIndexConvertDic);

            // To iterate through both dic at once. 
            // https://stackoverflow.com/questions/1955766/iterate-two-lists-or-arrays-with-one-foreach-statement-in-c-sharp/1955780
            var posandnrm = posDataset.Zip(nrmDataset, (s, p) => new { posDataset = s, nrmDataset = p });
            var UVandposandnrm = posandnrm.Zip(UVDataset, (s, p) => new { posandnrm = s, UVDataset = p});

            foreach (var data in UVandposandnrm)
            {
                if (data.posandnrm.nrmDataset.Length != 3 || data.posandnrm.posDataset.Length != 3 || data.UVDataset.Length != 2)
                    throw new Exception("Pos or Nrm dataset does not comply XYZ specification");

                var posArr = data.posandnrm.posDataset;
                var nrmArr = data.posandnrm.nrmDataset;
                var UVArr = data.UVDataset;
                posDatasetVec.Add(new Vector3(posArr[0], posArr[1], posArr[2]));
                nrmDatasetVec.Add(new Vector3(nrmArr[0], nrmArr[1], nrmArr[2]));
                UVDatasetVec.Add(new Vector2(UVArr[0], UVArr[1]));
            }

            foreach(uint data in trianglesDataset)
            {
                trianglesDatasetInt.Add((int)data);
            }

            TriangleListUtils.CalculateTangentsBitangents(posDatasetVec, nrmDatasetVec, UVDatasetVec, trianglesDatasetInt, out Vector3[] tangents, out Vector3[] bitangents);

            for(int i = 0; i < posDataset.Count; i++)
            {
                foreach(dynamic pos in posDataset[i])
                {
                    byte[] floatBuffer = BitConverter.GetBytes((float)pos);
                    if(floatBuffer.Length != 4) { }
                    Array.Reverse(floatBuffer);
                    vertexDataStream.Write(floatBuffer, 0, floatBuffer.Length);
                }

                vertexDataStream.Write(oneFloatBuffer, 0, oneFloatBuffer.Length);

                foreach (dynamic nrm in nrmDataset[i])
                {
                    byte[] floatBuffer = BitConverter.GetBytes((float)nrm);
                    Array.Reverse(floatBuffer);
                    vertexDataStream.Write(floatBuffer, 0, floatBuffer.Length);
                }

                vertexDataStream.Write(oneFloatBuffer, 0, oneFloatBuffer.Length);

                for(int j = 0; j < 3; j++)
                {
                    byte[] floatBuffer = BitConverter.GetBytes((float)tangents[i][j]);
                    Array.Reverse(floatBuffer);
                    vertexDataStream.Write(floatBuffer, 0, floatBuffer.Length);
                }

                vertexDataStream.Write(oneFloatBuffer, 0, oneFloatBuffer.Length);

                for (int j = 0; j < 3; j++)
                {
                    byte[] floatBuffer = BitConverter.GetBytes((float)bitangents[i][j]);
                    Array.Reverse(floatBuffer);
                    vertexDataStream.Write(floatBuffer, 0, floatBuffer.Length);
                }

                vertexDataStream.Write(oneFloatBuffer, 0, oneFloatBuffer.Length);

                // Bone stuff, to be changed depends on source.

                int[] boneWeightIndices = boneWeightIndiceList[i];
                int[] boneVertexIndices = boneVertexIndiceList[i];

                var weightandIndices = boneWeightIndices.Zip(boneVertexIndices, (s, p) => new { boneWeightIndices = s, boneVertexIndices = p });

                float totalBoneWeight = 0f;
                foreach (var boneWeightandVertexIndex in weightandIndices)
                {
                    int boneWeightIndex = boneWeightandVertexIndex.boneWeightIndices;
                    // TODO: not use first.
                    dynamic boneWeight = boneWeightIndex == -1? 0f : boneWeightList[boneWeightIndex].First();

                    int boneVertexIndex = DAEjointIndexConvertDic[boneWeightandVertexIndex.boneVertexIndices];
                    if (boneVertexIndex == 0 || boneVertexIndex == 1 || boneVertexIndex == 2)
                    {

                    }

                    dynamic boneIndex = boneVertexIndex == -1 ? 0 : boneVertexIndex;
                    if (boneIndex >= VBNjointIndexConvertDic.Count)
                        boneIndex = 0; // is this ever called?
                    //TODO: make warnings

                    int convertedVertexIndex = VBNjointIndexConvertDic[boneIndex];

                    if(boneWeight > 0f && (convertedVertexIndex == 0 || convertedVertexIndex == 1 || convertedVertexIndex == 2))
                    {

                    }

                    appendIntMemoryStream(vertexDataStream, convertedVertexIndex, true);
                    vertexDataStream.Seek(0x0C, SeekOrigin.Current);

                    appendFloatMemoryStream(vertexDataStream, boneWeight, true);
                    vertexDataStream.Seek(-0x10, SeekOrigin.Current);

                    totalBoneWeight += boneWeight;
                }

                if (Math.Round(totalBoneWeight, 1) != 1f)
                    throw new Exception("Bone Weight != 1!");

                vertexDataStream.Seek(0x10, SeekOrigin.Current);
            }

            vertexSize = (int)vertexDataStream.Position;
        }

        private void alignDAEControllerIndexandJointHierarchy(List<dynamic[]> controllerJointsNameList, List<string> nodeHierarcyJointsName, out Dictionary<int, int> DAEjointIndexConvertDic)
        {
            DAEjointIndexConvertDic = new Dictionary<int, int>();
            DAEjointIndexConvertDic[-1] = -1;

            if (controllerJointsNameList.Count != nodeHierarcyJointsName.Count)
                throw new Exception("DAE Controller bone count mismatch with Node Hierarchy bone count!");

            for(int i = 0; i < controllerJointsNameList.Count; i++)
            {
                dynamic controllerJointsName = controllerJointsNameList[i].First();

                if (!nodeHierarcyJointsName.Contains(controllerJointsName))
                    throw new Exception("Can't find DAE controller bone name in Node Hierarchy!");

                int nodeHierarchyIndex = nodeHierarcyJointsName.IndexOf(controllerJointsName);
                DAEjointIndexConvertDic[i] = nodeHierarchyIndex;
            }
        }

        private void alignDAEandVBNBoneIndices(List<string> VBNBoneNameList, List<string> nodeHierarchyJointName, out Dictionary<int, int> jointConvertDic)
        {
            if (VBNBoneNameList.Count != nodeHierarchyJointName.Count)
                throw new Exception("DAE bone count mismatch with VBN bone count!");

            jointConvertDic = new Dictionary<int, int>();
            for(int i = 0; i < VBNBoneNameList.Count; i++)
            {
                string DAEBoneName = nodeHierarchyJointName[i];
                
                if (!VBNBoneNameList.Contains(DAEBoneName))
                    throw new Exception("Can't find DAE bone name in VBN Bone Name List!");

                int VBNIndex = VBNBoneNameList.IndexOf(DAEBoneName);
                jointConvertDic[i] = VBNIndex;
            }
        }

        /*
        private void parseJointsHierarchy(List<dynamic[]> boneInvBindMatList, List<dynamic[]> boneJointsNameList)
        {
            // Not needed, but retain this method for future implementation of VBN exporting.
            //https://gamedev.stackexchange.com/questions/28465/in-maya-how-do-i-convert-trans-rot-scale-data-to-a-4-x-4-transformation-matrix

            if (boneInvBindMatList.Count != boneJointsNameList.Count)
                throw new Exception("InvBindMatList count not equal to Joints count!");

            var boneInvBindMatandBoneJointsLists = boneInvBindMatList.Zip(boneJointsNameList, (s, p) => new { boneInvBindMat = s, boneJointsName = p });
            // converting array to Matrix4.
            Dictionary<string, Matrix4> boneInvBindMatDic = new Dictionary<string, Matrix4>();
            Dictionary<string, Matrix4> boneBindMatDic = new Dictionary<string, Matrix4>();
            foreach (var boneInvBindMatandBoneJoints in boneInvBindMatandBoneJointsLists)
            {
                Matrix4 invBindMatrix = new Matrix4();
                string jointName = boneInvBindMatandBoneJoints.boneJointsName.First();

                for (int row = 0; row < 4; row++)
                {
                    for (int column = 0; column < 4; column++)
                    {
                        invBindMatrix[row, column] = (float)boneInvBindMatandBoneJoints.boneInvBindMat[(row * 4) + column];
                    }
                }

                boneInvBindMatDic[jointName] = invBindMatrix;

                try
                {
                    Matrix4.Invert(invBindMatrix);
                }
                catch
                {
                    throw new Exception("inverse Bind Matrix failed!");
                }

                boneBindMatDic[jointName] = invBindMatrix;
            }

            // assuming first mat is always identity (root of the bones)
            Matrix4 idendityMat = Matrix4.Identity;
            Matrix4 firstJointBindMat = boneBindMatDic.ElementAt(0).Value;
            string firstJointName = boneJointsNameList.First().First();

            if (firstJointBindMat != idendityMat)
                throw new Exception("First bind matrix is not identity! (non root)");

            // assuming second mat is always child to root. Since first mat is identity, the transform and bind mat is inversed of each other.
            Matrix4 secondJointBindMat = boneInvBindMatDic.ElementAt(1).Value;
            string secondJointName = boneJointsNameList.ElementAt(1).First();

            Dictionary<string, Matrix4> boneTransMat = new Dictionary<string, Matrix4>();
            boneTransMat[firstJointName] = firstJointBindMat;
            boneTransMat[secondJointName] = secondJointBindMat;

            Dictionary<string, string> boneHierarchy = new Dictionary<string, string>();
            boneHierarchy[firstJointName] = "root"; // root indicates the parent is nil, this bone / joint is the parent to all.
            boneHierarchy[secondJointName] = firstJointName;

            // Start iterate from third mat.
            for (int jointCount = 2; jointCount < boneJointsNameList.Count; jointCount++)
            {
                foreach (var parentBoneTransMat in boneTransMat)
                {
                    //checkIfParent(boneBindMatDic.ElementAt(jointCount).Value, parentBoneTransMat.Value);
                }
            }

            //http://wazim.com/collada-tutorial1/
        }
        */

        private void parseVisualScenetypeJoint(XElement[] jointNodes, Dictionary<string, string> jointHierarchy, Dictionary<string, Matrix4> jointPosDic, bool isRootJoint)
        {
            foreach (XElement jointNode in jointNodes)
            {
                string jointName = jointNode.Attribute("sid").Value;

                XElement matrix = jointNode.Element(ns + "matrix");

                if (matrix.Attribute("sid").Value != "transform")
                    throw new Exception("VisualScenetypeJoint node's matrix is not transform!");

                string[] matrixString = spiltStringData(matrix.Value).ToArray();

                if (matrixString.Length != 16)
                    throw new Exception("Transform Matrix length not 16!");

                Matrix4 transformMatrix = convertArraytoMatrix4(matrixString);
                jointPosDic[jointName] = transformMatrix;

                XElement[] childNodes = jointNode.Elements(ns + "node").ToArray();

                if(!isRootJoint)
                    jointHierarchy[jointName] = jointNode.Parent.Attribute("sid").Value;

                parseVisualScenetypeJoint(childNodes, jointHierarchy, jointPosDic, false);
            }
        }

        // TODO: add proper material support
        private void writeMaterials(MemoryStream materialStream)
        {
            // This method is just used to fill the spaces so that the format is correct. Material hex editing needs to be done manually or else the file will not work.
            appendUIntMemoryStream(materialStream, 0x12140000, true); // Flags is changing constantly. I have no idea, I will just 1214 fornow. 1245 makes it glow
            appendZeroMemoryStream(materialStream, 4); // Unknown 0 bytes
            appendZeroMemoryStream(materialStream, 2); // should be SrcFactor as per Smash Forge. 
            appendUShortMemoryStream(materialStream, 0x0003, true); // number of texture files, set it to 3 as per default.
            appendZeroMemoryStream(materialStream, 6); // check smash forge for the actual proper metadata. Most of them is 0 in FB
            appendUShortMemoryStream(materialStream, 0x0405, true);
            appendZeroMemoryStream(materialStream, 0x0C);

            // Start of texture.
            for (int i = 0; i < 3; i++)
            {
                appendUIntMemoryStream(materialStream, 0xFFFFFFFF, true); // Hash Name of the DDS file. Need manual hex editing.
                appendZeroMemoryStream(materialStream, 8);
                appendUIntMemoryStream(materialStream, 0x03030302, true);
                appendUIntMemoryStream(materialStream, 0x06000400, true);
                appendZeroMemoryStream(materialStream, 4);
            }

            appendZeroMemoryStream(materialStream, 0x10);
        }

        // Utilities
        private Matrix4 convertArraytoMatrix4(string[] data)
        {
            int count = 0, columnCount = 0;
            Matrix4 matrix = new Matrix4();
            Vector4 column = new Vector4();
            foreach (string value in data)
            {
                float.TryParse(value, out float result);
                if(count < 4)
                {
                    column[count] = result;
                    count++;
                }
                
                if(count >= 4)
                {
                    count = 0;
                    switch (columnCount)
                    {
                        case 0:
                            matrix.Column0 = column;
                            break;
                        case 1:
                            matrix.Column1 = column;
                            break;
                        case 2:
                            matrix.Column2 = column;
                            break;
                        case 3:
                            matrix.Column3 = column;
                            break;
                        default:
                            break;
                    }
                    columnCount++;
                    column = new Vector4();
                }
            }
            return matrix;
        }

        private Matrix4 convertArraytoMatrix4(float[] data)
        {
            int count = 0, columnCount = 0;
            Matrix4 matrix = new Matrix4();
            Vector4 column = new Vector4();
            foreach (float value in data)
            {
                if (count < 4)
                {
                    column[count] = value;
                    count++;
                }

                if (count >= 4)
                {
                    count = 0;
                    switch (columnCount)
                    {
                        case 0:
                            matrix.Column0 = column;
                            break;
                        case 1:
                            matrix.Column1 = column;
                            break;
                        case 2:
                            matrix.Column2 = column;
                            break;
                        case 3:
                            matrix.Column3 = column;
                            break;
                        default:
                            break;
                    }
                    columnCount++;
                    column = new Vector4();
                }
            }
            return matrix;
        }

        // Decomposing matrix code taken from SM64DSe
        // https://github.com/Arisotura/SM64DSe
        // Helper.cs
        public static void DecomposeSRTMatrix(Matrix4 matrix, out Vector3 scale, out Vector3 rotation, out Vector3 translation)
        {
            Quaternion quat;
            Decompose(matrix, out scale, out quat, out translation);
            matrix.Row0 = new Vector4(Vector3.Divide(matrix.Row0.Xyz, scale.X), 0);
            matrix.Row1 = new Vector4(Vector3.Divide(matrix.Row1.Xyz, scale.Y), 0);
            matrix.Row2 = new Vector4(Vector3.Divide(matrix.Row2.Xyz, scale.Z), 0);
            matrix.Row3 = new Vector4(0, 0, 0, 1);
            rotation = FromRotMatToEulerZYXInt(matrix);
        }

        /// <summary>
        /// Decomposes a matrix into a scale, rotation, and translation.
        /// </summary>
        /// <param name="scale">When the method completes, contains the scaling component of the decomposed matrix.</param>
        /// <param name="rotation">When the method completes, contains the rotation component of the decomposed matrix.</param>
        /// <param name="translation">When the method completes, contains the translation component of the decomposed matrix.</param>
        /// <remarks>
        /// This method is designed to decompose an SRT transformation matrix only.
        /// </remarks>
        private static bool Decompose(Matrix4 matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation)
        {
            //Source: Unknown
            //References: http://www.gamedev.net/community/forums/topic.asp?topic_id=441695

            //Get the translation.
            translation.X = matrix.M41;
            translation.Y = matrix.M42;
            translation.Z = matrix.M43;

            //Scaling is the length of the rows.
            scale.X = (float)Math.Sqrt((matrix.M11 * matrix.M11) + (matrix.M12 * matrix.M12) + (matrix.M13 * matrix.M13));
            scale.Y = (float)Math.Sqrt((matrix.M21 * matrix.M21) + (matrix.M22 * matrix.M22) + (matrix.M23 * matrix.M23));
            scale.Z = (float)Math.Sqrt((matrix.M31 * matrix.M31) + (matrix.M32 * matrix.M32) + (matrix.M33 * matrix.M33));

            //If any of the scaling factors are zero, than the rotation matrix can not exist.
            if (scale.X == 0.0f ||
                scale.Y == 0.0f ||
                scale.Z == 0.0f)
            {
                rotation = Quaternion.Identity;
                return false;
            }

            //The rotation is the left over matrix after dividing out the scaling.
            Matrix4 rotationmatrix = new Matrix4();
            rotationmatrix.M11 = matrix.M11 / scale.X;
            rotationmatrix.M12 = matrix.M12 / scale.X;
            rotationmatrix.M13 = matrix.M13 / scale.X;

            rotationmatrix.M21 = matrix.M21 / scale.Y;
            rotationmatrix.M22 = matrix.M22 / scale.Y;
            rotationmatrix.M23 = matrix.M23 / scale.Y;

            rotationmatrix.M31 = matrix.M31 / scale.Z;
            rotationmatrix.M32 = matrix.M32 / scale.Z;
            rotationmatrix.M33 = matrix.M33 / scale.Z;

            rotationmatrix.M44 = 1f;

            RotationMatrix(ref rotationmatrix, out rotation);
            return true;
        }

        /// <summary>
        /// Creates a quaternion given a rotation matrix.
        /// </summary>
        /// <param name="matrix">The rotation matrix.</param>
        /// <param name="result">When the method completes, contains the newly created quaternion.</param>
        private static void RotationMatrix(ref Matrix4 matrix, out Quaternion result)
        {
            float sqrt;
            float half;
            float scale = matrix.M11 + matrix.M22 + matrix.M33;
            result = new Quaternion();

            if (scale > 0.0f)
            {
                sqrt = (float)Math.Sqrt(scale + 1.0f);
                result.W = sqrt * 0.5f;
                sqrt = 0.5f / sqrt;

                result.X = (matrix.M23 - matrix.M32) * sqrt;
                result.Y = (matrix.M31 - matrix.M13) * sqrt;
                result.Z = (matrix.M12 - matrix.M21) * sqrt;
            }
            else if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
            {
                sqrt = (float)Math.Sqrt(1.0f + matrix.M11 - matrix.M22 - matrix.M33);
                half = 0.5f / sqrt;

                result.X = 0.5f * sqrt;
                result.Y = (matrix.M12 + matrix.M21) * half;
                result.Z = (matrix.M13 + matrix.M31) * half;
                result.W = (matrix.M23 - matrix.M32) * half;
            }
            else if (matrix.M22 > matrix.M33)
            {
                sqrt = (float)Math.Sqrt(1.0f + matrix.M22 - matrix.M11 - matrix.M33);
                half = 0.5f / sqrt;

                result.X = (matrix.M21 + matrix.M12) * half;
                result.Y = 0.5f * sqrt;
                result.Z = (matrix.M32 + matrix.M23) * half;
                result.W = (matrix.M31 - matrix.M13) * half;
            }
            else
            {
                sqrt = (float)Math.Sqrt(1.0f + matrix.M33 - matrix.M11 - matrix.M22);
                half = 0.5f / sqrt;

                result.X = (matrix.M31 + matrix.M13) * half;
                result.Y = (matrix.M32 + matrix.M23) * half;
                result.Z = 0.5f * sqrt;
                result.W = (matrix.M12 - matrix.M21) * half;
            }
        }

        public const float Tau = 2.0f * (float)Math.PI;
        public const float Deg2Rad = (float)(Tau / 360.0f);
        public const float Rad2Deg = (float)(360.0f / Tau);

        public static Vector3 FromRotMatToEulerZYXInt(Matrix4 mat)
        {
            //x''', y''', z''' are stored in rows of mat
            Vector3 angles = new Vector3(0, 0, 0);

            angles.Y = (float)-Math.Asin(mat.Row0.Z);
            if (Math.Abs(angles.Y) * 0x10000 / Tau > (float)0x4000 - 0.5)
            {
                angles.Z = 0;
                angles.X = (float)Math.Atan2(-mat.Row2.Y, mat.Row1.Y);
            }
            else
            {
                angles.Z = (float)Math.Atan2(mat.Row0.Y, mat.Row0.X);
                angles.X = (float)Math.Atan2(mat.Row1.Z, mat.Row2.Z);
            }

            //Whew!
            return angles;
        }

        private IEnumerable<string> spiltStringData(string input)
        {
            string withoutNewLine = input.Replace("\n", " ");
            IEnumerable<string> allValues = withoutNewLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).AsEnumerable();
            return allValues;
        }

        public static int FromFloat(float fval)
        {
            int fbits = BitConverter.ToInt32(BitConverter.GetBytes(fval), 0);
            int sign = fbits >> 16 & 0x8000;          // sign only
            int val = (fbits & 0x7fffffff) + 0x1000; // rounded value

            if (val >= 0x47800000)               // might be or become NaN/Inf
            {                                     // avoid Inf due to rounding
                if ((fbits & 0x7fffffff) >= 0x47800000)
                {                                 // is or must become NaN/Inf
                    if (val < 0x7f800000)        // was value but too large
                        return sign | 0x7c00;     // make it +/-Inf
                    return sign | 0x7c00 |        // remains +/-Inf or NaN
                        (fbits & 0x007fffff) >> 13; // keep NaN (and Inf) bits
                }
                return sign | 0x7bff;             // unrounded not quite Inf
            }
            if (val >= 0x38800000)               // remains normalized value
                return sign | val - 0x38000000 >> 13; // exp - 127 + 15
            if (val < 0x33000000)                // too small for subnormal
                return sign;                      // becomes +/-0
            val = (fbits & 0x7fffffff) >> 23;  // tmp exp for subnormal calc
            return sign | ((fbits & 0x7fffff | 0x800000) // add subnormal bit
                + (0x800000 >> val - 102)     // round depending on cut off
                >> 126 - val);   // div by 2^(1-(exp-127+15)) and >> 13 | exp=0
        }

        #endregion NUDtoDAE
    }
}
