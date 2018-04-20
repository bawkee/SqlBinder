using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlBinder.ConditionValues;

namespace SqlBinder.UnitTesting
{
	public partial class SqlBinder_Tests
	{
		/// <summary>
		/// Tests that cover some negative branches
		/// </summary>
		[TestClass]
		public class Negative_Tests
		{			
			[TestInitialize]
			public void InitializeTest()
			{
				//
			}

			[TestMethod]			
			public void Negative_Test_1()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]}}");
				query.ThrowScriptErrorException = true;

				// Set the condition
				query.SetCondition("Criteria1", new StringValue(new [] { null, "A", "B" } ));

				try
				{
					query.CreateCommand();
					Assert.Fail();
				}
				catch (Exception ex)
				{
					Assert.IsInstanceOfType(ex, typeof(ParserException));
					Assert.IsNotNull(ex.InnerException);
					Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidConditionException));
				}

				// Set the condition
				query.SetCondition("Criteria1", new NumberValue(new[] { 1, 2, 3 }));

				try
				{
					query.CreateCommand();
					Assert.Fail();
				}
				catch (Exception ex)
				{
					Assert.IsInstanceOfType(ex, typeof(ParserException));
					Assert.IsNotNull(ex.InnerException);
					Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidConditionException));
				}
			}

			[TestMethod]
			public void Negative_Test_2()
			{
				var query = new MockQuery(_connection, "SELECT * FROM TABLE1 {WHERE {COLUMN1 [Criteria1]}}");
				query.ThrowScriptErrorException = true;

				// Set the condition
				try
				{
					query.SetCondition("Criteria1", new NumberValue((IEnumerable<decimal>)null));
					query.CreateCommand();
					Assert.Fail();
				}
				catch (Exception ex)
				{
					Assert.IsInstanceOfType(ex, typeof(ArgumentException));
				}

				// Set the condition				
				try
				{
					query.SetCondition("Criteria1", Operator.Contains, new NumberValue(1));
					query.CreateCommand();
					Assert.Fail();
				}
				catch (Exception ex)
				{
					Assert.IsInstanceOfType(ex, typeof(ParserException));
					Assert.IsNotNull(ex.InnerException);
					Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidConditionException));
				}


				// Set the condition				
				try
				{
					query.SetCondition("Criteria1", new StringValue(null));
					query.CreateCommand();
					Assert.Fail();
				}
				catch (Exception ex)
				{
					Assert.IsInstanceOfType(ex, typeof(ArgumentException));
				}
			}
		}
	}
}
