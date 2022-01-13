using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace laba9
{
    class DBModel
    {
        public List<Attribute> Attributes { get; }

        public List<ForeignKey> ForeignKeys { get; }

        public DBModel(string connStr)
        {
            Attributes = new List<Attribute>();
            LoadAttributes(connStr);

            ForeignKeys = new List<ForeignKey>();
            LoadForeignKeys(connStr);
        }

        private void LoadForeignKeys(string connStr)
        {
            using var conn = new NpgsqlConnection(connStr);

            conn.Open();

            using var command = new NpgsqlCommand(
                "SELECT" +
                " tc.table_name AS foreign_table," +
                " kcu.column_name AS foreign_attribute," +
                " ccu.table_name AS original_table," +
                " ccu.column_name AS original_attribute " +
                "FROM information_schema.table_constraints AS tc " +
                "JOIN information_schema.key_column_usage AS kcu ON tc.constraint_name = kcu.constraint_name " +
                "JOIN information_schema.constraint_column_usage AS ccu ON ccu.constraint_name = tc.constraint_name " +
                "WHERE constraint_type = 'FOREIGN KEY';"
                , conn);

            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                ForeignKeys.Add(new ForeignKey()
                {
                    TableFrom = (string)reader["foreign_table"],
                    AttributeFrom = (string)reader["foreign_attribute"],
                    TableTo = (string)reader["original_table"],
                    AttributeTo = (string)reader["original_attribute"]
                }) ;
            }
        }

        internal ForeignKey GetForeignKey(string tableFrom, string tableTo)
        {
            return ForeignKeys.FirstOrDefault(fc => 
            fc.TableFrom == tableFrom && fc.TableTo == tableTo ||
            fc.TableFrom == tableTo && fc.TableTo == tableFrom);

        }

        internal List<string> GetNeighborTables(string table)
        {
            return ForeignKeys.Where(fc => fc.TableTo == table).Select(fc => fc.TableFrom).Union(
                ForeignKeys.Where(fc=>fc.TableFrom == table).Select(fc=>fc.TableTo)).ToList();
        }

        internal List<Attribute> GetTableAttributes(string table)
        {
            return Attributes.Where(attr => attr.TabbleName == table).ToList();
        }

        internal List<string> Tables
        {
            get => Attributes.Select(attr => attr.TabbleName).Distinct().ToList();
        }

        private void LoadAttributes(string connStr)
        {
            using var conn = new NpgsqlConnection(connStr);

            conn.Open();

            using var command = new NpgsqlCommand(
                "SELECT relname, attname, typcategory "+
                    "FROM(SELECT * FROM(SELECT * FROM pg_class WHERE relkind = 'r' AND relname NOT LIKE 'pg_%' AND relname NOT LIKE 'sql_%') c"+
                        " JOIN pg_attribute a ON a.attrelid = c.oid WHERE attnum > 0) as tab"+
                        " join pg_type t ON tab.atttypid = t.oid;"
                    , conn);

            var reader = command.ExecuteReader();

           while(reader.Read())
            {
                var table_name = (string)reader["relname"];
                var attribute_anme = (string)reader["attname"];
                var type_name = (char)reader["typcategory"];

                Attributes.Add(new Attribute()
                {
                    Name = attribute_anme,
                    TabbleName = table_name,
                    Type = type_name
                });

            }

        }
    }
}
