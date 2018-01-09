using System;
using System.IO;
using Rhino;

namespace Mitsuba {
	[System.Runtime.InteropServices.Guid("aa8f8bbb-0ae7-4c07-b8b7-03c6426aedd0")]

	public class MitsubaCommand : Rhino.Commands.Command {
		static MitsubaCommand m_theCommand;
		public MitsubaCommand() {
			m_theCommand = this;
		}

		///<summary>The one and only instance of this command</summary>
		public static MitsubaCommand TheCommand {
			get { return m_theCommand; }
		}

		///<returns>The command name as it appears on the Rhino command line</returns>
		public override string EnglishName {
			get { return "Mitsuba"; }
		}

		protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode) {
			string basePath = Path.GetDirectoryName(doc.Path);
			string filename = Path.GetFileNameWithoutExtension(doc.Path) + ".xml";
			MitsubaSettings settings = new MitsubaSettings();
			settings.Load(MitsubaPlugIn.ThePlugIn.PluginSettings);

            RhinoApp.WriteLine("Running command");

            try {
				MitsubaExporter exporter = new MitsubaExporter(settings, basePath, filename);
				exporter.Export(doc);
				return Rhino.Commands.Result.Success;
			} catch (Exception ex) {
				RhinoApp.WriteLine(ex.ToString());
				return Rhino.Commands.Result.Failure;
			}
		}
	}
}

