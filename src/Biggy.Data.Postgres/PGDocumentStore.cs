using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using Biggy.Core;
using Newtonsoft.Json;
using System.Data;

namespace Biggy.Data.Postgres {
  public class PgDocumentStore<T> : DocumentStoreBase<T> where T : new() {

    public PgDocumentStore(string connectionStringName, string tableName) 
      : base(new PgDbCore(connectionStringName), tableName) { }

    public PgDocumentStore(string connectionStringName) 
      : base(new PgDbCore(connectionStringName)) { }

    public PgDocumentStore(PgDbCore dbCore) 
      : base(dbCore) { }

    public PgDocumentStore(PgDbCore dbCore, string tableName) 
      : base(dbCore, tableName) { }

    public override List<T> TryLoadData() {
      var result = new List<T>();
      var tableName = DecideTableName();
      try {
        var sql = "select * from " + tableName;
        var data = Database.ExecuteDynamic(sql);
        //hopefully we have data
        foreach (var item in data) {
          //pull out the JSON
          var deserialized = JsonConvert.DeserializeObject<T>(item.body);
          result.Add(deserialized);
        }
      }
      catch (Npgsql.NpgsqlException x) {
        if (x.Message.Contains("does not exist")) {
          var sql = this.GetCreateTableSql();
          var added = Database.TransactDDL(Database.BuildCommand(sql));
          if (added == 0) {
            throw new InvalidProgramException("Document table not created");
          }
          TryLoadData();
        } else {
          throw;
        }
      }
      return result;
    }

    protected override string GetCreateTableSql() {
      string tableName = this.DecideTableName();
      string pkName = this.GetKeyName();
      Type keyType = this.GetKeyType();
      bool isAuto = this.DecideKeyIsAutoIncrementing();

      string pkTypeStatement = "serial primary key";
      if (!isAuto) {
        pkTypeStatement = "int primary key";
      }
      if (keyType == typeof(string) || keyType == typeof(Guid)) {
        pkTypeStatement = "text primary key";
      }

      string sqlformat = @"create table {0} (id {1}, body json, created_at timestamptz)";
      return string.Format(sqlformat, tableName, pkTypeStatement);
    }

    public override IEnumerable<IDbCommand> CreateInsertCommands(IEnumerable<T> items) {
      const int MAGIC_PG_PARAMETER_LIMIT = 2100;
      const int MAGIC_PG_ROW_VALUE_LIMIT = 1000;
      var commands = new List<System.Data.IDbCommand>();
      string insertFormat = "insert into {0} (id, body, created_at) values ";
      string valueGroupFormat = "({0}::json, now())";

      var sb = new StringBuilder();
      var args = new List<object>();
      var paramIndex = 0;
      var rowValueCounter = 0;
      var valueGroups = new List<string>();

      sb.AppendFormat(insertFormat, this.TableName);
      if (this.KeyIsAutoIncrementing) {
        ReserveAutoIdsForItems(items);
      }
      foreach (var item in items) {
        var ex = this.SetDataForDocument(item);
        var itemSchema = ex as IDictionary<string, object>;
        var parameterPlaceholders = new List<string>();

        // pg imposes limits on the number of params and rows in a single statement:
        if (paramIndex + itemSchema.Count >= MAGIC_PG_PARAMETER_LIMIT || rowValueCounter >= MAGIC_PG_ROW_VALUE_LIMIT) {
          // Grab the sql statement from sb and add a command to the list:
          sb.Append(string.Join(", ", valueGroups.ToArray()));
          commands.Add(this.Database.BuildCommand(sb.ToString(), args.ToArray()));

          // Start over:
          sb = new StringBuilder();
          sb.AppendFormat(insertFormat, this.TableName);
          paramIndex = 0;
          parameterPlaceholders.Clear();
          valueGroups.Clear();
          args.Clear();
        }
        foreach (var kvp in itemSchema) {
          args.Add(kvp.Value);
          parameterPlaceholders.Add("@" + paramIndex++.ToString());
        }
        valueGroups.Add(string.Format(valueGroupFormat, string.Join(",", parameterPlaceholders)));
      }
      sb.Append(string.Join(", ", valueGroups.ToArray()));
      commands.Add(this.Database.BuildCommand(sb.ToString(), args.ToArray()));
      return commands;
    }

    public void ReserveAutoIdsForItems(IEnumerable<T> items) {
      if (items.Count() > 0) {
        if (this.KeyIsAutoIncrementing) {
          // We have to use some tricks to batch insert the proper sequence values:
          string sequenceName = string.Format("{0}_id_seq", this.TableName);
          int itemCount = items.Count();
          string sqlReservedSequenceValues = string.Format("SELECT nextval('{0}') FROM generate_series( 1, {1} ) n", sequenceName, itemCount);

          // Load up the reserved values into the Id field for each item:
          using (var dr = Database.OpenReader(sqlReservedSequenceValues)) {
            int row = 0;
            while (dr.Read()) {
              var curr = items.ElementAt(row);
              this.SetKeyValue(curr, dr[0]);
              row++;
            }
          }
        }
      }
    }

    public override IEnumerable<IDbCommand> CreateUpdateCommands(IEnumerable<T> items) {
      var commands = new List<IDbCommand>();
      if (items.Count() > 0) {
        var args = new List<object>();
        var paramIndex = 0;
        string ParameterAssignmentFormat = "{0} = @{1}";
        string sqlFormat = ""
        + "update {0} set {1} where {2};";
        var sb = new StringBuilder();

        foreach (var item in items) {
          var ex = this.SetDataForDocument(item);
          var dc = ex as IDictionary<string, object>;
          var setValueStatements = new List<string>();
          foreach (var kvp in dc) {
            if (kvp.Key != this.KeyName) {
              args.Add(kvp.Value);
              string setItem = string.Format(ParameterAssignmentFormat + "::json", kvp.Key, paramIndex++.ToString());
              setValueStatements.Add(setItem);
            }
          }
          args.Add(this.GetKeyValue(item));
          string whereCriteria = string.Format(ParameterAssignmentFormat, "id", paramIndex++.ToString());
          sb.AppendFormat(sqlFormat, this.TableName, string.Join(",", setValueStatements), whereCriteria);
        }
        commands.Add(Database.BuildCommand(sb.ToString(), args.ToArray()));
      }
      return commands;
    }

    public override IEnumerable<IDbCommand> CreateDeleteCommands(IEnumerable<T> items) {
      var commands = new List<IDbCommand>();
      if (items.Count() > 0) {
        var args = new List<object>();
        var parameterPlaceholders = new List<string>();
        var paramIndex = 0;
        string sqlFormat = ""
          + "delete from {0} where id in({1})";
        foreach (var item in items) {
          args.Add(this.GetKeyValue(item));
          parameterPlaceholders.Add("@" + paramIndex++.ToString());
        }
        var sql = string.Format(sqlFormat, this.TableName, string.Join(",", parameterPlaceholders));
        commands.Add(Database.BuildCommand(sql, args.ToArray()));
      }
      return commands;
    }

    public override IDbCommand CreateDeleteAllCommand() {
      string sql = string.Format("delete from {0}", this.TableName);
      return Database.BuildCommand(sql);
    }
  }
}