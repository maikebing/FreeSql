﻿using FreeSql.Internal;
using FreeSql.Internal.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Maikebing.Data.Taos;

namespace FreeSql.TDengine
{

    class TDengineUtils : CommonUtils
    {
        public TDengineUtils(IFreeSql orm) : base(orm)
        {
        }

        public override DbParameter AppendParamter(List<DbParameter> _params, string parameterName, ColumnInfo col, Type type, object value)
        {
            if (string.IsNullOrEmpty(parameterName)) parameterName = $"p_{_params?.Count}";
            var dbtype = (DbType)_orm.CodeFirst.GetDbInfo(type)?.type;
            switch (dbtype)
            {
                case DbType.Guid:
                    if (value == null) value = null;
                    else value = ((Guid)value).ToString();
                    dbtype = DbType.String;
                    break;
                case DbType.Time:
                    if (value == null) value = null;
                    else value = ((TimeSpan)value).Ticks / 10000;
                    dbtype = DbType.Int64;
                    break;
            }
            var ret = new TaosParameter();
            ret.ParameterName = QuoteParamterName(parameterName);
            ret.DbType = dbtype;
            ret.Value = value;
            _params?.Add(ret);
            return ret;
        }

        public override DbParameter[] GetDbParamtersByObject(string sql, object obj) =>
            Utils.GetDbParamtersByObject<DbParameter>(sql, obj, "@", (name, type, value) =>
            {
                var dbtype = (DbType)_orm.CodeFirst.GetDbInfo(type)?.type;
                switch (dbtype)
                {
                    case DbType.Guid:
                        if (value == null) value = null;
                        else value = ((Guid)value).ToString();
                        dbtype = DbType.String;
                        break;
                    case DbType.Time:
                        if (value == null) value = null;
                        else value = ((TimeSpan)value).Ticks / 10000;
                        dbtype = DbType.Int64;
                        break;
                }
                var ret = new TaosParameter();
                ret.ParameterName = $"@{name}";
                ret.DbType = dbtype;
                ret.Value = value;
                return ret;
            });

        public override string FormatSql(string sql, params object[] args) => sql?.FormatTDengine(args);
        public override string QuoteSqlName(params string[] name)
        {
            if (name.Length == 1)
            {
                var nametrim = name[0].Trim();
                if (nametrim.StartsWith("(") && nametrim.EndsWith(")"))
                    return nametrim; //原生SQL
                if (nametrim.StartsWith("\"") && nametrim.EndsWith("\""))
                    return nametrim;
                return $"\"{nametrim.Replace(".", "\".\"")}\"";
            }
            return $"\"{string.Join("\".\"", name)}\"";
        }
        public override string TrimQuoteSqlName(string name)
        {
            var nametrim = name.Trim();
            if (nametrim.StartsWith("(") && nametrim.EndsWith(")"))
                return nametrim; //原生SQL
            return $"{nametrim.Trim('"').Replace("\".\"", ".").Replace(".\"", ".")}";
        }
        public override string[] SplitTableName(string name) => GetSplitTableNames(name, '"', '"', 2);
        public override string QuoteParamterName(string name) => $"@{(_orm.CodeFirst.IsSyncStructureToLower ? name.ToLower() : name)}";
        public override string IsNull(string sql, object value) => $"ifnull({sql}, {value})";
        public override string StringConcat(string[] objs, Type[] types) => $"{string.Join(" || ", objs)}";
        public override string Mod(string left, string right, Type leftType, Type rightType) => $"{left} % {right}";
        public override string Div(string left, string right, Type leftType, Type rightType) => $"{left} / {right}";
        public override string Now => "datetime(current_timestamp,'localtime')";
        public override string NowUtc => "current_timestamp";

        public override string QuoteWriteParamter(Type type, string paramterName) => paramterName;
        public override string QuoteReadColumn(Type type, Type mapType, string columnName) => columnName;

        public override string GetNoneParamaterSqlValue(List<DbParameter> specialParams, Type type, object value)
        {
            if (value == null) return "NULL";
            return FormatSql("{0}", value, 1);
        }
    }
}