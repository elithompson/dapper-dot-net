using System.Data;
using System.Linq;
using Dapper;
using SqlMapper;

namespace DapperTests_NET45
{
    public class Tests
    {
        public void TestBasicStringUsageAsync()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var query = connection.QueryAsync<string>("select 'abc' as [Value] union all select @txt", new { txt = "def" });
                var arr = query.Result.ToArray();
                arr.IsSequenceEqualTo(new[] { "abc", "def" });
            }
        }

        public void TestClassWithStringUsageAsync()
        {
            using (var connection = Program.GetOpenConnection())
            {
                var query = connection.QueryAsync<BasicType>("select 'abc' as [Value] union all select @txt", new { txt = "def" });
                var arr = query.Result.ToArray();
                arr.Select(x => x.Value).IsSequenceEqualTo(new[] { "abc", "def" });
            }
        }

        public void TestMultiMapWithSplitAsync()
        {
            var sql = @"select 1 as id, 'abc' as name, 2 as id, 'def' as name";
            using (var connection = Program.GetOpenConnection())
            {
                var productQuery = connection.QueryAsync<Product, Category, Product>(sql, (prod, cat) =>
                {
                    prod.Category = cat;
                    return prod;
                });

                var product = productQuery.Result.First();
                // assertions
                product.Id.IsEqualTo(1);
                product.Name.IsEqualTo("abc");
                product.Category.Id.IsEqualTo(2);
                product.Category.Name.IsEqualTo("def");
            }
        }

        public void TestExecuteAsyncCommand()
        {
            using(var connection = Program.GetOpenConnection())
            {
                connection.ExecuteAsync(@"
                    set nocount on 
                    create table #t(i int) 
                    set nocount off 
                    insert #t 
                    select @a a union all select @b 
                    set nocount on 
                    drop table #t", new { a = 1, b = 2 }).Result.IsEqualTo(2);
            }
        }

        public void TestExecuteAsyncCommandWithHybridParameters()
        {
            var p = new DynamicParameters(new { a = 1, b = 2 });
            p.Add("c", dbType: DbType.Int32, direction: ParameterDirection.Output);
            using (var connection = Program.GetOpenConnection())
            {
                connection.ExecuteAsync(@"set @c = @a + @b", p).Wait();
                p.Get<int>("@c").IsEqualTo(3);
            }
        }
        public void TestExecuteAsyncMultipleCommand()
        {
            using (var connection = Program.GetOpenConnection())
            {
                connection.ExecuteAsync("create table #t(i int)").Wait();
                int tally = connection.ExecuteAsync(@"insert #t (i) values(@a)", new[] { new { a = 1 }, new { a = 2 }, new { a = 3 }, new { a = 4 } }).Result;
                int sum = connection.Query<int>("select sum(i) from #t drop table #t").First();
                tally.IsEqualTo(4);
                sum.IsEqualTo(10);
            }
        }

        class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Category Category { get; set; }
        }
        class Category
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        class BasicType
        {
            public string Value { get; set; }
        }
    }
}