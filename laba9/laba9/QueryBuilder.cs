using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace laba9
{
    class QueryBuilder
    {
        private DBModel Model {get;}

        public QueryBuilder (DBModel model)
        {
            Model = model;
        }

        public string BuildQuery(List<Condition> conditions,  Dictionary<string, List<string>> tabAtr)
        {
            string str = "";

            List<string> strTAble = new List<string>();
            foreach (var egge in tabAtr)
            {
                if (egge.Value.Count == 0)
                    break;

                strTAble.Add(egge.Key);

                foreach(string egge1 in egge.Value)
                {
                    if(str =="")
                        str += egge.Key + "." + egge1+" ";
                    else
                        str +=", " + egge.Key + "." + egge1 + " ";
                }
            }
            if (str == "")
                str = "*";

            var select = "SELECT "+str;
            var usedTable = conditions.Select(c => c.TableName).Distinct().ToList().Union(strTAble).ToList();

            var from = "FROM " + BuildFromClause(usedTable);
            var where = "WHERE " + BuildWhereClause(conditions);

            return $"{select}\n{from}\n{where}";
        }

        private string BuildWhereClause(List<Condition> conditions)
        {
            return String.Join("\nAND ", conditions.Select(c => $"{c.TableName}.{c.AttributeName}{c.Operator}{c.Value}"));
        }

        private string BuildFromClause(List<string> tables)
        {
            if(tables.Count() == 0)
                return "";

            if (tables.Count() == 1)
                return tables[0];

            var pathPairs = new List<ForeignKey>();
            foreach(var table in tables.Skip(1))
            {
                var path = GetPathForeignKeys(tables[0], table);
                if (path is null)
                    return $"Нет пути между таблицами:{table[0]}, {table}";
                pathPairs.AddRange(path);
            }

            var fromClause = "";
            var usedTables = new HashSet<string>();

            foreach(var fc in pathPairs.Distinct())
            {
                if(fromClause =="")
                {
                    fromClause = fc.TableTo;
                    usedTables.Add(fc.TableTo);
                }

                var tableToJoin = usedTables.Contains(fc.TableFrom) ? fc.TableTo : fc.TableFrom;

                fromClause +=
                    $"\n JOIN {tableToJoin} ON {fc.TableFrom}.{fc.AttributeFrom} = {fc.TableTo}.{fc.AttributeTo}";

                usedTables.Add(tableToJoin);
            }

            return fromClause;
        }

        private List<ForeignKey> GetPathForeignKeys(string tableFrom, string tableTo, HashSet<string> usedTables = null)//DFS
        {
            List<ForeignKey> result = new();

            usedTables ??= new HashSet<string>();
            //1. Пометить текущую вершину использованной
            usedTables.Add(tableFrom);
            //2. Проверить есть ли внешний ключ между текущими двумя вершинами
            var fc = Model.GetForeignKey(tableFrom, tableTo);

            if (fc != null)
            {
                result.Add(fc);
                return result;
            }

            //3. Для всех соседних таблиц 
            var neighbors = Model.GetNeighborTables(tableFrom);
            foreach(var neighbor in neighbors)
            {
                if (usedTables.Contains(neighbor))
                    continue;

                var path = GetPathForeignKeys(neighbor, tableTo, usedTables);

                if (path != null)
                {
                    fc = Model.GetForeignKey(tableFrom, neighbor);
                    path.Insert(0, fc);
                    return path;
                }
            }
            return null;
            //  3.1 Если соседняя таблица не использованна:
            //  3.2 Получить путь от соседней вершины до конечной
            //  3.3 Если путь не пустой то вернуть его, добавив текущий внешний ключ между текущей таблицей и соседом
        }
    }
}
