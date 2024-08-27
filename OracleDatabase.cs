using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace ControlChart
{
    /// <summary>
    /// Oracle データベースへのアクセスを提供するクラス。
    /// </summary>
    public class OracleDatabase
    {
        private readonly string _connectionString;

        public OracleDatabase(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            _connectionString = connectionString;
        }

        /// <summary>
        /// 指定したクエリを実行し、結果を DataTable として返します。
        /// </summary>
        /// <param name="query">実行する SQL クエリ。</param>
        /// <param name="parameters">クエリのパラメータ。</param>
        /// <returns>結果の DataTable。</returns>
        public DataTable ExecuteQuery(string query, IDictionary<string, object> parameters = null)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));
            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    using (OracleCommand cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = query;

                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                cmd.Parameters.Add(new OracleParameter(param.Key, param.Value));
                            }
                        }

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            DataTable resultTable = new DataTable();
                            resultTable.Load(reader);
                            return resultTable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 指定した非クエリコマンドを実行します。
        /// </summary>
        /// <param name="query">実行する SQL クエリ。</param>
        /// <param name="parameters">クエリのパラメータ。</param>
        public int ExecuteNonQuery(string query, IDictionary<string, object> parameters = null)
        {
            int rowCnt = 0;

            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));

            using (OracleConnection connection = new OracleConnection(_connectionString))
            {
                connection.Open();

                using (OracleCommand cmd = connection.CreateCommand())
                {
                    cmd.CommandText = query;

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.Add(new OracleParameter(param.Key, param.Value));
                        }
                    }

                    rowCnt = cmd.ExecuteNonQuery();
                }
            }
            return rowCnt;
        }
    }
}
