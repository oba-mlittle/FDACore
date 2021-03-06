﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FDAScripter
{
    public partial class frmFDAObjects : Form
    {
        public frmFDAObjects()
        {
            InitializeComponent();
        }

        private void rbTags_CheckedChanged(object sender, EventArgs e)
        {
            if (rbTags.Checked)
            {
                DataTable tagsTable = Program.QueryDB("select * from DataPointDefinitionStructures order by DPDUID");
                dgvFDAObjects.DataSource = tagsTable;
            }
        }

        private void rbConn_CheckedChanged(object sender, EventArgs e)
        {
            if (rbConn.Checked)
            {
                DataTable tagsTable = Program.QueryDB("select * from FDASourceConnections order by Description");
                dgvFDAObjects.DataSource = tagsTable;
            }
        }

        
        private void dgvFDAObjects_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex != -1 && e.RowIndex != -1 && e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                DataGridViewCell c = (sender as DataGridView)[e.ColumnIndex, e.RowIndex];
                if (!c.Selected)
                {
                    c.DataGridView.ClearSelection();
                    c.DataGridView.CurrentCell = c;
                    c.Selected = true;
                }
                // open context menu
                ctxMenu.Show(Cursor.Position);

            }
        }

        private void menuItemInsert_Click(object sender, EventArgs e)
        {
            // insert the selected item into the script on the script editor form
            string objectType = "";
            string objectID = "";
            string IDColName = "";

            frmScriptEditor editor = (frmScriptEditor)this.Owner;

            if (rbTags.Checked)
            {
                objectType = "tag";
                IDColName = "DPDUID";
               
            }
            else if (rbConn.Checked)
            {
                objectType = "conn";
                IDColName = "SCUID";
            }

            objectID = dgvFDAObjects.SelectedRows[0].Cells[dgvFDAObjects.Columns[IDColName].Index].Value.ToString();

            editor.InsertFDAObject(objectID, objectType);
        }
    }
}
