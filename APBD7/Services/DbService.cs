using System.Data;
using System.Data.SqlClient;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using APBD7.Classes;

namespace APBD7.Services;

public interface IDbService
{
   Task<int?> FullfilOrderAsync(CreateWarehouseProductRequest request);
}

public class DbService(IConfiguration configuration) : IDbService
{
    private async Task<SqlConnection> GetConnection()
    {
        var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        return connection;
    }

  public async Task<int?> FullfilOrderAsync(CreateWarehouseProductRequest request)
  {
      if (request.Amount <= 0)
          throw new ArgumentException("Can`t fullfil order with 0 amount");
      await using var connection = await GetConnection();
      var command = new SqlCommand();
      command.Connection = connection;
      command.CommandText = """
                            SELECT price FROM Product WHERE IdProduct = @idproduct
                            """;
      command.Parameters.AddWithValue("@idproduct", request.IdProduct);
      var reader = await command.ExecuteReaderAsync();

      if (!reader.HasRows) throw new Exception("The Product isn`t found");
      await reader.ReadAsync();
      var price = reader.GetSqlDecimal(0);
      await reader.CloseAsync();
      command = new SqlCommand();
      command.Connection = connection;
      command.CommandText = """SELECT count(*) from Warehouse where IdWarehouse=@idwarehouse""";
      command.Parameters.AddWithValue("@idwarehouse", request.IdWarehouse);
      var scalar = (int)await command.ExecuteScalarAsync();
      if (scalar == 0) throw new Exception("No such warehouse has been found");
      command = new SqlCommand();
      command.Connection = connection;
      command.CommandText = """SELECT IdOrder, CreatedAt from [Order] where IdProduct=@idproduct AND Amount = @amount""";
      command.Parameters.AddWithValue("@idproduct", request.IdProduct);
      command.Parameters.AddWithValue("@amount", request.Amount);
      reader = await command.ExecuteReaderAsync();
      if (!reader.HasRows) throw new Exception("No such order has been found");
      await reader.ReadAsync();
      var idOrder = reader.GetInt32(0);
      var createdat = reader.GetDateTime(1);
      await reader.CloseAsync();
      if (createdat.CompareTo(request.CreatedAt) >= 0) throw new DataException("The date is too late");
      command = new SqlCommand();
      command.Connection = connection;
      command.CommandText = """SELECT IdProductWarehouse from Product_Warehouse where IdOrder = @idorder""";
      command.Parameters.AddWithValue("@idorder", idOrder);
      var scalar1 = await command.ExecuteScalarAsync();
      if (scalar1 is not null) throw new Exception("This order is already complete");
      command = new SqlCommand();
      command.Connection = connection;
      command.CommandText = """UPDATE [Order] set FulfilledAt = @datetime where idOrder = @idorder""";
      command.Parameters.AddWithValue("@idorder", idOrder);
      command.Parameters.AddWithValue("@datetime", DateTime.Now);
      command = new SqlCommand();
      command.Connection = connection;
      command.CommandText = """INSERT INTO Product_Warehouse values (@idwarehouse,@idproduct,@idorder,@amount,@price,@createdAt);SELECT CAST(SCOPE_IDENTITY() as int)""";
      command.Parameters.AddWithValue("@idwarehouse", request.IdWarehouse);
      command.Parameters.AddWithValue("@idproduct", request.IdProduct);
      command.Parameters.AddWithValue("@idorder", idOrder);
      command.Parameters.AddWithValue("@amount", request.Amount);
      command.Parameters.AddWithValue("@price", price);
      command.Parameters.AddWithValue("@createdAt", DateTime.Now);
      reader = await command.ExecuteReaderAsync();
      await reader.ReadAsync();
      var id = reader.GetInt32(0);
      await reader.CloseAsync();
      return id;
  }
}