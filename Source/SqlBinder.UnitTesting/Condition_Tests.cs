﻿using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlBinder.ConditionValues;

namespace SqlBinder.UnitTesting
{
	public partial class SqlBinder_Tests
	{
		/// <summary>
		/// Tests with parsing functionalities in mind such as comments, literals, escape characters etc.
		/// </summary>
		[TestClass]
		public class Condition_Tests
		{
			[TestInitialize]
			public void InitializeTest()
			{
				//
			}

			[TestMethod]
			public void Bool_Conditions_1()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]}}");

				// IS
				query.SetCondition("Criteria1", new BoolValue(true));
				var cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.IsTrue(cmd.Parameters[0].DbType == DbType.Boolean);
				Assert.AreEqual(true, cmd.Parameters[0].Value);

				// IS NOT
				query.SetCondition("Criteria1", Operator.IsNot, new BoolValue(false));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 != :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual(false, cmd.Parameters[0].Value);
			}

			[TestMethod]
			public void Date_Conditions_1()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]}}");

				var dt1 = DateTime.Now.AddDays(-10);
				var dt2 = DateTime.Now;

				// IS
				query.SetCondition("Criteria1", new DateValue(dt1));
				var cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.IsTrue(cmd.Parameters[0].DbType == DbType.DateTime);
				Assert.AreEqual(dt1, cmd.Parameters[0].Value);

				// IS LESS THAN
				query.SetCondition("Criteria1", Operator.IsLessThan, new DateValue(dt1));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 < :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual(dt1, cmd.Parameters[0].Value);

				// IS LESS THAN OR EQ
				query.SetCondition("Criteria1", Operator.IsLessThanOrEqualTo, new DateValue(dt1));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 <= :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual(dt1, cmd.Parameters[0].Value);

				// IS GR THAN
				query.SetCondition("Criteria1", Operator.IsGreaterThan, new DateValue(dt1));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 > :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual(dt1, cmd.Parameters[0].Value);

				// IS GR THAN OR EQ
				query.SetCondition("Criteria1", Operator.IsGreaterThanOrEqualTo, new DateValue(dt1));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 >= :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual(dt1, cmd.Parameters[0].Value);

				// IS NOT
				query.SetCondition("Criteria1", Operator.IsNot, new DateValue(dt1));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 != :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual(dt1, cmd.Parameters[0].Value);

				// IS BETWEEN
				query.SetCondition("Criteria1", Operator.IsBetween, new DateValue(dt1, dt2));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 BETWEEN :pCriteria1_1 AND :pCriteria1_2", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 2);
				Assert.AreEqual(dt1, cmd.Parameters[0].Value);
				Assert.AreEqual(dt2, cmd.Parameters[1].Value);

				// IS NOT BETWEEN
				query.SetCondition("Criteria1", Operator.IsNotBetween, new DateValue(dt1, dt2));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 NOT BETWEEN :pCriteria1_1 AND :pCriteria1_2", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 2);
				Assert.AreEqual(dt1, cmd.Parameters[0].Value);
				Assert.AreEqual(dt2, cmd.Parameters[1].Value);

				// IN
				query.SetCondition("Criteria1", Operator.IsAnyOf, new DateValue(new[] {dt1, dt2}));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 IN (:pCriteria1_1, :pCriteria1_2)", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 2);
				Assert.AreEqual(dt1, cmd.Parameters[0].Value);
				Assert.AreEqual(dt2, cmd.Parameters[1].Value);

				// NOT IN
				query.SetCondition("Criteria1", Operator.IsNotAnyOf, new DateValue(new[] { dt1, dt2 }));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 NOT IN (:pCriteria1_1, :pCriteria1_2)", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 2);
				Assert.AreEqual(dt1, cmd.Parameters[0].Value);
				Assert.AreEqual(dt2, cmd.Parameters[1].Value);
			}

			[TestMethod]
			public void Number_Conditions_1()
			{
				var query = new MockQuery(_connection,"SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]}}");

				Int32 n1 = 123;
				Int32 n2 = 456;

				// IS
				query.SetCondition("Criteria1", new NumberValue(n1));
				var cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.IsTrue(cmd.Parameters[0].DbType == DbType.Int32);
				Assert.AreEqual(n1, cmd.Parameters[0].Value);

				// IS LESS THAN
				query.SetCondition("Criteria1", Operator.IsLessThan, new NumberValue(n1));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 < :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual(n1, cmd.Parameters[0].Value);

				// IS LESS THAN OR EQ
				query.SetCondition("Criteria1", Operator.IsLessThanOrEqualTo, new NumberValue(n1));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 <= :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual(n1, cmd.Parameters[0].Value);

				// IS GR THAN
				query.SetCondition("Criteria1", Operator.IsGreaterThan, new NumberValue(n1));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 > :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual(n1, cmd.Parameters[0].Value);

				// IS GR THAN OR EQ
				query.SetCondition("Criteria1", Operator.IsGreaterThanOrEqualTo, new NumberValue(n1));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 >= :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual(n1, cmd.Parameters[0].Value);

				// IS NOT
				query.SetCondition("Criteria1", Operator.IsNot, new NumberValue(n1));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 != :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual(n1, cmd.Parameters[0].Value);

				// IS BETWEEN
				query.SetCondition("Criteria1", Operator.IsBetween, new NumberValue(n1, n2));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 BETWEEN :pCriteria1_1 AND :pCriteria1_2", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 2);
				Assert.AreEqual(n1, cmd.Parameters[0].Value);
				Assert.AreEqual(n2, cmd.Parameters[1].Value);

				// IS NOT BETWEEN
				query.SetCondition("Criteria1", Operator.IsNotBetween, new NumberValue(n1, n2));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 NOT BETWEEN :pCriteria1_1 AND :pCriteria1_2", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 2);
				Assert.AreEqual(n1, cmd.Parameters[0].Value);
				Assert.AreEqual(n2, cmd.Parameters[1].Value);

				// IN
				query.SetCondition("Criteria1", Operator.IsAnyOf, new NumberValue(new[] { n1, n2 }));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 IN (:pCriteria1_1, :pCriteria1_2)", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 2);
				Assert.AreEqual(n1, cmd.Parameters[0].Value);
				Assert.AreEqual(n2, cmd.Parameters[1].Value);

				// NOT IN
				query.SetCondition("Criteria1", Operator.IsNotAnyOf, new NumberValue(new[] { n1, n2 }));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 NOT IN (:pCriteria1_1, :pCriteria1_2)", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 2);
				Assert.AreEqual(n1, cmd.Parameters[0].Value);
				Assert.AreEqual(n2, cmd.Parameters[1].Value);
			}

			[TestMethod]
			public void Number_Conditions_2()
			{
				var query = new MockQuery(_connection,"SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]}}");

				decimal nDec = 123;
				double nDbl = 123;
				float nFlt = 123;				
				UInt32 nUInt32 = 123;
				UInt64 nUInt64 = 123;
				Int32 nInt32 = 123;
				Int64 nInt64 = 123;
				byte nByte = 123;
				sbyte nSByte = 123;
				char nChar = 'A';

				// DECIMAL
				query.SetCondition("Criteria1", new NumberValue(nDec));
				var cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.IsTrue(cmd.Parameters[0].DbType == DbType.Decimal);
				Assert.AreEqual(nDec, cmd.Parameters[0].Value);

				// DOUBLE
				query.SetCondition("Criteria1", new NumberValue(nDbl));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.IsTrue(cmd.Parameters[0].DbType == DbType.Double);
				Assert.AreEqual(nDbl, cmd.Parameters[0].Value);

				// FLOAT
				query.SetCondition("Criteria1", new NumberValue(nFlt));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.IsTrue(cmd.Parameters[0].DbType == DbType.Single);
				Assert.AreEqual(nFlt, cmd.Parameters[0].Value);

				// UINT32
				query.SetCondition("Criteria1", new NumberValue(nUInt32));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.IsTrue(cmd.Parameters[0].DbType == DbType.UInt32);
				Assert.AreEqual(nUInt32, cmd.Parameters[0].Value);

				// UINT64
				query.SetCondition("Criteria1", new NumberValue(nUInt64));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.IsTrue(cmd.Parameters[0].DbType == DbType.UInt64);
				Assert.AreEqual(nUInt64, cmd.Parameters[0].Value);

				// INT32
				query.SetCondition("Criteria1", new NumberValue(nInt32));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.IsTrue(cmd.Parameters[0].DbType == DbType.Int32);
				Assert.AreEqual(nInt32, cmd.Parameters[0].Value);

				// INT64
				query.SetCondition("Criteria1", new NumberValue(nInt64));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.IsTrue(cmd.Parameters[0].DbType == DbType.Int64);
				Assert.AreEqual(nInt64, cmd.Parameters[0].Value);

				// BYTE
				query.SetCondition("Criteria1", new NumberValue(nByte));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.IsTrue(cmd.Parameters[0].DbType == DbType.Byte);
				Assert.AreEqual(nByte, cmd.Parameters[0].Value);

				// SBYTE
				query.SetCondition("Criteria1", new NumberValue(nSByte));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.IsTrue(cmd.Parameters[0].DbType == DbType.SByte);
				Assert.AreEqual(nSByte, cmd.Parameters[0].Value);

				// CHAR
				query.SetCondition("Criteria1", new NumberValue(nChar));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.IsTrue(cmd.Parameters[0].DbType == DbType.StringFixedLength);
				Assert.IsTrue(cmd.Parameters[0].Size == 1);
				Assert.AreEqual(nChar, cmd.Parameters[0].Value);

			}

			[TestMethod]
			public void String_Conditions_1()
			{
				var query = new MockQuery(_connection,"SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]}}");

				var s1 = "Value 1";
				var s2 = "Value 2";

				// IS
				query.SetCondition("Criteria1", new StringValue(s1));
				var cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.IsTrue(cmd.Parameters[0].DbType == DbType.String);
				Assert.AreEqual(s1, cmd.Parameters[0].Value);

				// LIKE
				query.SetCondition("Criteria1", Operator.Contains, new StringValue(s1));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 LIKE :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual(s1, cmd.Parameters[0].Value);

				// NOT LIKE
				query.SetCondition("Criteria1", Operator.DoesNotContain, new StringValue(s1));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 NOT LIKE :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual(s1, cmd.Parameters[0].Value);

				// BEGINS WITH
				query.SetCondition("Criteria1", Operator.Contains, new StringValue(s1, StringValue.MatchOption.BeginsWith, "*"));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 LIKE :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual($"{s1}*", cmd.Parameters[0].Value);

				// CONTAINS
				query.SetCondition("Criteria1", Operator.Contains, new StringValue(s1, StringValue.MatchOption.OccursAnywhere, "*"));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 LIKE :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual($"*{s1}*", cmd.Parameters[0].Value);

				// ENDS WITH
				query.SetCondition("Criteria1", Operator.Contains, new StringValue(s1, StringValue.MatchOption.EndsWith, "*"));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 LIKE :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual($"*{s1}", cmd.Parameters[0].Value);

				// IS NOT
				query.SetCondition("Criteria1", Operator.IsNot, new StringValue(s1));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 != :pCriteria1_1", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 1);
				Assert.AreEqual(s1, cmd.Parameters[0].Value);

				// IS BETWEEN
				query.SetCondition("Criteria1", Operator.IsBetween, new StringValue(s1, s2));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 BETWEEN :pCriteria1_1 AND :pCriteria1_2", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 2);
				Assert.AreEqual(s1, cmd.Parameters[0].Value);
				Assert.AreEqual(s2, cmd.Parameters[1].Value);

				// IS NOT BETWEEN
				query.SetCondition("Criteria1", Operator.IsNotBetween, new StringValue(s1, s2));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 NOT BETWEEN :pCriteria1_1 AND :pCriteria1_2", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 2);
				Assert.AreEqual(s1, cmd.Parameters[0].Value);
				Assert.AreEqual(s2, cmd.Parameters[1].Value);

				// IN
				query.SetCondition("Criteria1", Operator.IsAnyOf, new StringValue(new[] { s1, s2 }));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 IN (:pCriteria1_1, :pCriteria1_2)", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 2);
				Assert.AreEqual(s1, cmd.Parameters[0].Value);
				Assert.AreEqual(s2, cmd.Parameters[1].Value);

				// NOT IN
				query.SetCondition("Criteria1", Operator.IsNotAnyOf, new StringValue(new[] { s1, s2 }));
				cmd = query.CreateCommand();
				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 NOT IN (:pCriteria1_1, :pCriteria1_2)", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 2);
				Assert.AreEqual(s1, cmd.Parameters[0].Value);
				Assert.AreEqual(s2, cmd.Parameters[1].Value);
			}

			[TestMethod]
			public void Custom_Conditions_1()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]} {COLUMN2 [Criteria2]} {COLUMN3 [Criteria3]}};");

				query.SetCondition("Criteria1", new CustomConditionValue(1, 2, 3));
				query.SetCondition("Criteria2", new CustomParameterlessConditionValue("test"));
				query.SetCondition("Criteria3", new CustomConditionValue(4, 5, 6));

				var cmd = query.CreateCommand();

				AssertCommand(cmd);
				Assert.AreEqual("SELECT * FROM TABLE1 WHERE COLUMN1 = sillyProcedure(:pCriteria1_1, :pCriteria1_2, :pCriteria1_3) " +
				                "AND COLUMN2 = 'test' /*hint*/ AND COLUMN3 = sillyProcedure(:pCriteria3_1, :pCriteria3_2, :pCriteria3_3);", cmd.CommandText);
				Assert.IsTrue(cmd.Parameters.Count == 6);
				Assert.AreEqual("pCriteria1_1", cmd.Parameters[0].ParameterName);
				Assert.AreEqual("pCriteria1_2", cmd.Parameters[1].ParameterName);
				Assert.AreEqual("pCriteria1_3", cmd.Parameters[2].ParameterName);
				Assert.AreEqual("pCriteria3_1", cmd.Parameters[3].ParameterName);
				Assert.AreEqual("pCriteria3_2", cmd.Parameters[4].ParameterName);
				Assert.AreEqual("pCriteria3_3", cmd.Parameters[5].ParameterName);
				Assert.AreEqual(1, cmd.Parameters[0].Value);
				Assert.AreEqual(2, cmd.Parameters[1].Value);
				Assert.AreEqual(3, cmd.Parameters[2].Value);
				Assert.AreEqual(4, cmd.Parameters[3].Value);
				Assert.AreEqual(5, cmd.Parameters[4].Value);
				Assert.AreEqual(6, cmd.Parameters[5].Value);
			}

			private void AssertCommand(IDbCommand cmd)
			{
				Assert.IsNotNull(cmd);
				Assert.IsTrue(cmd.CommandType == CommandType.Text);
				Assert.IsTrue(cmd.Parameters.Count != 0);
			}

		}
	}
}
