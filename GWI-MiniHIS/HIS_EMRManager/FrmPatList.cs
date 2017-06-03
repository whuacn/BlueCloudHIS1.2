﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HIS_EMRManager
{
    public partial class FrmPatList : Form
    {
        private string _selectedFileId = "";
        private bool _isSure = false;

        public string SelectedFileId
        {
            get { return _selectedFileId; }
            set { _selectedFileId = value; }
        }

        public bool IsSure
        {
            get { return _isSure; }
            set { _isSure = value; }
        }

        public FrmPatList(DataTable source)
        {
            InitializeComponent(); 
            dGVEResult.AutoGenerateColumns = false;
            dGVEResult.DataSource = source;
            dGVEResult.EndEdit();
        }

        private void dGVEResult_DoubleClick(object sender, EventArgs e)
        {
            if (dGVEResult.DataSource != null)
            {
                DataTable table = (DataTable)dGVEResult.DataSource;
                if (table.Rows.Count > dGVEResult.CurrentCell.RowIndex)
                {
                    SelectedFileId = table.Rows[dGVEResult.CurrentCell.RowIndex]["dahid"].ToString();
                    _isSure = true;
                }
            }
            this.Close();
        }

        private void dGVEResult_KeyDown(object sender, KeyEventArgs e)
        {
            if (dGVEResult.DataSource != null)
            {
                DataTable table = (DataTable)dGVEResult.DataSource;
                switch (e.KeyCode)
                {
                    case Keys.Down:
                        if (table.Rows.Count > dGVEResult.CurrentCell.RowIndex + 1)
                        {
                            dGVEResult.CurrentCell = dGVEResult[dGVEResult.CurrentCell.ColumnIndex, dGVEResult.CurrentCell.RowIndex + 1];
                        }
                        break;
                    case Keys.Up:
                        if (dGVEResult.CurrentCell.RowIndex > 0)
                        {
                            dGVEResult.CurrentCell = dGVEResult[dGVEResult.CurrentCell.ColumnIndex, dGVEResult.CurrentCell.RowIndex - 1];
                        }
                        break;
                    case Keys.Enter:
                        dGVEResult_DoubleClick(sender, null);
                        break;
                }
            }
        }
    }
}
