﻿namespace FDAInterface
{
    partial class FancyTextBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.flashtimer = new System.Windows.Forms.Timer(this.components);
            this.inhibitTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // flashtimer
            // 
            this.flashtimer.Interval = 500;
            this.flashtimer.Tick += new System.EventHandler(this.flashtimer_Tick);
            // 
            // inhibitTimer
            // 
            this.inhibitTimer.Interval = 1000;
            this.inhibitTimer.Tick += new System.EventHandler(this.inhibitTimer_Tick);
            // 
            // FancyTextBox
            // 
            this.ReadOnly = true;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer flashtimer;
        private System.Windows.Forms.Timer inhibitTimer;
    }
}
