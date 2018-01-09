using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Ionic.Zlib;

namespace Mitsuba {
	class MeshStore {
		private const ushort MTS_FILEFORMAT_HEADER     = 0x041C;
		private const ushort MTS_FILEFORMAT_VERSION_V4 = 0x04;
		private const uint   MTS_HAS_NORMALS           = 0x0001;
		private const uint   MTS_HAS_TEXCOORDS         = 0x0002;
		private const uint   MTS_HAS_COLORS            = 0x0008;
		private const uint   MTS_FACE_NORMALS          = 0x0010;
		private const uint   MTS_SINGLE_PRECISION      = 0x0100;
		private const uint   MTS_DOUBLE_PRECISION      = 0x0200;

		private string basePath;
		private string filename;
        private bool writeSerialized;
		private FileStream output;
		private int meshCount;
		private List<ulong> meshDict;

		public MeshStore(string basePath, bool serialized) {
			this.basePath = basePath;
            this.writeSerialized = serialized;
		}

		public string Filename {
			get { return filename; }
		}

        public bool WriteSerialized {
            get { return writeSerialized; }
        }

		public void Create(string filename) {
			output = new FileStream(Path.Combine(basePath, filename), FileMode.Create);
			this.filename = filename;
			meshDict = new List<ulong>();
			meshCount = 0;
		}

        public int StoreSerialized(Mesh mesh, string name) {
            MeshFaceList faces = mesh.Faces;
            MeshVertexList vertices = mesh.Vertices;
            MeshVertexNormalList normals = mesh.Normals;
            MeshTextureCoordinateList texCoords = mesh.TextureCoordinates;

            faces.ConvertQuadsToTriangles();

            int vertexCount = vertices.Count;
            int triangleCount = faces.TriangleCount;
            Log("MeshStore[" + meshCount + "]: adding mesh with " + vertexCount + " vertices, " + triangleCount + " triangles"
                + (name.Length > 0 ? (" (\"" + name + "\")") : ""));

            meshDict.Add((ulong)output.Position);
            Serialize(output, MTS_FILEFORMAT_HEADER);
            Serialize(output, MTS_FILEFORMAT_VERSION_V4);

            Stream zStream = new ZlibStream(output, CompressionMode.Compress,
                CompressionLevel.BestCompression, true);

            uint flags = MTS_SINGLE_PRECISION;
            if (texCoords.Count > 0)
                flags |= MTS_HAS_TEXCOORDS;
            if (normals.Count > 0)
                flags |= MTS_HAS_NORMALS;

            Serialize(zStream, flags);
            Serialize(zStream, name);
            Serialize(zStream, (ulong)vertexCount);
            Serialize(zStream, (ulong)triangleCount);

            for (int i = 0; i < vertexCount; ++i)
            {
                Point3f p = vertices[i];
                Serialize(zStream, p.X); Serialize(zStream, p.Y); Serialize(zStream, p.Z);
            }

            if (normals.Count > 0)
            {
                for (int i = 0; i < vertexCount; ++i)
                {
                    Vector3f n = normals[i];
                    Serialize(zStream, n.X); Serialize(zStream, n.Y); Serialize(zStream, n.Z);
                }
            }

            if (texCoords.Count > 0)
            {
                for (int i = 0; i < vertexCount; ++i)
                {
                    Point2f uv = texCoords[i];
                    Serialize(zStream, uv.X); Serialize(zStream, uv.Y);
                }
            }

            for (int i = 0; i < triangleCount; ++i)
            {
                MeshFace face = faces[i];
                if (!face.IsTriangle)
                    throw new Exception("Internal error: expected a triangle face!");
                Serialize(zStream, face.A); Serialize(zStream, face.B); Serialize(zStream, face.C);
            }
            zStream.Close();
            
            return meshCount++;
        }

        public String StoreOBJ(Mesh mesh, string name) {
            MeshFaceList faces = mesh.Faces;
            MeshVertexList vertices = mesh.Vertices;
            MeshVertexNormalList normals = mesh.Normals;
            MeshTextureCoordinateList texCoords = mesh.TextureCoordinates;

            faces.ConvertQuadsToTriangles();

            if (name.Length <= 0) {
                name = "mesh" + meshCount.ToString();
            }

            int vertexCount = vertices.Count;
            int triangleCount = faces.TriangleCount;
            Log("MeshStore[" + meshCount + "]: saving OBJ mesh with " + vertexCount + " vertices, " 
                + triangleCount + " triangles" + "\"" + name + "\")");


            //meshDict.Add((ulong)output.Position);
            //Serialize(output, MTS_FILEFORMAT_HEADER);
            //Serialize(output, MTS_FILEFORMAT_VERSION_V4);

            String filename = name + ".obj";
            FileStream output = new FileStream(Path.Combine(basePath, filename), FileMode.Create);
            StreamWriter stream = new StreamWriter(output);

            stream.WriteLine("# Object " + name);
            stream.WriteLine("# Generated by the Mitsuba Rhino plugin.");
            stream.WriteLine("# Vertices: " + vertexCount.ToString());
            stream.WriteLine("# Faces: " + triangleCount.ToString());

            for (int i = 0; i < vertexCount; ++i) {
                Point3f p = vertices[i];
                stream.WriteLine("v " + p.X.ToString() + " " + p.Y.ToString() + " " + p.Z.ToString());
            }

            if (normals.Count > 0) {
                for (int i = 0; i < vertexCount; ++i) {
                    Vector3f n = normals[i];
                    stream.WriteLine("vn " + n.X.ToString() + " " + n.Y.ToString() + " " + n.Z.ToString());
                }
            }

            if (texCoords.Count > 0) {
                for (int i = 0; i < vertexCount; ++i) {
                    Point2f uv = texCoords[i];
                    stream.WriteLine("vt " + uv.X.ToString() + " " + uv.Y.ToString());
                }
            }

            for (int i = 0; i < triangleCount; ++i) {
                MeshFace face = faces[i];
                if (!face.IsTriangle)
                    throw new Exception("Internal error: expected a triangle face!");
                String a = (face.A + 1).ToString();
                String b = (face.B + 1).ToString();
                String c = (face.C + 1).ToString();
                stream.WriteLine("f " + a + "/" + a + "/" + a + " "
                                      + b + "/" + b + "/" + b + " "
                                      + c + "/" + c + "/" + c);
            }

            stream.Close();
            meshCount++;
            return filename;
        }

		public void Close() {
			// Write the dictionary
			foreach (ulong entry in meshDict)
				Serialize(output, entry);

			Serialize(output, (uint) meshDict.Count);
			output.Close();
			output = null;
		}

		public void Serialize(Stream stream, object value) {
			byte[] result;
			Type type = value.GetType();

			if (type == typeof(string)) {
				/* Strings are handled differently */
				byte[] encodedBytes = System.Text.Encoding.UTF8.GetBytes((string) value);
				result = new byte[] { (byte) 0 };
				stream.Write(encodedBytes, 0, encodedBytes.Length);
				stream.Write(result, 0, result.Length);
				return;
			}

			if (type  == typeof(byte))
				result = new byte[] { (byte)value };
			else if (type == typeof(ushort))
				result = BitConverter.GetBytes((ushort) value);
			else if (type == typeof(int))
				result = BitConverter.GetBytes((int) value);
			else if (type == typeof(uint))
				result = BitConverter.GetBytes((uint) value);
			else if (type == typeof(ulong))
				result = BitConverter.GetBytes((ulong) value);
			else if (type == typeof(float))
				result = BitConverter.GetBytes((float) value);
			else
				throw new Exception("MeshStore::serialize(): unsupported data type!");

			if (!BitConverter.IsLittleEndian)
				Array.Reverse(result);

			stream.Write(result, 0, result.Length);
		}

		void Log(string text) {
			RhinoApp.WriteLine(text);
		}
	}
}
