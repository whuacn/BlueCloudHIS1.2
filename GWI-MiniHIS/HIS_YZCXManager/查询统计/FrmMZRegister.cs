﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GWI.HIS.Windows.Controls;
using HIS.YZCX_BLL;

namespace HIS_YZCXManager
{
    public partial class FrmMZRegister : BaseMainForm
    {
        public FrmMZRegister(int OperatorId,string FormText)
        {
            InitializeComponent( );

            this.Text = FormText;
            this.FormTitle = FormText;
        }

        private void btnStat_Click( object sender , EventArgs e )
        {
            int viewType = cboViewType.SelectedIndex;
            DateTime dateBegin = Convert.ToDateTime(dtpFrom.Value.ToString("yyyy-MM-dd") + " 00:00:00");
            DateTime dateEnd = Convert.ToDateTime( dtpEnd.Value.ToString( "yyyy-MM-dd" ) + " 23:59:59" );

            DataSet dsData = ReportClass.GetRegisterReport( viewType , dateBegin , dateEnd );

            dgvList.AutoGenerateColumns = false;
            dgvList.DataSource = dsData.Tables["TB_A"];

            plGraph.Tag = dsData.Tables["TB_B"];

            ShowGraph(  );
        }

        private void ShowGraph(  )
        {
            if ( plGraph.Tag == null )
                return;

            DataTable tbData = (DataTable)plGraph.Tag;

            TableColumn[] columns = new TableColumn[1];

            columns[0].ColumnName = "人次";
            columns[0].ColumnField = "NUM";

            GraphControl gc;
            DataTableStruct datatablestruct = DataTableStruct.Rows;
           
            Color[] colors = new Color[] { Color.Blue , Color.Aqua , Color.Beige , Color.BlanchedAlmond , Color.Brown , Color.Chocolate , Color.DarkGray , Color.DodgerBlue };

            if ( rdHistogram.Checked )
                gc = new HistogramGraphControl( this.plGraph , datatablestruct , columns , colors , tbData , "DATE" );
            else
                gc = new LineGraphControl( this.plGraph , datatablestruct , columns , colors , tbData , "DATE" );

            object obj = tbData.Compute( "Sum(Num)" , "" );
            string title = "门诊人次：" + ( obj == null ? "0" : obj.ToString( ) ) + "人";
            gc.GraphTitle = title;
            gc.DrawGraph( );
        }

        private void btnClose_Click( object sender , EventArgs e )
        {
            this.Close( );
        }

        private void FrmRegisterReport_Load( object sender , EventArgs e )
        {
            cboViewType.SelectedIndex = 0;
        }

        private void rdLine_CheckedChanged( object sender , EventArgs e )
        {
            
                ShowGraph( );
        }
    }
}
