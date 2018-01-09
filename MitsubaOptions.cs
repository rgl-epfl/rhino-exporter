using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Rhino;

namespace Mitsuba {
	struct MitsubaSettings {
		public enum Integrator {
			EDirectIllumination = 0,
			EPathTracer,
			EAdjointParticleTracer,
			EBidirectional,
			EKelemenMLT,
			EVeachMLT,
			EERPT
		};
        public bool writeSerialized;
		public string rendererDirectory;
		public Integrator integrator;
		public int xres, yres;
		public int pathLength;
		public int samplesPerPixel;

		public void Load(PersistentSettings settings) {
            writeSerialized = settings.GetBool("WriteSerialized", false);
            rendererDirectory = settings.GetString("RendererDirectory", "");
			xres = settings.GetInteger("XResolution", 1024);
			yres = settings.GetInteger("YResolution", 768);
			pathLength = settings.GetInteger("PathLength", 3);
			samplesPerPixel = settings.GetInteger("SamplesPerPixel", 4);
			integrator = (Integrator) settings.GetInteger("Integrator", 
				(int) Integrator.EDirectIllumination);
		}

		public void Store(PersistentSettings settings) {
            settings.SetBool("WriteSerialized", writeSerialized);
            settings.SetString("RendererDirectory", rendererDirectory);
			settings.SetInteger("XResolution", xres);
			settings.SetInteger("YResolution", yres);
			settings.SetInteger("PathLength", pathLength);
			settings.SetInteger("SamplesPerPixel", samplesPerPixel);
			settings.SetInteger("Integrator", (int) integrator);
		}
	};

	partial class MitsubaOptionsControl : UserControl {
		private Button browseButton;
		private Label label2;
		private Label label3;
		private TextBox rendererDirectory;
		private Label label4;
		private Label label5;
		private Label label6;
		private Label label8;
		private Label label9;
		private ComboBox integrator;
		private NumericUpDown pathLength;
		private ComboBox resolution;
		private Label label7;
		private NumericUpDown samplesPerPixel;
		private FolderBrowserDialog folderBrowserDialog1;
        private CheckBox writeSerializedBox;
        private Label label10;

		public MitsubaOptionsControl(ref MitsubaSettings settings) {
			InitializeComponent();

            writeSerializedBox.Checked = settings.writeSerialized;
			integrator.SelectedIndex = (int) settings.integrator;
			pathLength.Value = settings.pathLength;
			samplesPerPixel.Value = settings.samplesPerPixel;
			rendererDirectory.Text = settings.rendererDirectory;
			resolution.Text = settings.xres + "x" + settings.yres;
			pathLength.Enabled = integrator.SelectedIndex != (int) MitsubaSettings.Integrator.EDirectIllumination;
		}

		public void Apply(ref MitsubaSettings settings) {
            settings.writeSerialized = writeSerializedBox.Checked;
			settings.integrator = (MitsubaSettings.Integrator) integrator.SelectedIndex;
			settings.rendererDirectory = rendererDirectory.Text;
			settings.samplesPerPixel = (int) samplesPerPixel.Value;
			settings.pathLength = (int) pathLength.Value;
			string[] parts = resolution.Text.Split('x');
			if (parts.Length != 2)
				throw new Exception("Invalid resolution specification!");
			settings.xres = Convert.ToInt32(parts[0]);
			settings.yres = Convert.ToInt32(parts[1]);
		}

		private void InitializeComponent() {
            this.browseButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.rendererDirectory = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.integrator = new System.Windows.Forms.ComboBox();
            this.pathLength = new System.Windows.Forms.NumericUpDown();
            this.resolution = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.samplesPerPixel = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.writeSerializedBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pathLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.samplesPerPixel)).BeginInit();
            this.SuspendLayout();
            // 
            // browseButton
            // 
            this.browseButton.Location = new System.Drawing.Point(345, 50);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(28, 23);
            this.browseButton.TabIndex = 2;
            this.browseButton.Text = "..";
            this.browseButton.UseVisualStyleBackColor = true;
            this.browseButton.Click += new System.EventHandler(this.browseButton_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 18);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(183, 25);
            this.label2.TabIndex = 3;
            this.label2.Text = "Mitsuba Renderer";
            // 
            // label3
            // 
            this.label3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label3.Location = new System.Drawing.Point(112, 24);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(280, 2);
            this.label3.TabIndex = 4;
            // 
            // rendererDirectory
            // 
            this.rendererDirectory.Location = new System.Drawing.Point(168, 52);
            this.rendererDirectory.Name = "rendererDirectory";
            this.rendererDirectory.Size = new System.Drawing.Size(174, 31);
            this.rendererDirectory.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.label4.Location = new System.Drawing.Point(112, 141);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(280, 3);
            this.label4.TabIndex = 7;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(16, 159);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(166, 25);
            this.label5.TabIndex = 6;
            this.label5.Text = "Render Settings";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(40, 55);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(202, 25);
            this.label6.TabIndex = 8;
            this.label6.Text = "Renderer directory :";
            this.label6.Click += new System.EventHandler(this.label6_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(40, 230);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(115, 25);
            this.label8.TabIndex = 10;
            this.label8.Text = "Integrator :";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(40, 265);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(133, 25);
            this.label9.TabIndex = 11;
            this.label9.Text = "Path length :";
            // 
            // integrator
            // 
            this.integrator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.integrator.FormattingEnabled = true;
            this.integrator.Items.AddRange(new object[] {
            "Direct Illumination",
            "Path tracer",
            "Adjoint Particle Tracer",
            "Bidirectional Path Tracer",
            "Primary Sample Space MLT",
            "Path Space MLT",
            "Energy Redistribution Path Tracing"});
            this.integrator.Location = new System.Drawing.Point(168, 227);
            this.integrator.Name = "integrator";
            this.integrator.Size = new System.Drawing.Size(205, 33);
            this.integrator.TabIndex = 12;
            this.integrator.SelectedIndexChanged += new System.EventHandler(this.integrator_SelectedIndexChanged);
            // 
            // pathLength
            // 
            this.pathLength.Location = new System.Drawing.Point(168, 263);
            this.pathLength.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.pathLength.Name = "pathLength";
            this.pathLength.Size = new System.Drawing.Size(205, 31);
            this.pathLength.TabIndex = 13;
            this.pathLength.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // resolution
            // 
            this.resolution.FormattingEnabled = true;
            this.resolution.Items.AddRange(new object[] {
            "512x512",
            "640x480",
            "768x512",
            "768x576",
            "1024x768",
            "1024x1024",
            "1280x720",
            "1280x1024",
            "1920x1080"});
            this.resolution.Location = new System.Drawing.Point(168, 192);
            this.resolution.Name = "resolution";
            this.resolution.Size = new System.Drawing.Size(205, 33);
            this.resolution.TabIndex = 14;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(40, 195);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(190, 25);
            this.label7.TabIndex = 15;
            this.label7.Text = "Image Resolution :";
            // 
            // samplesPerPixel
            // 
            this.samplesPerPixel.Location = new System.Drawing.Point(168, 300);
            this.samplesPerPixel.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.samplesPerPixel.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.samplesPerPixel.Name = "samplesPerPixel";
            this.samplesPerPixel.Size = new System.Drawing.Size(205, 31);
            this.samplesPerPixel.TabIndex = 17;
            this.samplesPerPixel.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(40, 302);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(195, 25);
            this.label10.TabIndex = 16;
            this.label10.Text = "Samples per pixel :";
            // 
            // writeSerializedBox
            // 
            this.writeSerializedBox.AutoSize = true;
            this.writeSerializedBox.Location = new System.Drawing.Point(45, 99);
            this.writeSerializedBox.Name = "writeSerializedBox";
            this.writeSerializedBox.Size = new System.Drawing.Size(273, 29);
            this.writeSerializedBox.TabIndex = 19;
            this.writeSerializedBox.Text = "Write serialized meshes";
            this.writeSerializedBox.UseVisualStyleBackColor = true;
            // 
            // MitsubaOptionsControl
            // 
            this.Controls.Add(this.writeSerializedBox);
            this.Controls.Add(this.samplesPerPixel);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.resolution);
            this.Controls.Add(this.pathLength);
            this.Controls.Add(this.integrator);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.rendererDirectory);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.browseButton);
            this.Name = "MitsubaOptionsControl";
            this.Size = new System.Drawing.Size(411, 361);
            ((System.ComponentModel.ISupportInitialize)(this.pathLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.samplesPerPixel)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		private void browseButton_Click(object sender, EventArgs e) {
			if (folderBrowserDialog1.ShowDialog() == DialogResult.OK) {
				rendererDirectory.Text = folderBrowserDialog1.SelectedPath;
			}
		}

		private void integrator_SelectedIndexChanged(object sender, EventArgs e) {
			pathLength.Enabled = integrator.SelectedIndex != (int) MitsubaSettings.Integrator.EDirectIllumination;
		}

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }

    class MitsubaOptionsPage : Rhino.UI.OptionsDialogPage {
		MitsubaOptionsControl m_control;
		PersistentSettings m_settings;
		MitsubaSettings m_mitsubaSettings;

		public MitsubaOptionsPage(Rhino.PersistentSettings settings)
			: base("Mitsuba") {
			m_settings = settings;
			m_mitsubaSettings = new MitsubaSettings();
			m_mitsubaSettings.Load(m_settings);
			m_control = new MitsubaOptionsControl(ref m_mitsubaSettings);
		}

		public override Control PageControl {
			get { return m_control; }
		}

		public override bool OnApply() {
			try {
				m_control.Apply(ref m_mitsubaSettings);
				m_mitsubaSettings.Store(m_settings);
				return true;
			} catch (Exception e) {
				RhinoApp.WriteLine(e.ToString());
				return false;
			}
		}
	}
}

