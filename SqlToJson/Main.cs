using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SqlToJson
{
    public partial class Main : Form
    {
        private Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private void loadConfig()
        {
            txConnStr.Text = config.AppSettings.Settings["connStr"].Value;
            txSqlQuery.Text = config.AppSettings.Settings["sqlQuery"].Value;
            txOutFile.Text = config.AppSettings.Settings["targetFile"].Value;
        }
        private void saveConfig()
        {
            config.AppSettings.Settings["connStr"].Value = txConnStr.Text;
            config.AppSettings.Settings["sqlQuery"].Value = txSqlQuery.Text;
            config.AppSettings.Settings["targetFile"].Value = txOutFile.Text;
            config.Save(ConfigurationSaveMode.Modified);
        }



        public Main()
        {
            InitializeComponent();
            loadConfig();
        }

        private DataTable sqlData = new DataTable();

        private void btnPreview_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            btnSave.Enabled = false;
            saveConfig();
            try
            {
                lvPreview.Items.Clear();
                lvPreview.Columns.Clear();

                FetchSqlData();
            }
            catch (Exception ex)
            {
                lblRowCount.Text = string.Format("ERROR: {0}", ex.Message);
            }
            this.Enabled = true;
        }


        public void FetchSqlData()
        {
            sqlData = new DataTable();
            using (SqlConnection conn = new SqlConnection(txConnStr.Text))
            {
                using (SqlCommand cmd = new SqlCommand(txSqlQuery.Text, conn))
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(sqlData);
                    PopulateListViewPreview(sqlData);
                }
            }
        }

        private void PopulateListViewPreview(DataTable data) {
            lvPreview.Items.Clear();
            foreach (DataColumn column in data.Columns)
            {
                var col = lvPreview.Columns.Add(column.ColumnName);
            }
            foreach (DataRow row in data.Rows)
            {
                ListViewItem item = new ListViewItem(row[0].ToString());
                for (int i = 1; i < data.Columns.Count; i++)
                    item.SubItems.Add(row[i].ToString());
                lvPreview.Items.Add(item);
            }
            lvPreview.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            lblRowCount.Text = string.Format("{0} results", data.Rows.Count.ToString());
            btnSave.Enabled = data.Rows.Count > 0;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            saveConfig();
            if (sqlData.Rows.Count == 0)
            {
                lblRowCount.Text = "no data to serialize!!";
                return;
            }

            try
            {
                string jsonResultStr = JsonConvert.SerializeObject(sqlData, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                System.IO.File.WriteAllText(txOutFile.Text, jsonResultStr);
                lblRowCount.Text = string.Format("{0} items saved in {1}", sqlData.Rows.Count.ToString(), txOutFile.Text);
            }
            catch (Exception ex)
            {
                lblRowCount.Text = "ERR: " + ex.Message;
            }
        }

    }
}
