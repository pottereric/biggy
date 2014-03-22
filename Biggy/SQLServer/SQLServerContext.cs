﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Configuration;

namespace Biggy.SQLServer
{
  public class SQLServerContext : BiggyRelationalContext 
  {

    public SQLServerContext(string connectionStringName) : base(connectionStringName) { }

    public override string DbDelimiterFormatString {
      get { return "[{0}]"; }
    }

    public override DbConnection OpenConnection() {
      var conn = new SqlConnection(this.ConnectionString);
      conn.Open();
      return conn;
    }

    protected override void LoadDbColumnsList() {
      this.DbColumnsList = new List<DbColumnMapping>();
      var sql = ""
        + "SELECT c.TABLE_NAME, c.COLUMN_NAME, "
        + "  CASE tc.CONSTRAINT_TYPE WHEN 'PRIMARY KEY' THEN CAST(1 AS BIt) ELSE CAST(0 AS Bit) END AS IsPrimaryKey,  "
	      + "  CASE (COLUMNPROPERTY(object_id(tc.TABLE_NAME), kcu.COLUMN_NAME, 'IsIdentity')) WHEN 1 THEN CAST(1 AS Bit) ELSE CAST(0 AS Bit) END as IsAuto "
        + "FROM INFORMATION_SCHEMA.COLUMNS c "
        + "LEFT OUTER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu "
        + "ON c.TABLE_SCHEMA = kcu.CONSTRAINT_SCHEMA AND c.TABLE_NAME = kcu.TABLE_NAME AND c.COLUMN_NAME = kcu.COLUMN_NAME "
        + "LEFT OUTER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc "
        + "ON kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA AND kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME";

      using (var conn = this.OpenConnection()) {
        using (var cmd = this.CreateCommand(sql, conn)) {
          var dr = cmd.ExecuteReader();
          while (dr.Read()) {
            var clm = dr["COLUMN_NAME"] as string;
            var newColumnMapping = new DbColumnMapping(this.DbDelimiterFormatString) {
              TableName = dr["TABLE_NAME"] as string,
              ColumnName = clm,
              PropertyName = clm,
              IsPrimaryKey = (bool)dr["IsPrimaryKey"],
              IsAutoIncementing = (bool)dr["IsAuto"]
            };
            this.DbColumnsList.Add(newColumnMapping);        
          }
        }
      }
    }

    protected override void LoadDbTableNames() {
      this.DbTableNames = new List<string>();
      var sql = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo'";
      using (var conn = this.OpenConnection()) {
        using (var cmd = this.CreateCommand(sql, conn)) {
          var dr = cmd.ExecuteReader();
          while (dr.Read()) {
            this.DbTableNames.Add(dr.GetString(0));
          }
        }
      }
    }

    public override string GetInsertReturnValueSQL(string delimitedPkColumn) {
      return string.Format("; SELECT SCOPE_IDENTITY() as {0}", delimitedPkColumn);
    }

    public override string BuildSelect(string where, string orderBy, int limit) {
      string sql = limit > 0 ? "SELECT TOP " + limit + " {0} FROM {1} " : "SELECT {0} FROM {1} ";
      if (!string.IsNullOrEmpty(where)) {
        sql += where.Trim().StartsWith("where", StringComparison.OrdinalIgnoreCase) ? where : " WHERE " + where;
      }
      if (!String.IsNullOrEmpty(orderBy)) {
        sql += orderBy.Trim().StartsWith("order by", StringComparison.OrdinalIgnoreCase) ? orderBy : " ORDER BY " + orderBy;
      }
      return sql;
    }


    public override string GetSingleSelect(string delimitedTableName, string where) {
      return string.Format("SELECT TOP 2 * FROM {0} WHERE {1}", delimitedTableName, where);
    }

    public override bool TableExists(string delimitedTableName) {
      bool exists = false;
      string select = ""
          + "SELECT COUNT(TABLE_NAME) FROM INFORMATION_SCHEMA.TABLES "
          + "WHERE TABLE_SCHEMA = 'dbo' "
          + "AND  TABLE_NAME = '{0}'";
      string sql = string.Format(select, delimitedTableName);
      var result = Convert.ToInt32(this.Scalar(sql));
      if (result > 0) {
        exists = true;
      }
      return exists;
    }
  }
}
