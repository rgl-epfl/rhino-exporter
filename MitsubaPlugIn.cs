using System;
using Rhino;
using Rhino.PlugIns;

namespace Mitsuba {
	public class MitsubaPlugIn : Rhino.PlugIns.FileExportPlugIn {
		static MitsubaPlugIn m_theplugin;

		public MitsubaPlugIn() {
			m_theplugin = this;
		}

		public static MitsubaPlugIn ThePlugIn {
			get { return m_theplugin; }
		}

		public override PlugInLoadTime LoadTime {
			get { return PlugInLoadTime.WhenNeededOrOptionsDialog; }
		}

		public PersistentSettings PluginSettings {
			get { return Settings; }
		}

		protected override Rhino.PlugIns.FileTypeList AddFileTypes(Rhino.FileIO.FileWriteOptions options) {
			Rhino.PlugIns.FileTypeList rc = new Rhino.PlugIns.FileTypeList();
			rc.AddFileType("Mitsuba scene (*.xml)", "xml");
			return rc;
		}

		protected override Rhino.PlugIns.WriteFileResult WriteFile(string filename, int index, RhinoDoc doc, Rhino.FileIO.FileWriteOptions options) {
			RhinoApp.WriteLine("Not implemented: 'save as'. Please use the Mitsuba command.");
			return Rhino.PlugIns.WriteFileResult.Failure;
		}


		protected override void OptionsDialogPages(System.Collections.Generic.List<Rhino.UI.OptionsDialogPage> pages) {
			pages.Add(new MitsubaOptionsPage(PluginSettings));
		}
	}
}
