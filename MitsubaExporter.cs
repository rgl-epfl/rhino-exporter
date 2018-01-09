﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Diagnostics;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Display;
using System.Globalization;

namespace Mitsuba {
    class MitsubaExporter {
		private const string MTS_VERSION = "0.4.0";
		private string m_basePath, m_filename;
		private XmlDocument m_xmlDocument;
		private MitsubaSettings m_settings;
		private MeshStore m_meshStore;
		private int m_idCounter = 0;
		private Dictionary<Guid, string> m_idMap;
		private Dictionary<int, string> m_xmlIdMap;
		private HashSet<string> m_materialNames;

		public MitsubaExporter(MitsubaSettings settings, string basePath, string filename) {
			m_settings = settings;
			m_basePath = basePath;
			m_filename = filename;
			m_meshStore = new MeshStore(basePath, m_settings.writeSerialized);
		}

		public void Export(RhinoDoc doc) {
			/* Measure the time spent in Export() */
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			/* Create an empty scene DOM representation */
			m_xmlDocument = new XmlDocument();
			m_xmlDocument.LoadXml("<?xml version=\"1.0\"?> <scene version=\"" + MTS_VERSION + "\"/>");
			XmlElement docRoot = m_xmlDocument.DocumentElement;

			m_meshStore.Create(Path.GetFileNameWithoutExtension(m_filename) + ".serialized");
			string sceneFile = Path.Combine(m_basePath, m_filename);

			try {
				m_idMap = new Dictionary<Guid, string>();
				m_xmlIdMap = new Dictionary<int, string>();
				m_materialNames = new HashSet<string>();

				XmlElement defaultEmitter = m_xmlDocument.CreateElement("emitter");
				XmlElement emitterTrafo = m_xmlDocument.CreateElement("transform");
				XmlElement rotateTrafo = m_xmlDocument.CreateElement("rotate");
				XmlElement extendProperty = m_xmlDocument.CreateElement("boolean");
				XmlElement sunScale = m_xmlDocument.CreateElement("float");
				extendProperty.SetAttribute("name", "extend");
				extendProperty.SetAttribute("value", "true");
				sunScale.SetAttribute("name", "sunRadiusScale");
				sunScale.SetAttribute("value", "10");
				defaultEmitter.SetAttribute("type", "sunsky");
				emitterTrafo.SetAttribute("name", "toWorld");
				rotateTrafo.SetAttribute("x", "1");
				rotateTrafo.SetAttribute("angle", "90");
				emitterTrafo.AppendChild(rotateTrafo);
				defaultEmitter.AppendChild(emitterTrafo);
				defaultEmitter.AppendChild(extendProperty);
				defaultEmitter.AppendChild(sunScale);
				docRoot.AppendChild(defaultEmitter);

				/* Export the integrator */
				ExportIntegrator(docRoot);

				/* Export materials */
				for (int i = 0; i < doc.Materials.Count; ++i) {
					Log("Exporting material '" + doc.Materials[i].Name + "'");
					ExportMaterial(docRoot, doc, i);
				}

				/* Export instance defitions and references */
				foreach (InstanceDefinition idef in doc.InstanceDefinitions) {
					Log("Exporting instance definition '" + idef.Name + "'");
					ExportInstanceDef(docRoot, idef);
				}

				RhinoObject[] instanceRefs = doc.Objects.FindByObjectType(ObjectType.InstanceReference);
				foreach (RhinoObject o in instanceRefs) {
					if (o.Name.Length > 0)
						Log("Exporting instance reference '" + o.Name + "'");
					ExportInstanceRef(docRoot, (InstanceObject) o);
				}

				/* Export the remaining geometry */
				List<RhinoObject> objects =
					new List<RhinoObject>(doc.Objects.FindByObjectType(ObjectType.Brep));
				objects.AddRange(doc.Objects.FindByObjectType(ObjectType.Extrusion));
				objects.AddRange(doc.Objects.FindByObjectType(ObjectType.Mesh));

				foreach (RhinoObject o in objects) {
                    ExportRenderMesh(docRoot, o, false);  // false: not serialized representation
				}

				/* Create a sensor for the current view */
				ExportSensor(docRoot, doc.Views.ActiveView.ActiveViewport);

				/* Write the XML scene document */
				FileStream output = new FileStream(sceneFile, FileMode.Create);
				StreamWriter sw = new StreamWriter(output);
				XmlTextWriter xmlWriter = new XmlTextWriter(sw);
				xmlWriter.Formatting = Formatting.Indented;
				xmlWriter.Indentation = 4;
				m_xmlDocument.WriteTo(xmlWriter);
				sw.Close();
				output.Close();
				stopwatch.Stop();
				Log("Export is done (took " + stopwatch.ElapsedMilliseconds + " ms)");

				if (m_settings.rendererDirectory.Length > 0) {
					Process proc = new Process();
					proc.StartInfo.FileName = Path.Combine(m_settings.rendererDirectory, "mtsgui.exe");
					proc.StartInfo.Arguments = "\"" + sceneFile + "\"";
					proc.StartInfo.WorkingDirectory = m_basePath;
					proc.Start();
				} else {
					Log("Warning: Unable to launch Mitsuba: please set the renderer directory in the Rhino options.");
				}
			} finally {
				m_meshStore.Close();
			}
		}

		private bool ExportIntegrator(XmlElement parent) {
			string type;
			switch (m_settings.integrator) {
				case MitsubaSettings.Integrator.EVeachMLT: type = "mlt"; break;
				case MitsubaSettings.Integrator.EKelemenMLT: type = "pssmlt"; break;
				case MitsubaSettings.Integrator.EPathTracer: type = "path"; break;
				case MitsubaSettings.Integrator.EBidirectional: type = "bdpt"; break;
				case MitsubaSettings.Integrator.EDirectIllumination: type = "direct"; break;
				case MitsubaSettings.Integrator.EAdjointParticleTracer: type = "ptracer"; break;
				case MitsubaSettings.Integrator.EERPT: type = "erpt"; break;
				default:
					throw new Exception("Unknown integrator type!");
			}

			XmlElement integrator = m_xmlDocument.CreateElement("integrator");
			integrator.SetAttribute("type", type);
			if (m_settings.integrator != MitsubaSettings.Integrator.EDirectIllumination)
				integrator.AppendChild(MakeProperty("maxDepth", m_settings.pathLength));

			parent.AppendChild(integrator);
			return true;
		}


		private bool ExportMaterial(XmlElement parent, RhinoDoc doc, int idx) {
			Material mat = doc.Materials[idx];

			if (mat.UseCount == 0)
				return false;

			/* Super-lame material exporter for now */
			string matType = "diffuse";
			if (mat.Transparency == 1.0) {
				matType = "dielectric";
			} else if (mat.Shine > 0) {
				matType = "conductor";
			}
			Log(" -> type = " + matType);

			string materialName = mat.Name.Length > 0 ? 
				mat.Name : "Unnamed material";

			int counter = 1;
			while (m_materialNames.Contains(materialName))
				materialName = mat.Name + "_" + counter++;
			m_materialNames.Add(materialName);
			m_xmlIdMap[idx] = materialName;

			XmlElement bsdfElement = m_xmlDocument.CreateElement("bsdf");
			bsdfElement.SetAttribute("type", matType);
			bsdfElement.SetAttribute("id", materialName);
			parent.AppendChild(bsdfElement);

			return true;
		}


		/// <summary>
		/// Export an a perspective or orthographic sensor
		/// </summary>
		/// <param name="parent">The parent node in the output XML document</param>
		/// <param name="obj">The RhinoViewport instance to be exported</param>
		/// <returns>true if any content was exported</returns>
		private bool ExportSensor(XmlElement parent, RhinoViewport view) {
			bool perspective = view.IsPerspectiveProjection;
			bool orthographic = view.IsParallelProjection;

			if (!perspective && !orthographic) {
				Log("Warning: camera type not supported -- ignoring.");
				return false;
			}

			double left, right, bottom, top, near, far;
			view.GetFrustum(out left, out right, out bottom, out top, out near, out far);

			XmlElement sensorElement = m_xmlDocument.CreateElement("sensor");
			sensorElement.SetAttribute("type", perspective ? "perspective" : "orthographic");
			Transform toWorld = view.GetTransform(CoordinateSystem.Camera, CoordinateSystem.World);
			toWorld = toWorld * Transform.Mirror(new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, -1)));
			toWorld = toWorld * Transform.Mirror(new Plane(new Point3d(0, 0, 0), new Vector3d(-1, 0, 0)));
			XmlElement toWorldElement = MakeProperty("toWorld", toWorld);

			if (perspective) {
				double focusDistance = view.CameraLocation.DistanceTo(view.CameraTarget);
				double halfDiag, halfVert, halfHoriz;
				view.GetCameraAngle(out halfDiag, out halfVert, out halfHoriz);
				toWorld = toWorld * Transform.Mirror(new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, -1)));
				sensorElement.AppendChild(MakeProperty("fovAxis", "diagonal"));
				sensorElement.AppendChild(MakeProperty("fov", 2 * halfDiag * 180 / Math.PI));
				sensorElement.AppendChild(MakeProperty("focusDistance", focusDistance));
			} else {
				XmlElement scaleNode = m_xmlDocument.CreateElement("scale");
				double scale = (right - left)/2;
				scaleNode.SetAttribute("x", ToStringHelper(scale));
				scaleNode.SetAttribute("y", ToStringHelper(scale));
				toWorldElement.PrependChild(scaleNode);
			}

			// Some extra room for nativating using the interactive viewer
			near /= 10; far *= 10;

			sensorElement.AppendChild(toWorldElement);
			sensorElement.AppendChild(MakeProperty("nearClip", near));
			sensorElement.AppendChild(MakeProperty("farClip", far));

			XmlElement filmElement = m_xmlDocument.CreateElement("film");
			filmElement.SetAttribute("type", "hdrfilm");
			filmElement.AppendChild(MakeProperty("width", m_settings.xres));
			filmElement.AppendChild(MakeProperty("height", m_settings.yres));
			sensorElement.AppendChild(filmElement);

			bool independentSampler = false;
			if (m_settings.integrator == MitsubaSettings.Integrator.EAdjointParticleTracer ||
				m_settings.integrator == MitsubaSettings.Integrator.EKelemenMLT ||
				m_settings.integrator == MitsubaSettings.Integrator.EVeachMLT) {
				/* These integrators require the independent sampler */
				independentSampler = true;
			}

			XmlElement samplerElement = m_xmlDocument.CreateElement("sampler");
			samplerElement.SetAttribute("type", independentSampler ? "independent" : "ldsampler");
			samplerElement.AppendChild(MakeProperty("sampleCount", m_settings.samplesPerPixel));
			sensorElement.AppendChild(samplerElement);

			parent.AppendChild(sensorElement);
			return true;
		}

		/// <summary>
		/// Export an entire Rhino instance definition by turning it into a Mitsuba shapegroup
		/// </summary>
		/// <param name="parent">The parent node in the output XML document</param>
		/// <param name="obj">The InstanceDefinition instance to be exported</param>
		/// <returns>true if any content was exported</returns>
		private bool ExportInstanceDef(XmlElement parent, InstanceDefinition idef) {
			if (!idef.InUse(1))
				return false;

			string id = GetID("group");
			m_idMap.Add(idef.Id, id);

			XmlElement shapeElement = m_xmlDocument.CreateElement("shape");
			if (idef.Name.Length > 0)
				shapeElement.AppendChild(m_xmlDocument.CreateComment(" Rhino object '" + idef.Name + "' "));

			shapeElement.SetAttribute("type", "shapegroup");
			shapeElement.SetAttribute("id", id);

			RhinoObject[] objects = idef.GetObjects();

			bool success = false;
			foreach (RhinoObject o in objects)
				success |= ExportRenderMesh(shapeElement, o, false);

			if (success)
				parent.AppendChild(shapeElement);
			return success;
		}

		/// <summary>
		/// Export an a Rhino instance reference using the 'instance' plugin
		/// </summary>
		/// <param name="parent">The parent node in the output XML document</param>
		/// <param name="obj">The InstanceObject instance to be exported</param>
		/// <returns>true if any content was exported</returns>
		private bool ExportInstanceRef(XmlElement parent, InstanceObject inst) {
			Guid guid = inst.InstanceDefinition.Id;

			if (!m_idMap.ContainsKey(guid)) {
				Log("Warning: no content found -- perhaps the instance definition was empty?");
				return false;
			}

			XmlElement shapeElement = m_xmlDocument.CreateElement("shape");
			if (inst.Name.Length > 0)
				shapeElement.AppendChild(m_xmlDocument.CreateComment(" Rhino object '" + inst.Name + "' "));
			if (inst.InstanceDefinition.Name.Length > 0)
				shapeElement.AppendChild(m_xmlDocument.CreateComment(" (references '" 
					+ inst.InstanceDefinition.Name + "') "));

			shapeElement.SetAttribute("type", "instance");

			string id = m_idMap[guid];
			shapeElement.AppendChild(MakeReference(id));
			parent.AppendChild(shapeElement);
			shapeElement.AppendChild(MakeProperty("toWorld", inst.InstanceXform));
			return true;
		}

		/// <summary>
		/// Export the render mesh associated with a certain object
		/// </summary>
		/// <param name="parent">The parent node in the output XML document</param>
		/// <param name="obj">The RhinoObject instance to be exported</param>
		/// <returns>true if any content was exported</returns>
		private bool ExportRenderMesh(XmlElement parent, RhinoObject obj, bool serialized) {
			ObjectType type = obj.ObjectType;
			if (type != ObjectType.Surface && type != ObjectType.Brep &&
				type != ObjectType.Mesh && type != ObjectType.Extrusion) {
				Log("Not exporting object of type " + type);
				return false;
			}

			ObjRef[] meshes = RhinoObject.GetRenderMeshes(
				new RhinoObject[] { obj }, true, true);

			if (meshes == null)
				return false;

			foreach (ObjRef meshRef in meshes) {
				if (meshRef == null)
					continue;

				XmlElement shapeElement = m_xmlDocument.CreateElement("shape");
				if (obj.Name.Length > 0)
					shapeElement.AppendChild(m_xmlDocument.CreateComment(" Rhino object '" + obj.Name + "' "));

				RhinoDoc doc = obj.Document;
				Mesh mesh = meshRef.Mesh();

				int matIdx = -1;
				switch (obj.Attributes.MaterialSource) {
					case ObjectMaterialSource.MaterialFromLayer:
						matIdx = doc.Layers[obj.Attributes.LayerIndex].RenderMaterialIndex;
						break;
					case ObjectMaterialSource.MaterialFromObject:
						matIdx = obj.Attributes.MaterialIndex;
						break;
				}

                
                RhinoApp.WriteLine("Exporting Mesh:" + obj.Name);
                int index = m_meshStore.Store(mesh, obj.Name);
                shapeElement.AppendChild(MakeProperty("filename", m_meshStore.Filename));
                shapeElement.AppendChild(MakeProperty("shapeIndex", index));
                shapeElement.SetAttribute("type", "serialized");
                parent.AppendChild(shapeElement);

				if (matIdx >= 0 && m_xmlIdMap.ContainsKey(matIdx)) {
					shapeElement.AppendChild(MakeReference(m_xmlIdMap[matIdx]));

					/* Create an area emitter if requested */
					Material mat = doc.Materials[matIdx];
					if (mat.EmissionColor.GetBrightness() > 0) {
						XmlElement emitterElement = m_xmlDocument.CreateElement("emitter");
						emitterElement.SetAttribute("type", "area");
						emitterElement.AppendChild(MakeProperty("radiance", mat.EmissionColor));
						shapeElement.AppendChild(emitterElement);
					}
				}
			}

			return meshes.Length > 0;
		}

		/// <summary>
		/// Create a XML property node
		/// </summary>
		/// <param name="name">Name of the property</param>
		/// <param name="value">Value of the property. An appropriate property
		/// tag will be chosen based on the value's type.</param>
		/// <returns>The created XML element</returns>
		XmlElement MakeProperty(string name, object value) {
			Type type = value.GetType();
			string elementType;
			if (type == typeof(string))
				elementType = "string";
			else if (type == typeof(int))
				elementType = "integer";
			else if (type == typeof(string))
				elementType = "string";
			else if (type == typeof(float) || type == typeof(double))
				elementType = "float";
			else if (type == typeof(Transform))
				elementType = "transform";
			else if (type == typeof(System.Drawing.Color))
				elementType = "srgb";
			else
				throw new Exception("Unknown element type!");

			XmlElement element = m_xmlDocument.CreateElement(elementType);
			element.SetAttribute("name", name);

			if (type == typeof(Transform)) {
				XmlElement matrix = m_xmlDocument.CreateElement("matrix");
				Transform trafo = (Transform) value;
				string matStr = "";
				for (int i = 0; i < 4; ++i)
					for (int j = 0; j < 4; ++j)
						matStr += trafo[i, j].ToString(CultureInfo.InvariantCulture) + ", ";
				matrix.SetAttribute("value", matStr.Substring(0, matStr.Length - 2));
				element.AppendChild(matrix);
			} else if (type == typeof(System.Drawing.Color)) {
				System.Drawing.Color color = (System.Drawing.Color) value;
				element.SetAttribute("value", ToStringHelper(color.R / 255.0f) + ", "
					+ ToStringHelper(color.G / 255.0f) + ", " + ToStringHelper(color.B / 255.0f));
			} else if (type == typeof(float)) {
				element.SetAttribute("value", ToStringHelper((float) value));
			} else if (type == typeof(double)) {
				element.SetAttribute("value", ToStringHelper((double) value));
			} else if (type == typeof(int)) {
				element.SetAttribute("value", ToStringHelper((int) value));
			} else {
				element.SetAttribute("value", value.ToString());
			}
			return element;
		}

		static string ToStringHelper(int value) {
			return value.ToString(CultureInfo.InvariantCulture);
		}

		static string ToStringHelper(double value) {
			return value.ToString(CultureInfo.InvariantCulture);
		}

		static string ToStringHelper(float value) {
			return value.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Create an XML reference node
		/// </summary>
		/// <param name="name">Name of the referenced object</param>
		/// <returns>The created XML element</returns>
		XmlElement MakeReference(string name) {
			XmlElement element = m_xmlDocument.CreateElement("ref");
			element.SetAttribute("id", name);
			return element;
		}

		/// <summary>
		/// Print some debug output onto Rhino's console
		/// </summary>
		private static void Log(string text) {
			RhinoApp.WriteLine(text);
		}

		/// <summary>
		/// Return an unique ID string for use in the generated XML document
		/// </summary>
		private string GetID(string prefix) {
			return prefix + m_idCounter++.ToString();
		}
	}
}
