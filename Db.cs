using System.Data.SqlClient;

namespace Prodazha_Arenda_Nedvizhki
{
    public static class Db
    {
        public static readonly string ConnectionString =
            @"Data Source=localhost\SQLEXPRESS;Initial Catalog=ProdAren;Integrated Security=True";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
}