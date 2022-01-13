using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace laba9
{
    public partial class CustomQueryForm : Form
    {

        private string _templateConnStr = new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Port = 5432,
            Username = "postgres",
            Password = "postgres"
        }.ConnectionString;

        private DBModel _dBModel;
        private QueryBuilder _queryBuilder;
        private BindingList<Condition> _conditions= new ();
        private string _connStr;

        public CustomQueryForm()
        {
            InitializeComponent();

            InitialazeDatabaseList();
        }

        private void InitialazeDatabaseList()
        {
            var connStr = new NpgsqlConnectionStringBuilder(_templateConnStr) { Database = "postgres" }.ConnectionString;

            using var conn = new NpgsqlConnection(connStr);

            conn.Open();

            using var command = new NpgsqlCommand(
                "SELECT datname " +
                "FROM pg_catalog.pg_database " +
                "WHERE datistemplate = false " +
                "ORDER BY datname"
                , conn);

            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                cbDatabase.Items.Add(reader[0]);
            }
            cbDatabase.SelectedIndex = 0;
        }

        private void InitialazeConditionTab()
        {
            _conditions.Clear();
            dgvConditions.DataSource = _conditions;

            cbCondTables.Items.Clear();
            cbCondTables.Items.AddRange(_dBModel.Tables.ToArray());
            cbCondTables.SelectedIndex = 0;

            cbCondOperator.SelectedIndex = 0;
        }

        private void InitialazeAttributeTab()
        {
            Dictionary<string, List<string>> tabAtr = new Dictionary<string, List<string>>();
            using var conn = new NpgsqlConnection(_connStr);
            conn.Open();
            using var command = new NpgsqlCommand(
                "SELECT relname, attname " +
                "FROM(SELECT * FROM(SELECT * FROM pg_class WHERE relkind = 'r' AND relname NOT LIKE 'pg_%' AND relname NOT LIKE 'sql_%') c " +
                "JOIN pg_attribute a ON a.attrelid = c.oid WHERE attnum > 0) as tab ORDER BY relname", conn);

            var reader = command.ExecuteReader();
            string curTable = "";
            while (reader.Read())
            {
                if (curTable == "" || curTable != reader["relname"].ToString())
                {
                    curTable = reader["relname"].ToString();
                    tabAtr.Add(curTable, new List<string>());
                }
                tabAtr[curTable].Add(reader["attname"].ToString());
            }

            foreach (var table in tabAtr)
            {
                var column = table.Value;

                var groupBox = new System.Windows.Forms.GroupBox();
                groupBox.Text = table.Key;
                var flp = new FlowLayoutPanel();
                flp.AutoSize = true;
                flp.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                flp.WrapContents = false;
                flp.Dock = DockStyle.Fill;
                flp.FlowDirection = FlowDirection.TopDown;

                groupBox.Controls.Add(flp);

                foreach (var i in column)
                {
                    var checkBox = new CheckBox();
                    checkBox.Text = i;
                    checkBox.AutoSize = true;
                    checkBox.Dock = DockStyle.Top;
                    flp.Controls.Add(checkBox);
                }

                groupBox.AutoSize = true;
                groupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                flpProj.Controls.Add(groupBox);
            }

        }

        private void CustomQueryForm_Load(object sender, EventArgs e)
        {

        }

        private void cbCondTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedTable = (string)cbCondTables.SelectedItem;

            if (selectedTable is null)
                return;

            cbCondAttribute.Items.Clear();

            cbCondAttribute.Items.AddRange(_dBModel.GetTableAttributes(selectedTable).ToArray());
            cbCondAttribute.SelectedIndex = 0;

        }

        private void btAddCondition_Click(object sender, EventArgs e)
        {
            _conditions.Add(new Condition()
            {
                TableName = cbCondTables.Text,
                AttributeName = (cbCondAttribute.SelectedItem as Attribute)?.Name,
                Operator = cbCondOperator.Text,
                Value = cbCondValue.Text
            });
        }

        private void btShowQuery_Click(object sender, EventArgs e)
        {
            Dictionary<string, List<string>> tabAtr = new Dictionary<string, List<string>>();
            foreach (GroupBox egge in flpProj.Controls)
            {
                tabAtr.Add(egge.Text, new List<string>());

                FlowLayoutPanel panel = (FlowLayoutPanel)egge.Controls[0];

                foreach(var egge1 in panel.Controls)
                {
                    if((egge1 as CheckBox).Checked)
                    {
                        tabAtr[egge.Text].Add((egge1 as CheckBox).Text);
                    }
                }
                //foreach (CheckBox egge1 in egge.Controls)
                //{
                //    if (egge1.Checked)
                //    {
                //        tabAtr[egge.Text].Add(egge1.Text);
                //    }
                //}
            }
            try
            {
                MessageBox.Show(_queryBuilder.BuildQuery(_conditions.ToList(), tabAtr));
            } catch
            {
                MessageBox.Show("Error");
            }
        }

        private void btExecute_Click(object sender, EventArgs e)
        {
            dgvResult.Rows.Clear();
            dgvResult.Columns.Clear();

            using var conn = new NpgsqlConnection(_connStr);
            conn.Open();

            Dictionary<string, List<string>> tabAtr = new Dictionary<string, List<string>>();
            foreach (GroupBox egge in flpProj.Controls)
            {
                tabAtr.Add(egge.Text, new List<string>());

                FlowLayoutPanel panel = (FlowLayoutPanel)egge.Controls[0];

                foreach (var egge1 in panel.Controls)
                {
                    if ((egge1 as CheckBox).Checked)
                    {
                        tabAtr[egge.Text].Add((egge1 as CheckBox).Text);
                    }
                }
                //foreach (CheckBox egge1 in egge.Controls)
                //{
                //    if (egge1.Checked)
                //    {
                //        tabAtr[egge.Text].Add(egge1.Text);
                //    }
                //}
            }
            string str = "";

            try
            {
                str = _queryBuilder.BuildQuery(_conditions.ToList(), tabAtr);
            }
            catch
            {
                MessageBox.Show("Error");
            }


            using var command = new NpgsqlCommand(str, conn);
            var reader = command.ExecuteReader();
            for (var i = 0; i < reader.FieldCount; i++)
                dgvResult.Columns.Add(reader.GetName(i), reader.GetName(i));

            while(reader.Read())
            {
                var dgvRow = new DataGridViewRow();
                for (var i = 0; i < reader.FieldCount; i++)
                    dgvRow.Cells.Add(new DataGridViewTextBoxCell() { Value = reader[i] });

                dgvResult.Rows.Add(dgvRow);
            }

            tabControl.SelectTab("tpResult");
        }

        private void btSelectDatabase_Click(object sender, EventArgs e)
        {
            _connStr = new NpgsqlConnectionStringBuilder(_templateConnStr) { Database = cbDatabase.Text }.ConnectionString;

            _dBModel = new DBModel(_connStr);
            _queryBuilder = new QueryBuilder(_dBModel);

            InitialazeConditionTab();
            InitialazeAttributeTab();
        }

        private void btSelectAll_Click(object sender, EventArgs e)
        {
            foreach (GroupBox egge in flpProj.Controls)
            {
                FlowLayoutPanel panel = (FlowLayoutPanel)egge.Controls[0];

                foreach (var egge1 in panel.Controls)
                {
                    (egge1 as CheckBox).Checked = true;
                }
            }
        }

        private void btClearSelection_Click(object sender, EventArgs e)
        {
            foreach (GroupBox egge in flpProj.Controls)
            {
                FlowLayoutPanel panel = (FlowLayoutPanel)egge.Controls[0];

                foreach (var egge1 in panel.Controls)
                {
                    (egge1 as CheckBox).Checked = false;
                }
            }
        }
    }
}